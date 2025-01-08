import os
import re
from enum import Enum, auto
from typing import List, Dict, Any, TextIO, Set, Optional


class Member(object):
    def __init__(self, name: str, type: str, offset: int, matrix_stride: int, array_sizes: List[int],
                 array_size_is_literal: bool, array_stride: int):
        self.name = name
        self.type = type
        self.offset = offset
        self.matrix_stride = matrix_stride
        self.array_sizes = array_sizes
        self.array_size_is_literal = array_size_is_literal
        self.array_stride = array_stride


class ShaderType(object):
    def __init__(self, name: str, members: List[Member]):
        self.name = name
        self.members = members


class BufferInput(object):
    def __init__(self, type: str, name: str, block_size: int, set: int, binding: int):
        self.type = type
        self.name = name
        self.block_size = block_size
        self.set = set
        self.binding = binding


class TextureInput(object):
    def __init__(self, type: str, name: str, set: int, binding: int):
        self.type = type
        self.name = name
        self.set = set
        self.binding = binding


class StageInput(object):
    def __init__(self, type: str, name: str, location: int):
        self.type = type
        self.name = name
        self.location = location


class ArgumentBufferBinding(object):
    def __init__(self, typename: str, name: str, index: int):
        self.typename = typename
        self.name = name
        self.index = index


class SpirvReflection(object):
    def __init__(self, types: Dict[str, ShaderType], inputs: List[StageInput], ssbos: List[BufferInput],
                 ubos: List[BufferInput], textures: List[TextureInput]):
        self.types = types
        self.inputs = inputs
        self.ssbos = ssbos
        self.ubos = ubos
        self.textures = textures
        self.arg_buffer_bindings: List[ArgumentBufferBinding] = []


class ShaderCodeCollection(object):
    def __init__(self):
        self.metal_vertex_source = ''
        self.metal_fragment_source = ''
        self.gl_vertex_spirv = b''
        self.gl_fragment_spirv = b''


# noinspection PyArgumentList
class StepMode(Enum):
    PER_VERTEX = auto()
    PER_INSTANCE = auto()


# noinspection PyArgumentList
class ShaderStage(Enum):
    VERTEX = auto()
    FRAGMENT = auto()


class InputDirective(object):
    def __init__(self):
        self.structure_name = 'Vertex'
        self.step_mode = StepMode.PER_VERTEX
        self.hint = ''
        self.buffer_type = ''


class ShaderDirectives(object):
    def __init__(self):
        self.full_class_name = ''
        self.input_directives_by_input_name: Dict[str, InputDirective] = {}
        self.descriptor_field_hints_by_name: Dict[str, str] = {}


class Shader(object):
    def __init__(self, resource_prefix: str, directives: ShaderDirectives,
                 reflections_by_stage: Dict[ShaderStage, SpirvReflection]):
        self.resource_prefix = resource_prefix
        self.directives = directives
        self.reflections_by_stage = reflections_by_stage


class InputAttribute(object):
    def __init__(self, name: str, input: StageInput, directive: InputDirective):
        self.name = name
        self.input = input
        self.directive = directive
        self.offset = 0


class SourceWriter(object):
    def __init__(self, f: TextIO):
        self.f = f
        self.indent_level = 0

    def indent(self):
        self.indent_level += 1

    def deindent(self):
        self.indent_level -= 1

    def write_line(self, *lines: str):
        for line in lines:
            self.f.write('    ' * self.indent_level)
            self.f.write(line)
            self.f.write('\n')


directive_pattern = re.compile(r'\/\/\s*#(\w+):\s*([^\n]+)')
input_pattern = re.compile(r'layout\s*\(.*\)\s*in\s*\w+\s*(\w+);\s*\/\/\s*#input\s*(.*)')


def parse_member(data: Dict[str, Any]) -> Member:
    return Member(
        name=data['name'],
        type=data['type'],
        offset=data['offset'],
        matrix_stride=data.get('array_stride', 0),
        array_sizes=data.get('array', []),
        array_size_is_literal=data.get('array_size_is_literal', False),
        array_stride=data.get('array_stride', 0)
    )


