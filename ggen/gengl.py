from typing import List, Set, Dict
from xml.etree import ElementTree
from xml.etree.ElementTree import Element

GL_TARGET_MAJOR = 4
GL_TARGET_MINOR = 0

NAMESPACE = 'CeresGL'
OUTPUT_PATH = 'GL.Generated.cs'


class Feature(object):
    def __init__(self):
        self.commands: Set[str] = set()
        self.enums: Set[str] = set()


class Type(object):
    def __init__(self):
        self.group = ''
        self.name = ''
        
        self.indirection_count = 0
        
        # Is first level of indirection const qualified? This is used to guess which types should be used for interop
        # and for wrapping. For example, 'const GLchar *' will use immutable C# string,
        # while 'GLchar *' will use byte[].
        self.is_indirection_const = False


class CommandParameter(object):
    def __init__(self):
        self.type = Type()
        self.name = ''
        self.len = ''


class Command(object):
    def __init__(self):
        self.name = ''
        self.return_type = Type()
        self.parameters: List[CommandParameter] = []


class Enum(object):
    def __init__(self):
        self.name = ''
        self.value = ''
        self.type = ''
        self.groups: List[str] = []


def apply_feature(feature: Feature, element: Element):
    for require in element.findall('require'):
        feature.commands.update([command.attrib['name'] for command in require.findall('command')])
        feature.enums.update([enum.attrib['name'] for enum in require.findall('enum')])

    for remove in element.findall('remove'):
        feature.commands.difference_update([command.attrib['name'] for command in remove.findall('command')])
        feature.commands.difference_update([enum.attrib['name'] for enum in remove.findall('enum')])


def parse_type(element: Element) -> Type:
    type = Type()
    type.group = element.attrib.get('group', '')
    ptype = element.find('ptype')
    
    full_type_text = element.text or ''
    
    if ptype is not None:
        type.name = ptype.text.strip()
        full_type_text += ptype.text
        full_type_text += ptype.tail
    else:
        type.name = full_type_text.replace('const', '').replace('*', '').strip()
        
    type.indirection_count = full_type_text.count('*')
    type.is_indirection_const = full_type_text.startswith('const')

    return type


def parse_command(element: Element) -> Command:
    command = Command()
    proto = element.find('proto')
    name_elem = proto.find('name')
    command.return_type = parse_type(proto)
    command.name = name_elem.text.strip()

    for param_element in element.findall('param'):
        param = CommandParameter()
        param.type = parse_type(param_element)
        param.len = param_element.attrib.get('len', '')
        param.name = param_element.find('name').text.strip()
        command.parameters.append(param)

    return command


def parse_enum(element: Element) -> Enum:
    enum = Enum()
    enum.name = element.attrib['name']
    enum.value = element.attrib['value']
    groups = element.attrib.get('group', '').strip()
    if groups:
        enum.groups = groups.split(',')

    type = element.attrib.get('type')
    if type is None:
        enum.type = 'GLenum'
    else:
        if type == 'u':
            enum.type = 'GLuint'
        elif type == 'ull':
            enum.type = 'GLuint64'
        else:
            raise ValueError(f'Don\'t understand enum type specifier {type}')

    return enum


