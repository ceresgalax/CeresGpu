import os
from typing import List, Set, Tuple, Iterable


class Node(object):
    def __init__(self, filepath: str, action: str, inputs: List[Tuple[str, 'Node']]):
        self.filepath = filepath
        self.action = action
        self.tagged_inputs = inputs
        
    def get_input_nodes(self) -> Iterable['Node']:
        return (input for tag, input in self.tagged_inputs)
    
    def get_inputs_with_tag(self, tag: str) -> Iterable['Node']:
        return (input for t, input in self.tagged_inputs if tag == t)

    
class Graph(object):
    def __init__(self):
        self.root_nodes: List[Node] = []
        
    def find_dirty_nodes(self, min_modtime: int) -> Set[Node]:
        dirty_nodes: Set[Node] = set()
        
        stack: List[Node] = list(self.root_nodes)
        
        visited_nodes: Set[Node] = set()
        
        while len(stack) > 0:
            node = stack[-1]

            if node not in visited_nodes:
                visited_nodes.add(node)
                stack.extend(node.get_input_nodes())
                continue
            
            stack.pop()
                
            # Check if any of the inputs are dirty
            is_dirty = False
            for input in node.get_input_nodes():
                if input in dirty_nodes:
                    is_dirty = True
                    break
            
            # If this is a source file node (we can tell since it has no inputs), assert that the file exists.
            if len(node.tagged_inputs) == 0 and not os.path.isfile(node.filepath):
                raise ValueError(f'Missing source file {node.filepath}')
                
            # Inputs are clean, check if this node itself is dirty based on inputs
            if not is_dirty:
                if not os.path.isfile(node.filepath):
                    is_dirty = True
                else:
                    output_modtime = os.stat(node.filepath).st_mtime_ns
                    for input in node.get_input_nodes():
                        # Note: Could memoize the modtime here
                        if not os.path.isfile(input.filepath):
                            is_dirty = True
                            break
                        else:
                            input_modtime = max(os.stat(input.filepath).st_mtime_ns, min_modtime)
                            if input_modtime >= output_modtime:
                                is_dirty = True
                                break
                
            if is_dirty:
                dirty_nodes.add(node)
        
        return dirty_nodes
    
    def walk(self) -> Iterable[Node]:
        stack: List[Node] = list(self.root_nodes)
        
        visited_nodes = set()
        
        while len(stack) > 0:
            node = stack[-1]
            
            if node in visited_nodes:
                yield stack.pop()
            else:
                stack.extend(node.get_input_nodes())
                visited_nodes.add(node)
