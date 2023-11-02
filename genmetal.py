import os.path
import re
import subprocess
from typing import Match, List, TextIO, Tuple, Optional

NAMESPACE = 'CeresGpu.MetalBinding'


PROTOTYPE_PATTERN = re.compile(r'^(?P<returntype>\S+)\s+(?P<name>\w+)\s*\((?P<params>.*)\).*;')
ENUM_TYPE_PATTERN = re.compile(r'^\s*typedef NS_ENUM\((?P<backing>.*),\s*(?P<name>.*)\)')
ENUM_PATTERN = re.compile(r'^\s*(?P<name>\w+).*=\s*(?P<value>\d+)')


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


def main():
    root = os.path.normpath(os.path.join(__file__, '..'))

    xcode_select_result = subprocess.run(['xcode-select', '-p'], check=True, stdout=subprocess.PIPE, encoding='utf-8')
    developer_dir = xcode_select_result.stdout.strip()
    metal_headers_path = developer_dir + '/Platforms/MacOSX.platform/Developer/SDKs/MacOSX.sdk/System/Library/' \
                                         'Frameworks/Metal.framework/Headers'
    
    header_path = os.path.join(root, 'metalbinding', 'metalbinding.h')
    cs_out_path = os.path.join(root, 'CeresGpu', 'MetalBinding', 'Metal.Generated.cs')
    os.makedirs(os.path.dirname(cs_out_path), exist_ok=True)
    
    with open(header_path) as f:
        lines = f.read().split('\n')
        
    prototypes = []
        
    for line in lines:
        if line.lstrip().startswith('//'):
            continue
    
        prototype_match = PROTOTYPE_PATTERN.match(line)
        if prototype_match:
            prototypes.append(parse_prorotype(prototype_match))
    
    enums = [
        parse_enum_header(os.path.join(metal_headers_path, 'MTLPixelFormat.h'), 'MTLPixelFormat'),
        parse_enum_header(os.path.join(metal_headers_path, 'MTLRenderPipeline.h'), 'MTLBlendOperation'),
        parse_enum_header(os.path.join(metal_headers_path, 'MTLRenderPipeline.h'), 'MTLBlendFactor'),
        parse_enum_header(os.path.join(metal_headers_path, 'MTLDepthStencil.h'), 'MTLCompareFunction'),
        parse_enum_header(os.path.join(metal_headers_path, 'MTLDepthStencil.h'), 'MTLStencilOperation'),
        parse_enum_header(os.path.join(metal_headers_path, 'MTLVertexDescriptor.h'), 'MTLVertexFormat'),
        parse_enum_header(os.path.join(metal_headers_path, 'MTLVertexDescriptor.h'), 'MTLVertexStepFunction'),
        parse_enum_header(os.path.join(metal_headers_path, 'MTLStageInputOutputDescriptor.h'), 'MTLIndexType'),
        parse_enum_header(os.path.join(metal_headers_path, 'MTLRenderCommandEncoder.h'), 'MTLCullMode'),
        parse_enum_header(os.path.join(metal_headers_path, 'MTLSampler.h'), 'MTLSamplerMinMagFilter'),
        parse_enum_header(os.path.join(metal_headers_path, 'MTLSampler.h'), 'MTLSamplerMipFilter'),
        parse_enum_header(os.path.join(metal_headers_path, 'MTLRenderPass.h'), 'MTLLoadAction'),
        parse_enum_header(os.path.join(metal_headers_path, 'MTLRenderPass.h'), 'MTLStoreAction')
    ]
    
    with open(cs_out_path, 'w') as f:
        writer = SourceWriter(f)
        gen_cs_file(writer, prototypes, enums)

    
class FunctionParameter(object):
    def __init__(self, typename: str, name: str):
        self.typename = typename
        self.name = name
    
    
class FunctionPrototype(object):
    def __init__(self, return_type: str, name: str, params: List[FunctionParameter]):
        self.return_type = return_type
        self.name = name
        self.params = params
        
        
