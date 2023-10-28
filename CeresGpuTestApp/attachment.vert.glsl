// #CSNAME:CeresGpuTestApp.AttachmentTestShader
// #CSFIELD:Vertex
#version 450 


layout(location = 0) in vec2 vert_pos;

layout(binding = 0) uniform VertUniforms {
    float scale;
} u;

// uniform vec4 uvs;
// uniform mat4 mvp; // #hint:modelToClip

layout(location = 0) out vec4 color;
layout(location = 1) out vec2 uv;

void main()
{   
/*
    if (gl_VertexIndex == 0) {
        gl_Position = vec4(-0.5, -0.5, 0.5, 1.0);
        color = vec4(1,0,0,1);
    }
    if (gl_VertexIndex == 1) {
        gl_Position = vec4(0, 0.5, 0.5, 1.0);
        color = vec4(0,1,0,1);
    }
    if (gl_VertexIndex == 2) {
        gl_Position = vec4(0.5, -0.5, 0.5, 1.0);
        color = vec4(0,0,1,1);
    }
*/
    gl_Position = vec4(vert_pos.xy * u.scale, 0.0f, 1.0f);
    color = vec4(vert_pos.xy, vert_pos.y, 1.0f);
    uv = vert_pos;
}
