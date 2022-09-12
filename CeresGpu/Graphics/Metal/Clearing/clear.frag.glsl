// #CSNAME:Metalancer.Graphics.Metal.Clearing.ClearShader
// #CSFIELD:Fragment
#version 450

// TODO: PUSH CONSTANTS
layout(binding = 0) uniform FragUniforms {
    vec4 clearColor;
} u; 

layout(location = 0) out vec4 frag_color;

void main()
{
    frag_color = u.clearColor; 
}
