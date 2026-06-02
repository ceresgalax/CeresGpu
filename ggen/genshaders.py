import os
from enum import Enum, auto
from typing import List, Dict, Any, Optional, Tuple

from .csutil import SourceWriter, make_cs_string_literal
from .shaderdirectives import ShaderDirectives, InputDirective, StepMode
from .spvreflection import SpirvReflection, StageInput, BufferInput, ArgumentBufferBinding, TextureInput, ShaderType


class ShaderStage(Enum):
    VERTEX = auto()
    FRAGMENT = auto()


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


current_shader_id = 1


def generate_shader_cs_file(output_path: str, shader: Shader):
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
        generate_shader_cs_class(SourceWriter(f), shader)


def generate_shader_cs_class(f: SourceWriter, shader: Shader):
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
    
    # Figure out flattened arg buffer indices for metal impl
    abstracted_argbuffer_indices_by_name = {}
    flattened_arg_buffers: Dict[Any, int] = {} 
    
    for stage, reflection in shader.reflections_by_stage.items():
        descriptors: List[Tuple[str, int]] = []  # (name, set)
        descriptors.extend(((ubo.name, ubo.set) for ubo in reflection.ubos))
        descriptors.extend(((ssbo.name, ssbo.set) for ssbo in reflection.ssbos))
        descriptors.extend(((tex.name, tex.set) for tex in reflection.textures))
            
        for name, set in descriptors:
            key = (stage, set)
            index = flattened_arg_buffers.get(key)
            if index is None:
                index = len(flattened_arg_buffers)
                flattened_arg_buffers[key] = index

            abstracted_argbuffer_indices_by_name[name] = index
            
    print(abstracted_argbuffer_indices_by_name)
    print(flattened_arg_buffers)

    def get_cs_shader_stage(stage: ShaderStage) -> str:
        return 'ShaderStage.Vertex' if stage == ShaderStage.VERTEX else 'ShaderStage.Fragment'

    def write_descriptor_info_field_for_buffer(stage: ShaderStage, buffer: BufferInput, reflection: SpirvReflection, cs_descriptor_type: str):

        # Figure out what the metal argument buffer binding index is 
        # (Note: It's possible for the argument buffer binding to be compiled out.)
        def get_argument_buffer() -> Optional[ArgumentBufferBinding]:
            for abb in reflection.arg_buffer_bindings:
                # For some reason spirv-cross reflects the Uniform typename as the name.
                print(f'{buffer.name} -- {abb.typename}, {abb.name}, {abb.index}')
                if abb.typename == buffer.name:
                    return abb
            return None

        if buffer.type[0] == '_':
            type_name = reflection.types[buffer.type].name
        else:
            type_name = spirv_to_cs_types[buffer.type]

        abb = get_argument_buffer()

        hint = shader.directives.descriptor_hints_by_name.get(buffer.name, '')

        f.write_line(
            'new DescriptorInfo {',
            '    Binding = MakeBinding(',
            '        backend,',
            f'        gl: new GLDescriptorBindingInfo {{ Location = {buffer.binding} }},',  
            f'        metal: new MetalDescriptorBindingInfo {{',
            f'            FunctionArgumentBufferIndex = {buffer.set},',
            f'            AbstractedBufferIndex = {abstracted_argbuffer_indices_by_name[buffer.name]},',
            f'            Stage = {get_cs_shader_stage(stage)},',
            f'            BufferId = {"null" if abb is None else abb.index}',
            f'        }},',
            f'        vulkan: new VulkanDescriptorBindingInfo {{ Set = {buffer.set}, Binding = {buffer.binding} }}',
            '    ),',
            f'    DescriptorType = DescriptorType.{cs_descriptor_type},',
            f'    BufferType = typeof({type_name}),',
            f'    Name = "{make_cs_string_literal(buffer.name)}",',
            f'    Hint = "{make_cs_string_literal(hint)}"'
            '},'
        )

    def write_descriptor_info_field_for_texture(stage: ShaderStage, texture: TextureInput, reflection: SpirvReflection):
        # Figure out what the metal argument buffer binding index is
        def get_binding_index(name: str):
            for abb in reflection.arg_buffer_bindings:
                if abb.name == name:
                    return abb.index
            return 0

        texture_binding = get_binding_index(texture.name)
        sampler_binding = get_binding_index(texture.name + 'Smplr')

        hint = shader.directives.descriptor_hints_by_name.get(texture.name, '')

        f.write_line(
            'new DescriptorInfo {',
            '    Binding = MakeBinding(',
            '        backend,',
            f'        gl: new GLDescriptorBindingInfo {{ Location = {texture.binding} }},',
            f'        metal: new MetalDescriptorBindingInfo {{',
            f'            FunctionArgumentBufferIndex = {texture.set},',
            f'            AbstractedBufferIndex = {abstracted_argbuffer_indices_by_name[texture.name]},',
            f'            Stage = {get_cs_shader_stage(stage)},',
            f'            BufferId = {texture_binding},',
            f'            SamplerBufferId = {sampler_binding},',
            f'        }},',
            f'        vulkan: new VulkanDescriptorBindingInfo {{ Set = {texture.set}, Binding = {texture.binding} }}',
            '    ),',
            '    DescriptorType = DescriptorType.Texture,',
            f'    Name = "{make_cs_string_literal(texture.name)}",',
            f'    Hint = "{make_cs_string_literal(hint)}",',
            '},'
        )

    for stage, reflection in shader.reflections_by_stage.items():
        for ubo in reflection.ubos:
            write_descriptor_info_field_for_buffer(stage, ubo, reflection, 'UniformBuffer')

        for ssbo in reflection.ssbos:
            write_descriptor_info_field_for_buffer(stage, ssbo, reflection, 'ShaderStorageBuffer')

        for texture in reflection.textures:
            write_descriptor_info_field_for_texture(stage, texture, reflection)
    
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
            gen_structure(f, type, reflection, shader.directives.field_hints_by_name)

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
            f'    StepRate = 1,',
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
        f'public class Instance : IShaderInstance<{class_name}>',
        '{',
    )
    f.indent()

    f.write_line(
        'private IShaderInstanceBacking _backing;',
        '',
        'public IShaderInstanceBacking Backing => _backing;',
        '',
    )

    #
    # Generate ShaderInstanceConstructor
    #
    f.write_line(
        f'public Instance(IRenderer renderer, {class_name} shader)',
        '{',
        '    _backing = renderer.CreateShaderInstanceBacking(shader);',
        '}',
        ''
    )

    #
    # Generate ShaderInstance Dispose Method
    #
    f.write_line(
        'public void Dispose()',
        '{',
        '    _backing.Dispose();',
        '}',
        ''
    )
    
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
            f'    _backing.{method_name}(buffer, in {class_name}.Descriptors[{descriptor_index}]);',
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
                f'    _backing.SetTextureDescriptor(texture, in {class_name}.Descriptors[{descriptor_index}]);',
                '}',
                '',
                f'public void Set{texture.name}Sampler(ISampler sampler)',
                '{',
                f'    _backing.SetSamplerDescriptor(sampler, in {class_name}.Descriptors[{descriptor_index}]);',
                '}',
                ''
            )
            descriptor_index += 1

    # End Instance class
    f.deindent()
    f.write_line('}', '')

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
    'Vector3': 12,
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