def parse_type(data: Dict[str, Any]) -> Optional[ShaderType]:
    first_offsetlet_member = next((member for member in data['members'] if 'offset' not in member), None)
    if first_offsetlet_member:
        # This is an incomplete type. Don't include it.
        # (spirv-cross will include these sometimes, along with the 'real' type that we care about)
        return None

    return ShaderType(
        name=data['name'],
        members=[parse_member(member) for member in data['members']]
    )


def parse_stage_input(data: Dict[str, Any]) -> StageInput:
    return StageInput(
        type=data['type'],
        name=data['name'],
        location=data['location']
    )


def parse_buffer_input(data: Dict[str, Any]) -> BufferInput:
    return BufferInput(
        type=data['type'],
        name=data['name'],
        block_size=data['block_size'],
        set=data['set'],
        binding=data['binding']
    )


def parse_texture_input(data: Dict[str, Any]) -> TextureInput:
    return TextureInput(
        type=data['type'],
        name=data['name'],
        set=data['set'],
        binding=data['binding']
    )


def parse_spv_reflection(data: Dict[str, Any]) -> SpirvReflection:
    types = [(k, parse_type(v)) for k, v in data.get('types', {}).items() if not v['name'].startswith('gl_')]

    return SpirvReflection(
        types={k: v for k, v in types if v},
        inputs=[parse_stage_input(input) for input in data.get('inputs', [])],
        ssbos=[parse_buffer_input(ssbo) for ssbo in data.get('ssbos', [])],
        ubos=[parse_buffer_input(ubo) for ubo in data.get('ubos', [])],
        textures=[parse_texture_input(texture) for texture in data.get('textures', [])]
    )


def parse_shader_directives(path: str) -> ShaderDirectives:
    data = ShaderDirectives()

    with open(path, 'r', encoding='utf-8') as f:
        while True:
            line = f.readline()
            if not line:
                break

            directive_match = directive_pattern.search(line)
            if directive_match:
                directive_name = directive_match.group(1)
                directive_value = directive_match.group(2)

                if directive_name == 'CSNAME':
                    data.full_class_name = directive_value.strip()
                    
                if directive_name == 'DescriptorHint':
                    name, hint = [part.strip() for part in directive_value.strip().split('=', maxsplit=2)]
                    data.descriptor_field_hints_by_name[name] = hint

            input_match = input_pattern.search(line)
            if input_match:
                input_name = input_match.group(1)
                input_directive_text = input_match.group(2)

                input_directive = InputDirective()
                data.input_directives_by_input_name[input_name] = input_directive

                property_strings = input_directive_text.strip().split(' ')
                for property_string in property_strings:
                    key, value = property_string.split(':')
                    key = key.lower()

                    if key == 'struct':
                        input_directive.structure_name = value
                    elif key == 'stepmode':
                        input_directive.step_mode = StepMode[value]
                    elif key == 'buffertype':
                        input_directive.buffer_type = value
                    elif key == 'hint':
                        input_directive.hint = value

    return data


current_shader_id = 1


def to_cs_style(val: str) -> str:
    val = val[0].upper() + val[1:]
    while True:
        index = val.find('_')
        if index == -1:
            break
        if index + 1 != len(val):
            val = val[:index] + val[index + 1].upper() + val[index + 2:]
        else:
            val = val[:index]

    return val


def make_cs_string_literal(val: str) -> str:
    return val.replace('\\', '\\\\').replace('\n', '\\n').replace('\r', '\\r').replace('\t', '\\t').replace('"', '\\"')


def make_multiline_cs_string_literal(lines: List[str]) -> str:
    parts = []
    for i, line in enumerate(lines):
        parts.append('"')
        parts.append(make_cs_string_literal(line))
        parts.append('"')
        if i + 1 != len(lines):
            parts.append(' +\n')

    return ''.join(parts)


