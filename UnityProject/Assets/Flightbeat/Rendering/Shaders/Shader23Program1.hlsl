// src: resources.assets Shader 23, GLSL blob 1, sha256 03f01bddd1b4426fcde5eef1e4b278afbd6bd7500d6fd8bed34130c521d10d42
// Arithmetic/constants follow GLSL; float32 storage matches recorded original ANGLE Metal (stage AD).
struct FbVertexInput
{
    float4 in_POSITION0 : POSITION0;
    float4 in_TEXCOORD0 : TEXCOORD0;
    float4 in_COLOR0 : COLOR0;
};
struct FbVaryings
{
    float4 position : SV_POSITION;
    float4 vs_COLOR0 : COLOR0;
    float2 vs_TEXCOORD0 : TEXCOORD0;
};
bool4 FbGreaterEqual(float4 a, float4 b) { return a >= b; }
bool4 FbLessThan(float4 a, float4 b) { return a < b; }
FbVaryings FbVert(FbVertexInput input)
{
    FbVaryings output;
    float4 u_xlat0;
    float4 u_xlat1;
    u_xlat0 = input.in_POSITION0.yyyy * float4(unity_ObjectToWorld[0][1], unity_ObjectToWorld[1][1], unity_ObjectToWorld[2][1], unity_ObjectToWorld[3][1]);
    u_xlat0 = float4(unity_ObjectToWorld[0][0], unity_ObjectToWorld[1][0], unity_ObjectToWorld[2][0], unity_ObjectToWorld[3][0]) * input.in_POSITION0.xxxx + u_xlat0;
    u_xlat0 = float4(unity_ObjectToWorld[0][2], unity_ObjectToWorld[1][2], unity_ObjectToWorld[2][2], unity_ObjectToWorld[3][2]) * input.in_POSITION0.zzzz + u_xlat0;
    u_xlat0 = u_xlat0 + float4(unity_ObjectToWorld[0][3], unity_ObjectToWorld[1][3], unity_ObjectToWorld[2][3], unity_ObjectToWorld[3][3]);
    u_xlat1 = u_xlat0.yyyy * float4(unity_MatrixVP[0][1], unity_MatrixVP[1][1], unity_MatrixVP[2][1], unity_MatrixVP[3][1]);
    u_xlat1 = float4(unity_MatrixVP[0][0], unity_MatrixVP[1][0], unity_MatrixVP[2][0], unity_MatrixVP[3][0]) * u_xlat0.xxxx + u_xlat1;
    u_xlat1 = float4(unity_MatrixVP[0][2], unity_MatrixVP[1][2], unity_MatrixVP[2][2], unity_MatrixVP[3][2]) * u_xlat0.zzzz + u_xlat1;
    output.position = float4(unity_MatrixVP[0][3], unity_MatrixVP[1][3], unity_MatrixVP[2][3], unity_MatrixVP[3][3]) * u_xlat0.wwww + u_xlat1;
    output.vs_COLOR0 = input.in_COLOR0;
    output.vs_TEXCOORD0.xy = input.in_TEXCOORD0.xy;
    return output;
}
float4 FbFrag(FbVaryings input) : SV_Target
{
    float4 SV_Target0;
    float2 u_xlat0;
    float u_xlat1;
    float u_xlat2;
    u_xlat0.xy = input.vs_TEXCOORD0.xy * float2(2.0, 2.0) + float2(-1.0, -1.0);
    u_xlat0.x = dot(u_xlat0.xy, u_xlat0.xy);
    u_xlat1 = u_xlat0.x + -0.5;
    u_xlat0.x = u_xlat0.x * -4.0;
    u_xlat0.x = exp2(u_xlat0.x);
    u_xlat0.x = u_xlat0.x * input.vs_COLOR0.w;
    u_xlat1 = u_xlat1 + u_xlat1;
    u_xlat1 = clamp(u_xlat1, 0.0, 1.0);
    u_xlat2 = u_xlat1 * -2.0 + 3.0;
    u_xlat1 = u_xlat1 * u_xlat1;
    u_xlat1 = (-u_xlat2) * u_xlat1 + 1.0;
    u_xlat0.x = u_xlat1 * u_xlat0.x;
    SV_Target0.w = u_xlat0.x;
    SV_Target0.xyz = input.vs_COLOR0.xyz;
    return SV_Target0;
}
