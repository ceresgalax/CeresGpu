"""
This module contains helpful functions and types for generating C# source files.
"""
from typing import TextIO, List


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