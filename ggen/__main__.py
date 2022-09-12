import argparse
import glob
import json
import os
import re
import sys
import subprocess
from typing import Dict, List, Optional

from . import genbuffers
from . import genshaders
from .genbuffers import Vertex
from .genshaders import ShaderDirectives, SpirvReflection, ShaderStage, ArgumentBufferBinding

argparser = argparse.ArgumentParser()
argparser.add_argument('root')


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


def process_shader(root: str, paths: List[str]):
    directives = ShaderDirectives()
    reflections: Dict[ShaderStage, SpirvReflection] = {}
    # metal_buffer_bindings: Dict[str, int] = {}
    shader_name = ''

    had_compile_errors = False
    for path in paths:
        filename = os.path.splitext(path)[0]
        name, mode = os.path.splitext(filename)
        shader_name = os.path.basename(name)

        stage = None
        if mode == '.vert':
            stage = ShaderStage.VERTEX
        elif mode == '.frag':
            stage = ShaderStage.FRAGMENT

        if stage == ShaderStage.VERTEX:
            directives = genshaders.parse_shader_directives(path)

        spv_path = filename + '.spv'
        gl_spv_path = filename + '_gl.spv'
        should_compile = True
        if os.path.isfile(spv_path):
            source_stat = os.stat(path)
            spv_stat = os.stat(spv_path)
            if source_stat.st_mtime_ns == spv_stat.st_mtime_ns:
                should_compile = False

        if should_compile:
            print(f'Compiling {path}...')
            subprocess.check_call([GLSLANG_BINARY, '-V', '-o', spv_path, path])
        else:
            print(f'{path} already up to date')

        relfection_json = subprocess.check_output([SPIRV_CROSS_BINARY, spv_path, '--reflect'])
        reflection_data = json.loads(relfection_json)
        reflection = genshaders.parse_spv_reflection(reflection_data)
        reflections[stage] = reflection
            
        # Output Metal shader
        metal_source = subprocess.check_output([SPIRV_CROSS_BINARY, spv_path, '--msl', '--msl-version', '20000', '--msl-argument-buffers'], text=True)
        with open(filename + '.metal', 'w') as f:
            f.write(metal_source)
            
        # Compile a version for OpenGL as well
        gl_defines = [
            '-Dgl_VertexIndex=gl_VertexID',
            '-Dgl_InstanceIndex=gl_InstanceID'
        ]
        subprocess.check_call([GLSLANG_BINARY, '-G100', *gl_defines, '-o', gl_spv_path, path])

        reflection.arg_buffer_bindings = find_argument_bindings(metal_source)
        
        # metal_buffer_bindings.update(find_metal_buffer_bindings(metal_source))

    # if not root_reflection:
    #     print(f'Error: No vert shader for {shader_name}')
    #     return

    # for reflection in reflections:
    #     root_reflection.types.update(reflection.types)
    #     root_reflection.ubos.extend(reflection.ubos)
    #     root_reflection.ssbos.extend(reflection.ssbos)
    #     root_reflection.textures.extend(reflection.textures)

    shader = genshaders.Shader(shader_name, directives, reflections)

    genshaders.generate_shader_file(root, shader)


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


def process_shaders(root):
    paths = glob.glob(f'{root}/**/*.glsl', recursive=True)

    shaders_by_name: Dict[str, List[str]] = {}

    # Figure out which shaders belong together
    for path in paths:
        filename = os.path.basename(path)
        name = os.path.splitext(os.path.splitext(filename)[0])[0]
        shaders = shaders_by_name.setdefault(name, [])
        shaders.append(path)

    for name, paths in shaders_by_name.items():
        process_shader(root, paths)


def main():
    args = argparser.parse_args()
    process_shaders(args.root)


if __name__ == '__main__':
    main()