def main():
    tree = ElementTree.parse('gl.xml')
    commands_elem = tree.getroot().find('commands')
    feature_elems: List[Element] = tree.getroot().findall('feature')
    enums_elems: List[Element] = tree.getroot().findall('enums')

    feature = Feature()

    for feature_elem in feature_elems:
        if feature_elem.attrib['api'] != 'gl':
            continue

        major, minor = feature_elem.attrib['number'].split('.', maxsplit=2)
        if int(major) > GL_TARGET_MAJOR:
            continue

        if int(major) == GL_TARGET_MAJOR and int(minor) > GL_TARGET_MINOR:
            continue

        apply_feature(feature, feature_elem)

    commands: Dict[str, Command] = {}

    for command_elem in commands_elem.findall('command'):
        command = parse_command(command_elem)
        commands[command.name] = command

    #
    # Parse and organise enums
    #
    enums: Dict[str, Enum] = {}
    enums_by_group: Dict[str, List[Enum]] = {}

    for enums_element in enums_elems:
        for enum_element in enums_element.findall('enum'):
            enum = parse_enum(enum_element)
            enums[enum.name] = enum

            for group in enum.groups:
                group_list = enums_by_group.get(group)
                if group_list is None:
                    group_list = []
                    enums_by_group[group] = group_list
                group_list.append(enum)

    enum_groups = set(enums_by_group.keys())

    #
    # Generate Usings, Namespace Opening, and Class Opening
    #
    builder: List[str] = [f'''using System;
using System.CodeDom.Compiler;
using System.Runtime.InteropServices;
// ReSharper disable InconsistentNaming

namespace {NAMESPACE}
{{
''']

    #
    # Generate Enums classes
    #

    for group_name, enum_list in enums_by_group.items():
        builder.append(f'    {get_codegen_attribute()}\n    public enum {group_name} : uint\n    {{\n')

        for enum in enum_list:
            name = enum.name

            if name.startswith('GL_'):
                name = name[3:]
            if name[0].isdigit():
                name = '_' + name

            builder.append(f'        {name} = unchecked((uint){enum.value})')
            builder.append(',\n')
        builder.pop()
        builder.append('\n    }\n\n')

    #
    # Open the GL Class
    #
    builder.append(f'''    {get_codegen_attribute()}
    public partial class GL
    {{
''')

    #
    # Generate Commands delegates and fields
    #
    builder.append('        #pragma warning disable CS8618\n\n')

    for command_name in feature.commands:
        builder.extend(gen_command_field(commands[command_name]))

    builder.append('        #pragma warning restore CS8618\n\n')

    #
    # Open the Init() method.
    #
    builder.append('''
        public void Init(ILoader loader)
        {
''')

    #
    # Generate loading code inside the Init() method.
    #
    for command_name in feature.commands:
        builder.extend(gen_command_loading_code(commands[command_name]))

    #
    # Close the Init() method.
    #
    builder.append('        }\n\n')

    #
    # Generate wrapper methods
    #
    for command_name in feature.commands:
        builder.extend(gen_wrapper_method(enum_groups, commands[command_name]))

    #
    # Close the GL class, close the namespace. EOF.
    #
    builder.append('''
    }
}
''')

    with open(OUTPUT_PATH, 'w') as f:
        for part in builder:
            f.write(part)


def get_codegen_attribute() -> str:
    return '[GeneratedCode("gengl.py", "0")]'


METHOD_INDENT = ' ' * 12


def append_method_code(parts: List[str], *lines: str) -> None:
    for line in lines:
        parts.append(METHOD_INDENT)
        parts.append(line)
        parts.append('\n')


def gen_command_field(command: Command) -> List[str]:
    if command.return_type.indirection_count > 0:
        cs_return_type = 'IntPtr'
    else:
        cs_return_type = gl_primitive_to_cs_primitive(command.return_type.name)

    parts = [f'        public delegate {cs_return_type} {command.name}Delegate(']
    if len(command.parameters) > 0:
        for param in command.parameters:
            parts.append(f'{get_cs_interop_type(param.type)} {get_cs_parameter_name(param.name)}')
            parts.append(', ')

        parts.pop()

    parts.append(');\n')

    parts.append(f'        public {command.name}Delegate {command.name};\n\n')
    return parts


def gen_command_loading_code(command: Command) -> List[str]:
    parts = [f'            {command.name} = loader.GetProc<{command.name}Delegate>("{command.name}")!;\n']
    return parts


# =============================================================================
#
# Wrapper Method Generation Code
#
# =============================================================================


def gen_wrapper_method(enum_groups: Set[str], command: Command) -> List[str]:
    name = command.name
    if name.startswith('gl'):
        name = name[2:]

    has_return_value = not (command.return_type.name == 'void' and command.return_type.indirection_count == 0)

    #
    # Ensure that this is a wrapper we are capable of generating
    #
    for param in command.parameters:
        if param.type.indirection_count > 1:
            print(f'Wrapper Gen: Cannot generate wrapper {name}: Not supporting parameters with multiple levels of '
                  'indirection yet.')
            return []

    #
    # Begin wrapper method opening
    #
    cs_return_type = get_cs_wrapper_type(enum_groups, command.return_type, is_return_value=True)
    parts = [f'        public unsafe {cs_return_type} {name}(']

    #
    # Generate wrapper method parameters
    #
    if len(command.parameters) > 0:
        for param in command.parameters:
            wrapper_type = get_cs_wrapper_type(enum_groups, param.type)
            parameter_name = get_cs_parameter_name(param.name)
            parts.append(f'{wrapper_type} {parameter_name}')
            parts.append(', ')

        parts.pop()

    #
    # Finish wrapper method signature and open wrapper method body
    #
    parts.append(')\n        {\n')
    
    #
    # Declare return value
    #
    if has_return_value:
        parts.append(f'{get_cs_interop_type(command.return_type)} rv;')

    #
    # Generate parameter validation code
    #
    for param in command.parameters:
        gen_pointer_argument_validation(parts, param, command)

    #
    # Generate parameter translation code
    #
    for param in command.parameters:
        gen_argument_prep(parts, param)

    #
    # Generate command call
    #
    parts.append(METHOD_INDENT)

    # If the command returns a value, prepend return value assignment to the call.
    if has_return_value:
        parts.append(f'rv = ')

    parts.append(f'{command.name}(')
    if len(command.parameters) > 0:
        for param in command.parameters:
            parts.append(get_wrapper_argument_for_command(enum_groups, param))
            parts.append(', ')
        parts.pop()
    parts.append(');\n')

    #
    # Generate teardown code for any parameter translation
    #
    for param in command.parameters:
        gen_pointer_argument_teardown(parts, param)

    #
    # If the command returned a value, marshal and return that value.
    #
    if has_return_value:
        gen_return_value_marshall(parts, enum_groups, command.return_type)
        # parts.append(f'{METHOD_INDENT}return rv;\n')

    #
    # Close wrapper method body
    #
    parts.append('        }\n\n')

    return parts