def parse_param(text: str) -> FunctionParameter:
    parts = [p for p in text.split(' ') if p and not p.isspace()]
    
    typename = ''
    
    for part in parts[:-1]:
        typename += part
        if part != 'const':
            break
        typename += ' '
    
    name = parts[-1]
    return FunctionParameter(typename, name)
    

def parse_prorotype(match: Match) -> FunctionPrototype:
    return_type = match.group('returntype')
    name = match.group('name')
    param_text = match.group('params')
    
    param_strings = [p.strip() for p in param_text.split(',')]
    
    if len(param_strings) == 1 and param_strings[0].strip() == 'void':
        params = []
    else:
        params = [parse_param(p) for p in param_strings]
    
    return FunctionPrototype(return_type, name, params)


class EnumData(object):
    def __init__(self, name: str, backing_type: str):
        self.name = name
        self.backing_type = backing_type
        self.entries: List[Tuple[str, str]] = []


def parse_enum_header(path: str, prefix: str) -> Optional[EnumData]:
    with open(path) as f:
        lines = f.read().split('\n')
        
    data: Optional[EnumData] = None
    current_enum = ''
        
    for line in lines:
        if data and current_enum == prefix:
            match = ENUM_PATTERN.match(line)
            if match:
                name = match.group('name')
                if name.startswith(prefix):
                    name = name[len(prefix):]
                data.entries.append((name, match.group('value')))
                continue
            
        match = ENUM_TYPE_PATTERN.match(line)
        if match:
            name = match.group('name')
            backing = match.group('backing')
            current_enum = name
            if name == prefix and not data:
                data = EnumData(name, backing)
            continue
            
        if line.strip().startswith('}'):
            current_enum = ''
            
    return data


def gen_cs_file(f: SourceWriter, prototypes: List[FunctionPrototype], enums: List[EnumData]):
    f.write_line(
        'using System;',
        'using System.CodeDom.Compiler;',
        'using System.Runtime.InteropServices;',
        '',
        f'namespace {NAMESPACE}',
        '{'
    )
    f.indent()
    f.write_line(
        '[GeneratedCode("genmetal.py", "0")]',
        'public class MetalApi',
        '{'
    )
    f.indent()
    f.write_line('private const string DLL_NAME = "metalbinding";', '')
    
    for prototype in prototypes:
        gen_cs_extern(f, prototype)
        
    for enum in enums:
        f.write_line(f'public enum {enum.name} : {get_cs_type_primitive(enum.backing_type)}', '{')
        f.indent()
        
        for entry_name, value in enum.entries:
            f.write_line(f'{entry_name} = {value},')
        
        f.deindent()
        f.write_line('}', '')
    
    f.deindent()
    f.write_line('}')  # End class
    f.deindent()
    f.write_line('}')  # End namespace


def gen_cs_extern(f: SourceWriter, prototype: FunctionPrototype):
    cs_return_type = get_cs_type(prototype.return_type, '')
    
    cs_params = []
    for param in prototype.params:
        cs_param_type = get_cs_type(param.typename, param.name)
        cs_params.append(f'{cs_param_type} {param.name}')
    
    params_text = ', '.join(cs_params)
    
    f.write_line('[DllImport(DLL_NAME)]')
    f.write_line(f'public static extern {cs_return_type} {prototype.name}({params_text});', '')


def get_cs_type(typename: str, var_name: str):
    if typename == 'const char*':
        return 'string'
    
    if typename.endswith('*') and var_name.startswith('ref_'):
        return 'ref ' + get_cs_type_primitive(typename[:-1])
    
    if typename.endswith('*') or typename.startswith('id<'):
        return 'IntPtr'
    return get_cs_type_primitive(typename)


def get_cs_type_primitive(typename: str):
    if typename == 'BOOL':
        return 'bool'
    if typename == 'int32_t':
        return 'int'
    if typename == 'uint32_t':
        return 'uint'
    if typename == 'int64_t':
        return 'long'
    if typename == 'uint64_t':
        return 'ulong'
    if typename == 'NSUInteger':  # TODO: Make the binding interface use constant size types and remove support for this.
        return 'ulong'  # Assuming no 64-bit. Doing this because c# doesn't allow IntPtr backed enums
    return typename


if __name__ == '__main__':
    main()
