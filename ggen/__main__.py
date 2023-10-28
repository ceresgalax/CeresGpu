import argparse
import glob
import json
import os
import re
import sys
import subprocess
from typing import Dict, List, Tuple

from .graph import Graph, Node
from . import genbuffers
from . import genshaders
from .genbuffers import Vertex
from .genshaders import SpirvReflection, ShaderStage, ArgumentBufferBinding


argparser = argparse.ArgumentParser()
argparser.add_argument('root')

argparser.add_argument('--files', help="List of file paths to glsl files. Optional, used by msbuild target. "
                                       "The list of files is a single argument, delimited by semicolons.")

argparser.add_argument('--ggen-script-files')

argparser.add_argument('--rebuild', default=False, action='store_true')

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


def compile_to_spv(node: Node):
    input_path = node.tagged_inputs[0][1].filepath
    output_path = node.filepath
    subprocess.check_call([GLSLANG_BINARY, '-V', '-o', output_path, input_path])
    
    
def spv_to_opengl(node: Node):
    input_path = node.tagged_inputs[0][1].filepath
    output_path = node.filepath
    # gl_defines = [
    #     '-Dgl_VertexIndex=gl_VertexID',
    #     '-Dgl_InstanceIndex=gl_InstanceID'
    # ]
    subprocess.check_call([SPIRV_CROSS_BINARY, '--output', output_path, input_path])


def spv_to_metal(node: Node):
    input_path = node.tagged_inputs[0][1].filepath
    output_path = node.filepath
    
    metal_source = subprocess.check_output([SPIRV_CROSS_BINARY, input_path, '--msl', '--msl-version', '20000', '--msl-argument-buffers'], text=True)
    with open(output_path, 'w') as f:
        f.write(metal_source)


def spv_to_reflection(node: Node):
    input_path = node.tagged_inputs[0][1].filepath
    output_path = node.filepath

    reflection_json = subprocess.check_output([SPIRV_CROSS_BINARY, input_path, '--reflect'])
    with open(output_path, 'wb') as f:
        f.write(reflection_json)


def gen_cs(node: Node):
    source_inputs = [(t, n) for t, n in node.tagged_inputs if n.action == '']
    reflection_inputs = [(t, n) for t, n in node.tagged_inputs if n.action == ACTION_SPVCROSS_REFLECT]
    metal_inputs = [(t, n) for t, n in node.tagged_inputs if n.action == ACTION_SPVCROSS_METAL]
    output_path = node.filepath

    # Get output filename without '.generated.cs'
    shader_name = os.path.basename(output_path)[:-len('.generated.cs')]

    reflections: Dict[ShaderStage, SpirvReflection] = {}
    for tag, reflection_input in reflection_inputs:
        with open(reflection_input.filepath, 'r') as f:
            reflection_data = json.load(f)
        reflection = genshaders.parse_spv_reflection(reflection_data)

        # Find the corresponding metal source input
        matching_metal_input = [input for t, input in metal_inputs if t == tag][0]
        
        with open(matching_metal_input.filepath, 'r') as f:
            metal_source = f.read()
        
        reflection.arg_buffer_bindings = find_argument_bindings(metal_source)

        stage = None
        if tag == 'vert':
            stage = ShaderStage.VERTEX
        elif tag == 'frag':
            stage = ShaderStage.FRAGMENT
            
        reflections[stage] = reflection

    # Find the vert source input
    vert_source_input = [input for t, input in source_inputs if t == 'vert'][0]
    # Parse directives from the vert shader source
    directives = genshaders.parse_shader_directives(vert_source_input.filepath)
        
    shader = genshaders.Shader(shader_name, directives, reflections)
    genshaders.generate_shader_file(output_path, shader)
    

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


ACTION_COMPILE_TO_SPV = 'compile_to_spv'
ACTION_COMPILE_TO_OPENGL = 'spvcross_opengl'
ACTION_SPVCROSS_METAL = 'spvcross_metal'
ACTION_SPVCROSS_REFLECT = 'spvcross_reflect'
ACTION_GEN_CS = 'gen_cs'