def generate_shader_file(output_path: str, shader: Shader):
    output_dir = os.path.dirname(output_path)
    os.makedirs(output_dir, exist_ok=True)
    
    # # TODO: Later validate that all shader source files are in the same directory
    # first_path = paths[0]
    # dir = os.path.dirname(first_path)
    # 
    # project_path = dir
    # os.makedirs(project_path, exist_ok=True)
    # 
    # base_filename = os.path.basename(os.path.splitext(os.path.splitext(paths[0])[0])[0])

    # output_root = project_path
    # if output_dir:
    #     if not os.path.isabs(output_dir):
    #         output_dir = os.path.join(root, output_dir)
    #     rel_dir = os.path.relpath(dir, root)
    #     output_root = os.path.join(output_dir, rel_dir)
    #     os.makedirs(output_root, exist_ok=True)

    with open(output_path, 'w') as f:
        generate_shader_class(SourceWriter(f), shader)


def generate_shader_class(f: SourceWriter, shader: Shader):
    directives = shader.directives
    # reflection = shader.reflection
    full_name_parts = directives.full_class_name.split('.')
    namespace_parts = full_name_parts[:-1]
    class_name = full_name_parts[-1]
    namespace = '.'.join(namespace_parts)

    input_attributes_by_structure: Dict[str, List[InputAttribute]] = {}
    strides_by_structure: Dict[str, int] = {}
    
    vertex_reflection = shader.reflections_by_stage[ShaderStage.VERTEX]
    for input in vertex_reflection.inputs:
        directive = directives.input_directives_by_input_name.get(input.name, InputDirective())
        input_attributes_by_structure.setdefault(directive.structure_name, []).append(InputAttribute(input.name, input, directive))
    
    for input_attributes in input_attributes_by_structure.values():
        input_attributes.sort(key=lambda attrib: attrib.input.location)

    # Using statements
    f.write_line(
        '#nullable enable',
        'using System;',
        'using System.Collections.Generic;',
        'using System.CodeDom.Compiler;',
        'using System.IO;',
        'using System.Numerics;',
        'using System.Runtime.InteropServices;',
        'using CeresGL;',
        'using CeresGpu.Graphics;',
        'using CeresGpu.Graphics.Shaders;',
        'using CeresGpu.Graphics.OpenGL;',
        'using CeresGpu.Graphics.Metal;',
        'using CeresGpu.Graphics.Vulkan;',
        ''
    )

    # Begin Namespace
    f.write_line(f'namespace {namespace}')
    f.write_line('{')
    f.indent()

    # Begin Class
    f.write_line(
        '[GeneratedCode("genshaders.py", "0")]',
        f'public class {class_name} : IShader',
        '{',
        '    public IShaderBacking? Backing { get; set; }',
        '    public readonly ShaderVertexAttributeDescriptor[] _vertexAttributeDescriptors;',
        '',
    )
    f.indent()
    
    # Begin Constructor

    f.write_line(
        f'public {class_name}()',
        '{',
    )
    f.indent()

    # Output initialization of _vertexAttributeDescriptors in constructor
    f.write_line('_vertexAttributeDescriptors = new ShaderVertexAttributeDescriptor[] {')
    f.indent()

    attributes_by_index: List[Optional[InputAttribute]] = []
    for structure_name, attributes in input_attributes_by_structure.items():
        for attribute in attributes:
            while attribute.input.location >= len(attributes_by_index):
                attributes_by_index.append(None)
            attributes_by_index[attribute.input.location] = attribute
            
    for i, attribute in enumerate(attributes_by_index):
        if attribute is None:
            # The shader has gaps in the attribute location indices.
            f.write_line('default,')
        else:
            buffer_type = attribute.directive.buffer_type
            if not buffer_type:
                buffer_type = spirv_to_default_buffer_types[attribute.input.type]

            f.write_line(
                'new ShaderVertexAttributeDescriptor() {',
                f'    Name = "{make_cs_string_literal(attribute.name)}",',
                f'    Hint = "{make_cs_string_literal(attribute.directive.hint)}",',
                f'    Format = VertexFormat.{buffer_type_to_mtlvertexformat[buffer_type]}',
                '},'
            )
    
    f.deindent()
    f.write_line('};')
    
    # End Constructor
    f.deindent()
    f.write_line(
        '}',
        ''
    )
        
    # Output Dispose Method
    f.write_line(
        'public void Dispose()',
        '{',
        '    Backing?.Dispose();',
        '}',
        ''
    )
    
    # Begin Prime Method
    f.write_line(
        'public void Prime(IRenderer renderer)',
        '{',
        '    Backend backend = renderer switch {',
        '        GLRenderer gl => Backend.GL,',
        '        MetalRenderer metal => Backend.Metal,',
        '        VulkanRenderer vulkan => Backend.Vulkan,',
        '        _ => default',
        '    };',
        '',
        '    Descriptors = new DescriptorInfo[] {',
    )
    f.indent()

    # Write Descriptor info
    f.indent()
    
    def write_descriptor_info_field_for_buffer(buffer: BufferInput, reflection: SpirvReflection, cs_descriptor_type: str):

        # TODO: METAL BUFFER IDS WILL NOT MATCH THE SPIR-V BINDING INDEX WHEN ARRAYS OF BUFFERS ARE USED.
        # Metal uses an argument buffer Id for each array element, where Vulkan uses the same binding.
        # This is uncommon for my project, but re-mapping support may need to be added.
        # It would be easier to add this support with bindings to libspirvcross
        # instead of regex detection of the generated metal source.
        # https://github.com/KhronosGroup/SPIRV-Cross#msl-20

        # Figure out what the metal argument buffer binding index is
        def get_binding_index():
            for abb in reflection.arg_buffer_bindings:
                # For some reason spirv-cross reflects the Uniform typename as the name.
                if abb.typename == buffer.name:
                    return abb.index

        if buffer.type[0] == '_':
            type_name = reflection.types[buffer.type].name
        else:
            type_name = spirv_to_cs_types[buffer.type]

        f.write_line(
            'new DescriptorInfo {',
            '    Binding = MakeBinding(',
            '        backend,',
            f'        gl: new GLDescriptorBindingInfo {{ BindingIndex = {buffer.binding} }},',  # TODO: Do we need locations for GL?
            f'        metal: new MetalDescriptorBindingInfo {{ BindingIndex = {get_binding_index()} }},',
            f'        vulkan: new VulkanDescriptorBindingInfo {{ Set = {buffer.set}, Binding = {buffer.binding} }}',
            '    ),',
            f'    DescriptorType = DescriptorType.{cs_descriptor_type},',
            f'    BufferType = typeof({type_name}),',
            f'    Name = "{make_cs_string_literal(buffer.name)}"',
            '},'
        )

    def write_descriptor_info_field_for_texture(texture: TextureInput, reflection: SpirvReflection):
        # Figure out what the metal argument buffer binding index is
        def get_binding_index(name: str):
            for abb in reflection.arg_buffer_bindings:
                # For some reason spirv-cross reflects the Uniform typename as the name.
                if abb.name == name:
                    return abb.index
            return 0

        texture_binding = get_binding_index(texture.name)
        sampler_binding = get_binding_index(texture.name + 'Smplr')

        hint = shader.directives.descriptor_field_hints_by_name.get(texture.name, '')

        f.write_line(
            'new DescriptorInfo {',
            '    Binding = MakeBinding(',
            '        backend,',
            f'        gl: new GLDescriptorBindingInfo {{ BindingIndex = {texture.binding} }},',
            f'        metal: new MetalDescriptorBindingInfo {{ BindingIndex = {texture_binding}, SamplerIndex = {sampler_binding} }},',
            f'        vulkan: new VulkanDescriptorBindingInfo {{ Set = {texture.set}, Binding = {texture_binding} }}',
            '    ),',
            '    DescriptorType = DescriptorType.Texture,',
            f'    Name = "{make_cs_string_literal(texture.name)}",',
            f'    Hint = "{make_cs_string_literal(hint)}",',
            '},'
        )

    for stage, reflection in shader.reflections_by_stage.items():
        for ubo in reflection.ubos:
            write_descriptor_info_field_for_buffer(ubo, reflection, 'UniformBuffer')

        for ssbo in reflection.ssbos:
            write_descriptor_info_field_for_buffer(ssbo, reflection, 'ShaderStorageBuffer')

        for texture in reflection.textures:
            write_descriptor_info_field_for_texture(texture, reflection)
    
    # Close Descriptors initialization
    f.deindent()
    f.write_line('};')
    
    # End Prime Method
    f.deindent()
    f.write_line('}', '')

    # Emit DescriptorBinding helper method
    f.write_line(
        'private enum Backend { GL, Metal, Vulkan }',
        '',
        'private IDescriptorBindingInfo MakeBinding(Backend backend, in GLDescriptorBindingInfo gl, in MetalDescriptorBindingInfo metal, in VulkanDescriptorBindingInfo vulkan)',
        '{',
        '    return backend switch {',
        '        Backend.GL => gl,',
        '        Backend.Metal => metal,',
        '        Backend.Vulkan => vulkan,',
        '        _ => gl',
        '    };',
        '}',
        ''
    )

    # Emit GetShaderResource Method
    f.write_line(
        'public Stream? GetShaderResource(string postfix)',
        '{',
        f'    Type thisType = typeof({class_name});',
        f'    return thisType.Assembly.GetManifestResourceStream(thisType, "{make_cs_string_literal(shader.resource_prefix)}" + postfix);',
        # f'    return "{shader.resource_prefix}";',
        '}',
        '',
    )

    # Emit Structures
    for reflection in shader.reflections_by_stage.values():
        for type in reflection.types.values():
            gen_structure(f, type, reflection, shader.directives.descriptor_field_hints_by_name)

    current_vert_buffer_index = 0
    for structure_name, attributes in input_attributes_by_structure.items():
        f.write_line(
            '[StructLayout(LayoutKind.Explicit)]',
            f'public struct {structure_name}',
            '{'
        )
        f.indent()

        current_offset = 0
        for attribute in attributes:
            input = attribute.input
            input_directive = directives.input_directives_by_input_name.get(input.name, InputDirective())
            cs_type = get_cs_type(input, input_directive)
            if attribute.directive.hint:
                f.write_line(f'[Hint("{make_cs_string_literal(attribute.directive.hint)}")]')
            f.write_line(f'[FieldOffset({current_offset})] public {cs_type} {input.name};')
            attribute.offset = current_offset
            current_offset += cs_sizes[cs_type]

        strides_by_structure[structure_name] = current_offset

        f.deindent()
        f.write_line('}', '')

        f.write_line(f'private const int VERT_BUFFER_INDEX_{structure_name} = {current_vert_buffer_index};', '')
        current_vert_buffer_index += 1

    # Output GetVertexAttributeDescriptors
    f.write_line(
        'public ReadOnlySpan<ShaderVertexAttributeDescriptor> VertexAttributeDescriptors => _vertexAttributeDescriptors;',
        ''
    )

    # Begin DefaultVertexStructureLayout child class
    f.write_line(
        f'public class DefaultVertexBufferLayout : IVertexBufferLayout<{class_name}>',
        '{',
        '    private readonly VblBufferDescriptor[] _bufferDescriptors;',
        '    private readonly VblAttributeDescriptor[] _attributeDescriptors;',
        '    public ReadOnlySpan<VblBufferDescriptor> BufferDescriptors => _bufferDescriptors;',
        '    public ReadOnlySpan<VblAttributeDescriptor> AttributeDescriptors => _attributeDescriptors;',
        ''
    )
    f.indent()
    
    # DefaultVertexStructureLayout child class Constructor
    f.write_line(
        'public DefaultVertexBufferLayout()',
        '{',
        '    _attributeDescriptors = new VblAttributeDescriptor[] {',
    )
    f.indent()
    f.indent()
    for structure_name, attributes in input_attributes_by_structure.items():
        for attribute in attributes:
            f.write_line(
                'new VblAttributeDescriptor() {',
                f'    AttributeIndex = {attribute.input.location},',
                f'    BufferOffset = {attribute.offset},',
                f'    BufferIndex = VERT_BUFFER_INDEX_{structure_name},',
                '},'
            )
    f.deindent()
    f.write_line(
        '};',
        '',
        '_bufferDescriptors = new VblBufferDescriptor[] {',
    )
    f.indent()
    for structure_name, attributes in input_attributes_by_structure.items():
        step_mode = 'PerVertex' if attributes[0].directive.step_mode == StepMode.PER_VERTEX else 'PerInstance'
        f.write_line(
            'new VblBufferDescriptor() {',
            f'    StepFunction = VertexStepFunction.{step_mode},',
            f'    Stride = {strides_by_structure[structure_name]},',
            f'    BufferType = typeof({structure_name})',
            '},'
        )
    f.deindent()
    f.write_line('};')
    f.deindent()
    f.write_line('}', '')
    
    # End DefaultVertexStructureLayout child class
    f.deindent()
    f.write_line(
        '    public static readonly DefaultVertexBufferLayout Instance = new();'
        '}',
        ''
    )

    # Begin fields
    global current_shader_id
    f.write_line(f'public static readonly int Id = {current_shader_id};\n')
    current_shader_id += 1
    
    # Write DescriptorInfo constants

    f.write_line('private static DescriptorInfo[] Descriptors = new DescriptorInfo[] {')
    f.indent()
    
    
            
    f.deindent()
    f.write_line(
        '};',
        '',
        'public ReadOnlySpan<DescriptorInfo> GetDescriptors()',
        '{',
        '    return Descriptors;',
        '}',
        ''
    )
    
    # DefaultVertexBufferAdapter class
    f.write_line(
        f'public class DefaultVertexBufferAdapter : IVertexBufferAdapter<{class_name}, DefaultVertexBufferLayout>',
        '{',
        f'    private readonly object[] _buffers = new object[{len(input_attributes_by_structure)}];',
        '',
        '    ReadOnlySpan<object?> IUntypedVertexBufferAdapter.VertexBuffers => _buffers;',
        ''
    )
    f.indent()
    for structure_name, attributes in input_attributes_by_structure.items():
        f.write_line(
            f'public void Set{structure_name}(IBuffer<{structure_name}> buffer)',
            '{',
            f'    _buffers[VERT_BUFFER_INDEX_{structure_name}] = buffer;',
            '}'
        )
    f.deindent()
    f.write_line('}', '')
    
    # Begin Shader Instance Class
    f.write_line(
        f'public class Instance<TVertexBufferLayout, TVertexBufferAdapter> : IShaderInstanceWithAdapter<{class_name}, TVertexBufferLayout, TVertexBufferAdapter>',
        f'    where TVertexBufferLayout : IVertexBufferLayout<{class_name}>',
        f'    where TVertexBufferAdapter : IVertexBufferAdapter<{class_name}, TVertexBufferLayout>',
        '{',
        '    private IShaderInstanceBacking _backing;'
    )
    f.indent()

    #
    # Declare variables for each descriptor set
    #
    descriptor_set_variable_names = []
    descriptor_set_indices_by_stage: Dict[str, Set[int]] = {}

    for stage, reflection in shader.reflections_by_stage.items():
        descriptor_set_indices: Set[int] = set()
        descriptor_set_indices.update(ubo.set for ubo in reflection.ubos)
        descriptor_set_indices.update(ssbo.set for ssbo in reflection.ssbos)
        descriptor_set_indices.update(texture.set for texture in reflection.textures)
        descriptor_set_indices_by_stage[stage] = descriptor_set_indices

        stage_name = 'vertex' if stage == ShaderStage.VERTEX else 'fragment'

        for set_index in descriptor_set_indices:
            var_name = f'_{stage_name}DescriptorSet{set_index}'
            f.write_line(f'private readonly IDescriptorSet {var_name};')
            descriptor_set_variable_names.append(var_name)

    f.write_line('', 'private readonly IDescriptorSet[] _descriptorSets;')

    f.write_line(
        '',
        'public IShaderInstanceBacking Backing => _backing;',
        '',
        'public ReadOnlySpan<IDescriptorSet> GetDescriptorSets()',
        '{',
        '    return new ReadOnlySpan<IDescriptorSet>(_descriptorSets);',
        '}',
        '',
        f'public TVertexBufferAdapter VertexBuffers;',
        '',
        f'IVertexBufferAdapter<{class_name}, TVertexBufferLayout> IShaderInstance<{class_name}, TVertexBufferLayout>.VertexBuffers => VertexBuffers;',
        'IUntypedVertexBufferAdapter IUntypedShaderInstance.VertexBufferAdapter => VertexBuffers;',
        f'TVertexBufferAdapter IShaderInstanceWithAdapter<{class_name}, TVertexBufferLayout, TVertexBufferAdapter>.Adapter => VertexBuffers;',
        ''
    )

    #
    # Generate ShaderInstanceConstructor
    #
    f.write_line(
        f'public Instance(IRenderer renderer, {class_name} shader, TVertexBufferAdapter buffers)',
        '{',
        '    _backing = renderer.CreateShaderInstanceBacking(shader);',
        f'    _descriptorSets = new IDescriptorSet[{len(descriptor_set_variable_names)}];',
        '    VertexBuffers = buffers;',
        ''
    )
    f.indent()

    # set_array_index = 0
    for stage, indices in descriptor_set_indices_by_stage.items():
        for index in indices:
            stage_name = 'vertex' if stage == ShaderStage.VERTEX else 'fragment'
            var_name = f'{stage_name}DescriptorSet{index}'
            hints_var_name = var_name + 'Hints'
            f.write_line(
                '// TODO: Actually fill out these hints',
                f'DescriptorSetCreationHints {hints_var_name} = new DescriptorSetCreationHints();',
                f'_{var_name} = renderer.CreateDescriptorSet(shader.Backing!, ShaderStage.{stage_name.capitalize()}, {index}, in {hints_var_name});',
                f'_descriptorSets[{index}] = _{var_name};'
            )
            # set_array_index += 1

    f.deindent()
    f.write_line('}', '')

    #
    # Generate ShaderInstance Dispose Method
    #
    f.write_line('public void Dispose()', '{', )
    f.indent()
    for var_name in descriptor_set_variable_names:
        f.write_line(f'{var_name}.Dispose();')
    f.deindent()
    f.write_line('}', '')
    
    #
    # Buffer Setters
    #

    def gen_buffer_input(input: BufferInput, type: str, stage: ShaderStage, method_name: str,
                         reflection: SpirvReflection, descriptor_index: int):
        stage_name = 'vertex' if stage == ShaderStage.VERTEX else 'fragment'

        if input.type[0] == '_':
            type_name = reflection.types[input.type].name
        else:
            type_name = spirv_to_cs_types[input.type]

        f.write_line(
            f'public void Set{input.name}(IBuffer<{type_name}> buffer)',
            '{',
            f'    _{stage_name}DescriptorSet{input.set}.{method_name}(buffer, in {class_name}.Descriptors[{descriptor_index}]);',
            '}',
            ''
        )

    descriptor_index = 0
    for stage, reflection in shader.reflections_by_stage.items():
        for ubo in reflection.ubos:
            gen_buffer_input(ubo, 'ubo', stage, 'SetUniformBufferDescriptor', reflection, descriptor_index)
            descriptor_index += 1

        for ssbo in reflection.ssbos:
            gen_buffer_input(ssbo, 'ssbo', stage, 'SetShaderStorageBufferDescriptor', reflection, descriptor_index)
            descriptor_index += 1

        for texture in reflection.textures:
            stage_name = 'vertex' if stage == ShaderStage.VERTEX else 'fragment'
            f.write_line(
                f'public void Set{texture.name}(ITexture texture)',
                '{',
                f'    _{stage_name}DescriptorSet{texture.set}.SetTextureDescriptor(texture, in {class_name}.Descriptors[{descriptor_index}]);',
                '}',
                '',
                f'public void Set{texture.name}Sampler(ISampler sampler)',
                '{',
                f'    _{stage_name}DescriptorSet{texture.set}.SetSamplerDescriptor(sampler, in {class_name}.Descriptors[{descriptor_index}]);',
                '}',
                ''
            )
            descriptor_index += 1

    # End Instance class
    f.deindent()
    f.write_line('}', '')

    # DefaultVertexLayoutInstance class
    f.write_line(
        'public class DefaultVertexLayoutInstance : Instance<DefaultVertexBufferLayout, DefaultVertexBufferAdapter>',
        '{',
        f'    public DefaultVertexLayoutInstance(IRenderer renderer, {class_name} shader)',
        '        : base(renderer, shader, new DefaultVertexBufferAdapter())',
        '    {}',
        '}',
        ''
    )

    # Close class and namespace
    f.deindent()
    f.write_line('}')
    f.deindent()
    f.write_line('}')