def gen_pointer_argument_validation(parts: List[str], param: CommandParameter, command: Command) -> None:
    if not param.len:
        return

    # try:
    parts_to_multiply = param.len.split('*')
    plen = ' * '.join(get_length_expression(param.name, plen_part, command) for plen_part in parts_to_multiply)
    
    # except ValueError as e:
    #     print(f'Validation Gen: Skipping validation for param {param.name} of command {command.name}: {e}')
    #     return

    exception_message = f'Span must contain at least {plen} elements'

    append_method_code(parts,
                       f'if ({get_cs_parameter_name(param.name)}.Length < {plen}) {{',
                       f'    throw new ArgumentOutOfRangeException("{exception_message}", "{param.name}");',
                       '}'
                       )


def get_length_expression(parameter_name: str, plen: str, command: Command) -> str:
    plen_num = None
    try:
        plen_num = int(plen)
    except ValueError:
        pass

    if plen_num is not None:
        return plen

    compsize_beginning = 'COMPSIZE('
    if plen.startswith(compsize_beginning):
        end_index = plen.rfind(')')
        compsize_inners = plen[len(compsize_beginning):end_index]
        
        return f'Compsize.{command.name}_{parameter_name}({compsize_inners})'
        
        # raise ValueError(f'Don\'t understand "{plen}"')
        # inners = plen[len(compsize_beginning):-1]
        # compsize_parts = inners.split(',')
        # if len(compsize_parts) > 1:
        #     parts: List[str] = ['(']
        #     for part in compsize_parts:
        #         parts.append('(int)')
        #         parts.append(get_cs_parameter_name(part))
        #         parts.append(' * ')
        #     parts.pop()
        #     parts.append(')')
        #     return ''.join(parts)
        # else:
        #     # If only one part, then this is a special case length. Not supporting those yet.
        #     raise ValueError(f'Don\'t understand "{plen}"')
    else:
        # Find the corresponding parameter
        corresponding_param = None
        for other_param in command.parameters:
            if other_param.name == plen:
                corresponding_param = other_param

        if corresponding_param is None:
            raise ValueError(f'Don\'t understand "{plen}"')

        return corresponding_param.name


def gen_argument_prep(parts: List[str], param: CommandParameter) -> None:
    if param.type.indirection_count == 0:
        return

    cs_parameter_name = get_cs_parameter_name(param.name)

    # TODO: This is a hack, need to parse modifiers and pointer syntax
    # TODO: NEED TO USE TRY FINALLY HERE
    if 'GLchar' in param.type.name and param.type.is_indirection_const:
        append_method_code(parts, f'IntPtr {param.name}_ptr = Marshal.StringToHGlobalAnsi({cs_parameter_name});')
        return

    ptr_name = f'{param.name}_ptr'

    append_method_code(parts,
                       f'fixed (void* {ptr_name} = {cs_parameter_name}) {{')

    # handle_name = f'{param.name}_gchandle'
    # append_method_code(parts,
    #                    f'GCHandle {handle_name} = GCHandle.Alloc({cs_parameter_name}, GCHandleType.Pinned);',
    #                    f'IntPtr {param.name}_ptr = {handle_name}.AddrOfPinnedObject();')


def get_wrapper_argument_for_command(enum_groups: Set[str], param: CommandParameter) -> str:
    if param.type.indirection_count > 0:
        return f'(IntPtr){param.name}_ptr'

    cs_parameter_name = get_cs_parameter_name(param.name)

    wrapper_type = get_cs_wrapper_type(enum_groups, param.type)
    interop_type = get_cs_interop_type(param.type)
    if wrapper_type != interop_type:
        return f'({interop_type}){cs_parameter_name}'

    return cs_parameter_name