def get_mode(path: str):
    path_without_first_ext = os.path.splitext(path)[0]
    path_without_mode_ext, mode = os.path.splitext(path_without_first_ext)
    return mode.lstrip('.')


def process_shaders(args):
    if args.files:
        paths = set(args.files.split(';'))
        
        # Find shaders which belong together
        companion_files: List[str] = []
        for path in paths:
            basename = os.path.splitext(os.path.splitext(path)[0])[0]
            companion_files.extend(glob.glob(f'{basename}.*.glsl'))
        
        paths.update(companion_files)
        paths = [p for p in paths if p]
    else:
        paths = glob.glob(f'{args.root}/**/*.glsl', recursive=True)

    shaders_by_name: Dict[str, List[str]] = {}

    # Figure out which shaders belong together
    for path in paths:
        filename = os.path.basename(path)
        name = os.path.splitext(os.path.splitext(filename)[0])[0]
        shaders = shaders_by_name.setdefault(name, [])
        shaders.append(path)

    graph = Graph()
    
    for name, paths in shaders_by_name.items():
        rel_dir = os.path.dirname(os.path.relpath(paths[0], args.root))
        rel_out_dir = os.path.join(args.output_dir, rel_dir)
        
        source_nodes: List[Tuple[str, Node]] = []
        reflection_nodes: List[Tuple[str, Node]] = []
        metal_nodes: List[Tuple[str, Node]] = []
        for path in paths:
            mode = get_mode(path)
            
            source_node = Node(path, '', [])
            source_nodes.append((mode, source_node))
            
            filename_no_ext = os.path.splitext(os.path.basename(path))[0]
            output_no_ext = os.path.join(rel_out_dir, filename_no_ext)
            spv_path = output_no_ext + '.spv'
            opengl_path = output_no_ext + '_gl.glsl'
            metal_path = output_no_ext + '.metal'
            reflection_path = output_no_ext + '.reflection.json'
            
            spv_node = Node(spv_path, ACTION_COMPILE_TO_SPV, [('', source_node)])
            
            opengl_node = Node(opengl_path, ACTION_COMPILE_TO_OPENGL, [('', spv_node)])
            graph.root_nodes.append(opengl_node)
            
            metal_node = Node(metal_path, ACTION_SPVCROSS_METAL, [('', spv_node)])
            metal_nodes.append((mode, metal_node))
            
            reflection_node = Node(reflection_path, ACTION_SPVCROSS_REFLECT, [('', spv_node)])
            reflection_nodes.append((mode, reflection_node))
            
        generated_cs_path = os.path.join(rel_out_dir, f'{name}.Generated.cs')
        generated_cs_node = Node(generated_cs_path, ACTION_GEN_CS, reflection_nodes + metal_nodes + source_nodes)
        graph.root_nodes.append(generated_cs_node)
        
    actions = {
        ACTION_COMPILE_TO_SPV: compile_to_spv,
        ACTION_COMPILE_TO_OPENGL: spv_to_opengl,
        ACTION_SPVCROSS_METAL: spv_to_metal,
        ACTION_SPVCROSS_REFLECT: spv_to_reflection,
        ACTION_GEN_CS: gen_cs
    }
    
    min_modtime = 0  
    if args.ggen_script_files:
        script_files = [p.strip() for p in args.ggen_script_files.split(';')]
        for ggen_script_file in script_files:
            min_modtime = max(os.stat(ggen_script_file).st_mtime_ns, min_modtime)
    
    dirty_nodes = graph.find_dirty_nodes(min_modtime)
    for node in graph.walk():
        if node not in dirty_nodes and not args.rebuild:
            continue
        
        print(f'[{node.action}] {node.filepath}')
        sys.stdout.flush()
        if not node.action:
            continue
            
        os.makedirs(os.path.dirname(node.filepath), exist_ok=True)
            
        actions[node.action](node)


def main():
    args = argparser.parse_args()
    process_shaders(args)


if __name__ == '__main__':
    main()
