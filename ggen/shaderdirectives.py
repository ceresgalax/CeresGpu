"""
This module contains types and methods for parsing "Shader Directives" from shader source files.
Such shader directives are usually defined in comments at the top of shader source files (glsl)
and define additional metadata used by the shader source generation process.  
"""
import re
from enum import Enum, auto
from typing import Dict


class StepMode(Enum):
    PER_VERTEX = auto()
    PER_INSTANCE = auto()


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
        self.descriptor_hints_by_name: Dict[str, str] = {}
        self.field_hints_by_name: Dict[str, str] = {}


directive_pattern = re.compile(r'\/\/\s*#(\w+):\s*([^\n]+)')
input_pattern = re.compile(r'layout\s*\(.*\)\s*in\s*\w+\s*(\w+);\s*\/\/\s*#input\s*(.*)')


def parse_shader_directives(path: str) -> ShaderDirectives:
    data = ShaderDirectives()

    with open(path, 'r', encoding='utf-8') as f:
        while True:
            line = f.readline()
            if not line:
                break

            directive_match = directive_pattern.search(line)
            if directive_match:
                parse_directive(data, directive_match)

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


def parse_directive(data: ShaderDirectives, directive_match: re.Match[str]):
    directive_name = directive_match.group(1)
    directive_value = directive_match.group(2)

    if directive_name == 'CSNAME':
        data.full_class_name = directive_value.strip()

    elif directive_name == 'DescriptorHint':
        name, hint = [part.strip() for part in directive_value.strip().split('=', maxsplit=2)]
        data.descriptor_hints_by_name[name] = hint

    elif directive_name == 'FieldHint':
        name, hint = [part.strip() for part in directive_value.strip().split('=', maxsplit=2)]
        data.field_hints_by_name[name] = hint