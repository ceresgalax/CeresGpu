// #CSNAME:CeresGpuTestApp.AttachmentTestShader
// #CSFIELD:Fragment
#version 450

layout(location = 1) in vec2 uv;

layout(input_attachment_index = 0, location = 0, binding = 0)uniform subpassInput tex;

layout(location = 0) out vec4 frag_color;

void main()
{
    frag_color = subpassLoad(tex); //texture(tex, uv);
}
