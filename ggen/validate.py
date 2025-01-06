from typing import Dict, Set

from .genshaders import Shader


def validate_descriptor_set_bindings(shader: Shader):
    """
    Validate that `layout(set=X, binding=Y)` has been set correctly in the source shader.
    :param shader: 
    :return: 
    """ 
    
    bindings_by_set: Dict[int, Set[int]] = {}
    
    def add_binding_or_throw(descriptor_set: int, binding: int):
        print(f'descriptor_set = {descriptor_set}, binding = {binding}')
        bindings_in_set = bindings_by_set.setdefault(descriptor_set, set())
        if binding in bindings_in_set:
            raise ValueError(f'Conflicting binding: There is already a binding {binding} in set {descriptor_set}')
        bindings_in_set.add(binding)
    
    for reflection in shader.reflections_by_stage.values():
        for ubo in reflection.ubos:
            add_binding_or_throw(ubo.set, ubo.binding)
        for ssbo in reflection.ssbos:
            add_binding_or_throw(ssbo.set, ssbo.binding)
        for tex in reflection.textures:
            add_binding_or_throw(tex.set, tex.binding)
    
    # If we got here, we're all good to go!