def gen_pointer_argument_teardown(parts: List[str], param: CommandParameter) -> None:
    if param.type.indirection_count == 0:
        return

    # TODO: This is a hack, need to parse modifiers and pointer syntax
    # TODO: NEED TO USE TRY FINALLY HERE
    if 'GLchar' in param.type.name and param.type.is_indirection_const:
        append_method_code(parts, f'Marshal.FreeHGlobal({param.name}_ptr);')
        return

    append_method_code(parts, '}')
    # append_method_code(parts, f'{param.name}_gchandle.Free();')


def gen_return_value_marshall(parts: List[str], enum_groups: Set[str], type: Type):
    if type.group == 'String':
        append_method_code(parts, 'return Marshal.PtrToStringAnsi(rv);')
        return

    if type.group in enum_groups and type.group != 'Boolean':
        append_method_code(parts, f'return ({type.group})rv;')
        return

    append_method_code(parts, 'return rv;')
    

# =============================================================================
#
# Type Translation Code
#
# =============================================================================

def gl_primitive_to_cs_primitive(gl_type_name: str) -> str:
    """
    Translates a primitive GL type to a primitive C# type.
    """
    if gl_type_name == 'void':
        return 'void'
    if gl_type_name == 'GLint':
        return 'int'
    if gl_type_name == 'GLuint':
        return 'uint'
    if gl_type_name == 'GLboolean':
        return 'bool'
    if gl_type_name == 'GLenum':
        return 'uint'
    if gl_type_name == 'GLenum':
        return 'uint'
    if gl_type_name == 'GLsizei':
        return 'int'
    if gl_type_name == 'GLchar':
        return 'byte'
    if gl_type_name == 'GLfloat':
        return 'float'
    if gl_type_name == 'GLdouble':
        return 'double'
    if gl_type_name == 'GLintptr':
        return 'IntPtr'
    if gl_type_name == 'GLsizeiptr':
        return 'IntPtr'
    if gl_type_name == 'GLbitfield':
        return 'uint'
    if gl_type_name == 'GLbyte':
        return 'byte'
    if gl_type_name == 'GLubyte':
        return 'byte'
    if gl_type_name == 'GLshort':
        return 'short'
    if gl_type_name == 'GLushort':
        return 'ushort'
    if gl_type_name == 'GLint64':
        return 'long'
    if gl_type_name == 'GLuint64':
        return 'ulong'

    if gl_type_name == 'GLsync':
        return 'IntPtr'

    # if gl_type_name == 'const void *' or gl_type_name == 'void *':
    #     return 'IntPtr'
    # if gl_type_name == 'const void *const*' or gl_type_name == 'void **':
    #     return 'IntPtr'

    raise ValueError(f'Don\'t know how to translate GL primitive {gl_type_name} to C# primitive.')


def get_cs_interop_type(type: Type) -> str:
    """
    Returns the C# type to use as a parameter in a native function binding for the given command parameter data.
    """
    if type.indirection_count > 0:
        return 'IntPtr'
    return gl_primitive_to_cs_primitive(type.name)


def get_cs_wrapper_type(enum_groups: Set[str], type: Type, is_return_value=False) -> str:
    """
    Returns the C# type to use in a wrapper method's parameter for the given command parameter data.
    """
    if type.name == 'GLsizeiptr':
        # Keep things simple on the C# size and keep sizes a constant 32 bits.
        return 'uint'

    if type.group == 'String' or type.name == 'GLchar' and type.indirection_count == 1 and type.is_indirection_const:
        return 'string'

    if type.group in enum_groups and type.group not in ['Boolean']:
        base_type = type.group
    else:
        base_type = gl_primitive_to_cs_primitive(type.name)

    if type.indirection_count > 0:
        if is_return_value:
            # We don't support marshalling non-string pointers for return values yet.
            return 'IntPtr'
        if type.name == 'void':
            base_type = 'byte'
        return f'Span<{base_type}>'

    return base_type


def get_cs_parameter_name(parameter_name: str) -> str:
    """
    Returns a parameter name based on the given parameter name that is suitable for C#.
    Parameter names that are reserved C# keywords are appended with an underscore.
    """
    if parameter_name == 'ref' or parameter_name == 'params' or parameter_name == 'string':
        return parameter_name + '_'
    return parameter_name


if __name__ == '__main__':
    main()
