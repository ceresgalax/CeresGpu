"""
This module contains types and methods for parsing SpirV-cross reflection json
"""
from typing import List, Dict, Any, Optional


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


def parse_member(data: Dict[str, Any]) -> Member:
    print(repr(data))
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