def gen_structure(f: SourceWriter, shader_type: ShaderType, reflection: SpirvReflection, hints: Dict[str, str]):
    size = -1
    for member in shader_type.members:
        if len(member.array_sizes) == 1 and member.array_sizes[0] == 0:
            size = max(size, member.offset + member.array_stride)

    if size > 0:
        f.write_line(f'[StructLayout(LayoutKind.Explicit, Size={size})]')
    else:
        f.write_line(f'[StructLayout(LayoutKind.Explicit)]')

    f.write_line(
        f'public struct {shader_type.name}',
        '{'
    )
    f.indent()

    for member in shader_type.members:
        if member.type[0] == '_':
            type_name = reflection.types[member.type].name
        else:
            type_name = spirv_to_cs_types[member.type]
        
        hint = hints.get(f'{shader_type.name}.{member.name}')
        if hint:
            f.write_line(f'[Hint("{make_cs_string_literal(hint)}")]')

        f.write_line(f'[FieldOffset({member.offset})] public {type_name} {member.name};')

    f.deindent()
    f.write_line('}')
    f.write_line('')


spirv_to_cs_types = {
    'float': 'float',
    'int': 'int',
    'uint': 'uint',
    'mat4': 'Matrix4x4',
    'ivec2': 'IntVector2',
    'vec2': 'Vector2',
    'vec3': 'Vector3',
    'vec4': 'Vector4'
}

