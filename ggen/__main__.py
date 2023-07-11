import argparse
import enum
import glob
import json
import os
import re
import sys
import subprocess
from typing import Dict, List, Optional, Set

from . import genbuffers
from . import genshaders
from .genbuffers import Vertex
from .genshaders import ShaderDirectives, SpirvReflection, ShaderStage, ArgumentBufferBinding


class Target(enum.Enum):
    CS_CLASS = 'csclass'
    OPENGL_SPIRV = 'opengl_spirv'
    METAL = 'metal'


argparser = argparse.ArgumentParser()
argparser.add_argument('root')

argparser.add_argument('--files', help="List of file paths to glsl files. Optional, used by msbuild target. "
                                       "The list of files is a single argument, delimited by semicolons.")

argparser.add_argument('--targets',
                       help='List of the target shader types to output. Optional, used by the msbuild target.'
                            'This is a list of targets, delimited by semicolons.')

argparser.add_argument('--output-dir',
                       help='Directory to output generated cs files to.')


binaries_path = os.path.normpath(os.path.join(__file__, '..', 'binaries', sys.platform))
EXE_POSTFIX = '.exe' if sys.platform == 'win32' else ''
GLSLANG_BINARY = os.path.join(binaries_path, 'glslangValidator' + EXE_POSTFIX)
SPIRV_CROSS_BINARY = os.path.join(binaries_path, 'spirv-cross' + EXE_POSTFIX)


def parse_buffers(root: str) -> Dict[str, Vertex]:
    paths = glob.glob('**/*.buffer.json', recursive=True, root_dir=root)

    buffers = {}

    for path in paths:
        print(f'Parsing buffer at {path}')
        with open(path, encoding='utf-8-sig') as f:
            buffer_data = json.load(f)
        buffer = genbuffers.parse_buffer(buffer_data)
        buffers[buffer.name] = buffer

    return buffers


def process_shader(root: str, paths: List[str], targets: Optional[Set[Target]], output_dir: Optional[str]):
    directives = ShaderDirectives()
    reflections: Dict[ShaderStage, SpirvReflection] = {}
    shader_name = ''

    # NOTE: Due to how we're set up with msbuild, we'll want caching here to reduce redundant glslcross work that is
    # done for all targets.

    had_compile_errors = False
    for path in paths:
        dirname, filename = os.path.split(path)
        relative_dir = os.path.relpath(dirname, root)
        filename_no_ext = os.path.splitext(filename)[0]
        full_output_dir = os.path.join(root, output_dir, relative_dir)
        os.makedirs(full_output_dir, exist_ok=True)
        output_path_no_ext = os.path.join(full_output_dir, filename_no_ext)
        name, mode = os.path.splitext(filename_no_ext)
        shader_name = os.path.basename(name)

        stage = None
        if mode == '.vert':
            stage = ShaderStage.VERTEX
        elif mode == '.frag':
            stage = ShaderStage.FRAGMENT

        if stage == ShaderStage.VERTEX:
            directives = genshaders.parse_shader_directives(path)

        spv_path = output_path_no_ext + '.spv'

        print(f'Compiling {path} -> {spv_path} ...')
        # TODO: Cache spv output if possible. Either with our own dirty detection, or refactor and have an msbuild spv compile dependency target.
        subprocess.check_call([GLSLANG_BINARY, '-V', '-o', spv_path, path])

        relfection_json = subprocess.check_output([SPIRV_CROSS_BINARY, spv_path, '--reflect'])
        reflection_data = json.loads(relfection_json)
        reflection = genshaders.parse_spv_reflection(reflection_data)
        reflections[stage] = reflection
            
        # We generate the metal source for both the CS and Metal targets, since we need it for the CS target.
        if not targets or Target.METAL in targets or Target.CS_CLASS in targets:
            metal_source = subprocess.check_output([SPIRV_CROSS_BINARY, spv_path, '--msl', '--msl-version', '20000', '--msl-argument-buffers'], text=True)

            # Output Metal shader
            if not targets or Target.METAL in targets:
                with open(output_path_no_ext + '.metal', 'w') as f:
                    f.write(metal_source)
                
            reflection.arg_buffer_bindings = find_argument_bindings(metal_source)
            
        # Compile a version for OpenGL as well
        if not targets or Target.OPENGL_SPIRV in targets:
            gl_defines = [
                '-Dgl_VertexIndex=gl_VertexID',
                '-Dgl_InstanceIndex=gl_InstanceID'
            ]
            gl_spv_path = output_path_no_ext + '_gl.spv'
            subprocess.check_call([GLSLANG_BINARY, '-G100', *gl_defines, '-o', gl_spv_path, path])

    if not targets or Target.CS_CLASS in targets:
        shader = genshaders.Shader(shader_name, directives, reflections)
        genshaders.generate_shader_file(root, paths, shader, output_dir)


METAL_ENTRY_POINT_PARAMETERS_PATTERN = re.compile(r'main0\((.*)\)')
METAL_PARAMETER_PATTERN = re.compile(r'(?P<name>\w+)&?\s+\w+\s*\[\[\w+\((?P<index>\d+)\)\]\]')
METAL_ARGUMENT_INDEX_PATTERN = re.compile(r'(?P<typename>[^*&\s]+)[*&]?\s(?P<name>\w+)\s*\[\[id\((?P<argId>\d+)\)\]\];')


def find_metal_buffer_bindings(metal_source: str) -> Dict[str, int]:
    bindings = {}
    for line in metal_source.split('\n'):
        line = line.strip()
        entry_match = METAL_ENTRY_POINT_PARAMETERS_PATTERN.search(line)
        if entry_match:
            for param in entry_match.group(1).split(','):
                param_match = METAL_PARAMETER_PATTERN.search(param)
                if param_match:
                    bindings[param_match.group('name')] = int(param_match.group('index'))
        
    return bindings


def find_argument_bindings(metal_source: str) -> List[ArgumentBufferBinding]:
    bindings = []
    for line in metal_source.split('\n'):
        line = line.strip()
        match = METAL_ARGUMENT_INDEX_PATTERN.search(line)
        if match:
            typename = match.group('typename')
            name = match.group('name')
            id = int(match.group('argId'))
            bindings.append(ArgumentBufferBinding(typename, name, id))
        
    return bindings


def process_shaders(args):
    if args.files:
        paths = set(args.files.split(';'))
        
        # Filter out any receipt files that are part of the input, as those are an artifact of msbuild.
        # TODO: Inneficient
        paths = set(path for path in paths if '_receipt.txt' not in path)

        # Find shaders which belong together
        companion_files: List[str] = []
        for path in paths:
            basename = os.path.splitext(os.path.splitext(path)[0])[0]
            companion_files.extend(glob.glob(f'{basename}.*.glsl'))
        
        paths.update(companion_files)
            
    else:
        paths = glob.glob(f'{args.root}/**/*.glsl', recursive=True)
    
    targets = None
    if args.targets:
        targets = set((Target(targetname) for targetname in args.targets.split(';')))

    shaders_by_name: Dict[str, List[str]] = {}

    # Figure out which shaders belong together
    for path in paths:
        filename = os.path.basename(path)
        name = os.path.splitext(os.path.splitext(filename)[0])[0]
        shaders = shaders_by_name.setdefault(name, [])
        shaders.append(path)

    for name, paths in shaders_by_name.items():
        process_shader(args.root, paths, targets, args.output_dir)


def main():
    args = argparser.parse_args()
    process_shaders(args)


if __name__ == '__main__':
    main()
