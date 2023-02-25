// #CSNAME:CeresGpu.Graphics.Metal.Clearing.ClearShader
// #CSFIELD:Vertex
#version 450 

const vec4 verts[6] = vec4[6](
    vec4(-1, -1, 0, 1),
    vec4(-1,  1, 0, 1),
    vec4( 1,  1, 0, 1),
    vec4(-1, -1, 0, 1),
    vec4( 1,  1, 0, 1),
    vec4( 1, -1, 0, 1)
);

void main()
{   
    gl_Position = verts[gl_VertexIndex];
}