buffer_type_to_cs_type = {
    'R8G8B8A8_UNORM': 'uint',
    'R8G8B8A8_SNORM': 'int'
}

cs_sizes = {
    'float': 4,
    'int': 4,
    'uint': 4,
    'Matrix4x4': 64,
    'IntVector2': 8,
    'Vector2': 8,
    'Vector3': 16,  # Arbitrary choice by me to add padding
    'Vector4': 16
}

spirv_to_default_buffer_types = {
    'int': 'R32_SINT',
    'uint': 'R32_UINT',
    'vec2': 'R32G32_SFLOAT',
    'vec3': 'R32G32B32_SFLOAT',
    'vec4': 'R32G32B32A32_SFLOAT'
}

buffer_type_to_mtlvertexformat = {
    'R8G8B8A8_UNORM': 'UChar4',
    'R8G8B8A8_SNORM': 'Char4',
    'R32_SINT': 'Int',
    'R32_UINT': 'UInt',
    'R32_SFLOAT': 'Float',
    'R32G32_SFLOAT': 'Float2',
    'R32G32B32_SFLOAT': 'Float3',
    'R32G32B32A32_SFLOAT': 'Float4',
    # 'float': 'Float',
    # 'int':   'Int',
    # 'uint':   'UInt',
    # 'IntVector2': 'Int2',
    # 'Vector2':  'Float2',
    # 'Vector3':  'Float3',
    # 'Vector4':  'Float4'
}


def get_cs_type(input: StageInput, directive: InputDirective) -> str:
    if directive.buffer_type:
        cs_type = buffer_type_to_cs_type[directive.buffer_type]
    else:
        cs_type = spirv_to_cs_types[input.type]
    return cs_type
