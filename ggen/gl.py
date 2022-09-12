
def get_byte_size_of_vertex_attrib_type(gl_type: str) -> int:
    if gl_type == 'GL_FLOAT':
        return 4
    
    if gl_type == 'GL_UNSIGNED_BYTE':
        return 1

    raise ValueError(f'Don\'t know byte size of GL vertex attrib type {gl_type}')


def get_cs_type_for_vertex_attrib_type(gl_type: str) -> str:
    if gl_type == 'GL_FLOAT':
        return 'float'
    
    if gl_type == 'GL_UNSIGNED_BYTE':
        return 'byte'

    raise ValueError(f'Don\'t know what C# type to use for vertex attrib type {gl_type}')


def get_abbreviated_enum_name(name: str) -> str:
    if name.startswith('GL_'):
        name = name[3:]
    if name[0].isdigit():
        name = '_' + name
    return name
