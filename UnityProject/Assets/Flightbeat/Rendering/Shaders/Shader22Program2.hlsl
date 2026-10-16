// src: resources.assets Shader 22, GLSL blob 2, sha256 c6b033dc405900d151c76da6eddbcd779f97946018302f0d85e9f1252455e892
// Arithmetic/constants follow GLSL; float32 storage matches recorded original ANGLE Metal (stage AD).
struct FbVertexInput
{
    float4 in_POSITION0 : POSITION0;
    float4 in_COLOR0 : COLOR0;
    float2 in_TEXCOORD0 : TEXCOORD0;
    float2 in_TEXCOORD1 : TEXCOORD1;
};
struct FbVaryings
{
    float4 position : SV_POSITION;
    float4 vs_COLOR0 : COLOR0;
    float2 vs_TEXCOORD0 : TEXCOORD0;
    float2 vs_TEXCOORD1 : TEXCOORD1;
    float4 vs_TEXCOORD2 : TEXCOORD2;
};
float4 _TextureSampleAdd;
sampler2D_float _CodexTex;
sampler2D_float _ShipTex;
sampler2D_float _SpaceTex;
sampler2D_float _PlateTex;
sampler2D_float _LaunchTex;
sampler2D_float _BombTex;
sampler2D_float _DeckSheetTex;
sampler2D_float _BridgeTex;
sampler2D_float _AssistantTex;
sampler2D_float _AssistantTalkTex;
sampler2D_float _AssistantBlinkTex;
sampler2D_float _VictoryTex;
sampler2D_float _RescueTex;
sampler2D_float _DefeatTex;
sampler2D_float _MainTex;
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
    output.vs_TEXCOORD1.xy = input.in_TEXCOORD1.xy;
    output.vs_TEXCOORD2 = input.in_POSITION0;
    return output;
}
float4 FbFrag(FbVaryings input) : SV_Target
{
    float4 SV_Target0;
    float4 u_xlat0;
    float4 u_xlat16_0;
    bool u_xlatb0;
    float4 u_xlat1;
    bool4 u_xlatb1;
    float4 u_xlat2;
    bool4 u_xlatb2;
    float4 u_xlat3;
    float4 u_xlat4;
    float u_xlat6;
    float u_xlat7;
    bool u_xlatb7;
    float2 u_xlat11;
    float u_xlat12;
    u_xlatb0 = 14.5<input.vs_TEXCOORD1.x;
    if(u_xlatb0){
        u_xlat0 = tex2D(_CodexTex, input.vs_TEXCOORD0.xy);
        u_xlat0 = u_xlat0 * input.vs_COLOR0;
        SV_Target0 = u_xlat0;
    } else {
        u_xlatb0 = 13.5<input.vs_TEXCOORD1.x;
        if(u_xlatb0){
            u_xlat0.x = tex2D(_ShipTex, input.vs_TEXCOORD0.xy).w;
            u_xlat0.x = u_xlat0.x * input.vs_COLOR0.w;
            SV_Target0.xyz = input.vs_COLOR0.xyz;
            SV_Target0.w = u_xlat0.x;
        } else {
            u_xlatb0 = 12.5<input.vs_TEXCOORD1.x;
            if(u_xlatb0){
                u_xlat0 = tex2D(_SpaceTex, input.vs_TEXCOORD0.xy);
                u_xlat0 = u_xlat0 * input.vs_COLOR0;
                SV_Target0 = u_xlat0;
            } else {
                u_xlatb0 = 11.5<input.vs_TEXCOORD1.x;
                if(u_xlatb0){
                    u_xlat0 = tex2D(_PlateTex, input.vs_TEXCOORD0.xy);
                    u_xlat0 = u_xlat0 * input.vs_COLOR0;
                    SV_Target0 = u_xlat0;
                } else {
                    u_xlatb0 = 10.5<input.vs_TEXCOORD1.x;
                    if(u_xlatb0){
                        u_xlat0 = tex2D(_LaunchTex, input.vs_TEXCOORD0.xy);
                        u_xlat0 = u_xlat0 * input.vs_COLOR0;
                        SV_Target0 = u_xlat0;
                    } else {
                        u_xlatb0 = 9.5<input.vs_TEXCOORD1.x;
                        if(u_xlatb0){
                            u_xlat0 = tex2D(_BombTex, input.vs_TEXCOORD0.xy);
                            u_xlat0 = u_xlat0 * input.vs_COLOR0;
                            SV_Target0 = u_xlat0;
                        } else {
                            u_xlatb0 = 8.5<input.vs_TEXCOORD1.x;
                            if(u_xlatb0){
                                u_xlat0 = tex2D(_ShipTex, input.vs_TEXCOORD0.xy);
                                u_xlat0 = u_xlat0 * input.vs_COLOR0;
                                SV_Target0 = u_xlat0;
                            } else {
                                u_xlatb0 = 7.5<input.vs_TEXCOORD1.x;
                                if(u_xlatb0){
                                    u_xlat0 = tex2D(_DeckSheetTex, input.vs_TEXCOORD0.xy);
                                    u_xlat0 = u_xlat0 * input.vs_COLOR0;
                                    SV_Target0 = u_xlat0;
                                } else {
                                    u_xlatb0 = 2.5<input.vs_TEXCOORD1.x;
                                    if(u_xlatb0){
                                        u_xlatb0 = 6.5<input.vs_TEXCOORD1.x;
                                        if(u_xlatb0){
                                            u_xlat0 = tex2D(_BridgeTex, input.vs_TEXCOORD0.xy);
                                            u_xlat16_0 = u_xlat0;
                                        } else {
                                            u_xlatb1.x = input.vs_TEXCOORD1.x<3.5;
                                            if(u_xlatb1.x){
                                                u_xlat1 = tex2D(_AssistantTex, input.vs_TEXCOORD0.xy);
                                                u_xlat2.x = input.vs_TEXCOORD1.y * 0.5;
                                                u_xlatb7 = u_xlat2.x>=(-u_xlat2.x);
                                                u_xlat2.x = frac(abs(u_xlat2.x));
                                                u_xlat2.x = (u_xlatb7) ? u_xlat2.x : (-u_xlat2.x);
                                                u_xlatb2.x = 0.25<u_xlat2.x;
                                                if(u_xlatb2.x){
                                                    u_xlatb2.xy = FbGreaterEqual(input.vs_TEXCOORD0.xyxx, float4(0.423000008, 0.437999994, 0.0, 0.0)).xy;
                                                    u_xlatb2.zw = FbGreaterEqual(float4(0.0, 0.0, 0.566999972, 0.52700001), input.vs_TEXCOORD0.xxxy).zw;
                                                    u_xlat2.x = u_xlatb2.x ? float(1.0) : 0.0;
                                                    u_xlat2.y = u_xlatb2.y ? float(1.0) : 0.0;
                                                    u_xlat2.z = u_xlatb2.z ? float(1.0) : 0.0;
                                                    u_xlat2.w = u_xlatb2.w ? float(1.0) : 0.0;
;
                                                    u_xlat2.x = u_xlat2.z * u_xlat2.x;
                                                    u_xlat2.x = u_xlat2.y * u_xlat2.x;
                                                    u_xlat2.x = u_xlat2.w * u_xlat2.x;
                                                    u_xlat3 = tex2D(_AssistantTalkTex, input.vs_TEXCOORD0.xy);
                                                    u_xlat3 = (-u_xlat1) + u_xlat3;
                                                    u_xlat0 = u_xlat2.xxxx * u_xlat3 + u_xlat1;
                                                    u_xlat16_0 = u_xlat0;
                                                } else {
                                                    u_xlat16_0 = u_xlat1;
                                                }
                                                u_xlatb1.x = 1.5<input.vs_TEXCOORD1.y;
                                                if(u_xlatb1.x){
                                                    u_xlatb1.xy = FbGreaterEqual(input.vs_TEXCOORD0.xyxx, float4(0.291999996, 0.578000009, 0.0, 0.0)).xy;
                                                    u_xlatb1.zw = FbGreaterEqual(float4(0.0, 0.0, 0.640999973, 0.720000029), input.vs_TEXCOORD0.xxxy).zw;
                                                    u_xlat1.x = u_xlatb1.x ? float(1.0) : 0.0;
                                                    u_xlat1.y = u_xlatb1.y ? float(1.0) : 0.0;
                                                    u_xlat1.z = u_xlatb1.z ? float(1.0) : 0.0;
                                                    u_xlat1.w = u_xlatb1.w ? float(1.0) : 0.0;
;
                                                    u_xlat1.x = u_xlat1.z * u_xlat1.x;
                                                    u_xlat1.x = u_xlat1.y * u_xlat1.x;
                                                    u_xlat1.x = u_xlat1.w * u_xlat1.x;
                                                    u_xlat2 = tex2D(_AssistantBlinkTex, input.vs_TEXCOORD0.xy);
                                                    u_xlat2 = (-u_xlat16_0) + u_xlat2;
                                                    u_xlat0 = u_xlat1.xxxx * u_xlat2 + u_xlat16_0;
                                                    u_xlat16_0 = u_xlat0;
                                                }
                                            } else {
                                                u_xlat1 = tex2D(_VictoryTex, input.vs_TEXCOORD0.xy);
                                                u_xlatb2.xy = FbLessThan(input.vs_TEXCOORD1.xxxx, float4(4.5, 5.5, 0.0, 0.0)).xy;
                                                u_xlat3 = tex2D(_RescueTex, input.vs_TEXCOORD0.xy);
                                                u_xlat4 = tex2D(_DefeatTex, input.vs_TEXCOORD0.xy);
                                                u_xlat3 = (u_xlatb2.y) ? u_xlat3 : u_xlat4;
                                                u_xlat0 = (u_xlatb2.x) ? u_xlat1 : u_xlat3;
                                                u_xlat16_0 = u_xlat0;
                                            }
                                        }
                                        SV_Target0 = u_xlat16_0 * input.vs_COLOR0;
                                    } else {
                                        u_xlatb1.x = 1.5<input.vs_TEXCOORD1.x;
                                        if(u_xlatb1.x){
                                            u_xlat1.xy = input.vs_TEXCOORD0.xy * float2(1024.0, 2048.0) + float2(-0.5, -0.5);
                                            u_xlat11.xy = frac(u_xlat1.xy);
                                            u_xlat1.xy = floor(u_xlat1.xy);
                                            u_xlat1.xy = u_xlat1.xy + float2(0.5, 0.5);
                                            u_xlat2.xy = u_xlat1.xy * float2(0.0009765625, 0.00048828125);
                                            u_xlat2.x = tex2D(_MainTex, u_xlat2.xy).w;
                                            u_xlat0 = u_xlat1.xyxy * float4(0.0009765625, 0.00048828125, 0.0009765625, 0.00048828125) + float4(0.0009765625, 0.0, 0.0, 0.00048828125);
                                            u_xlat7 = tex2D(_MainTex, u_xlat0.xy).w;
                                            u_xlat12 = tex2D(_MainTex, u_xlat0.zw).w;
                                            u_xlat1.xy = u_xlat1.xy * float2(0.0009765625, 0.00048828125) + float2(0.0009765625, 0.00048828125);
                                            u_xlat1.x = tex2D(_MainTex, u_xlat1.xy).w;
                                            u_xlat6 = (-u_xlat2.x) + u_xlat7;
                                            u_xlat6 = u_xlat11.x * u_xlat6 + u_xlat2.x;
                                            u_xlat1.x = (-u_xlat12) + u_xlat1.x;
                                            u_xlat1.x = u_xlat11.x * u_xlat1.x + u_xlat12;
                                            u_xlat1.x = (-u_xlat6) + u_xlat1.x;
                                            u_xlat1.x = u_xlat11.y * u_xlat1.x + u_xlat6;
                                            u_xlat6 = ddx(u_xlat1.x);
                                            u_xlat11.x = ddy(u_xlat1.x);
                                            u_xlat6 = abs(u_xlat11.x) + abs(u_xlat6);
                                            u_xlat6 = u_xlat6 * 0.649999976;
                                            u_xlat6 = max(u_xlat6, 0.0120000001);
                                            u_xlat11.x = (-u_xlat6) + 0.5;
                                            u_xlat1.y = u_xlat6 + 0.5;
                                            u_xlat1.xy = (-u_xlat11.xx) + u_xlat1.xy;
                                            u_xlat6 = float(1.0) / u_xlat1.y;
                                            u_xlat1.x = u_xlat6 * u_xlat1.x;
                                            u_xlat1.x = clamp(u_xlat1.x, 0.0, 1.0);
                                            u_xlat6 = u_xlat1.x * -2.0 + 3.0;
                                            u_xlat1.x = u_xlat1.x * u_xlat1.x;
                                            u_xlat1.x = u_xlat1.x * u_xlat6;
                                            u_xlat1.x = u_xlat1.x * input.vs_COLOR0.w;
                                            SV_Target0.xyz = input.vs_COLOR0.xyz;
                                            SV_Target0.w = u_xlat1.x;
                                        } else {
                                            u_xlatb1.x = 0.5<input.vs_TEXCOORD1.x;
                                            if(u_xlatb1.x){
                                                u_xlat1.xy = input.vs_TEXCOORD0.xy * float2(2.0, 2.0) + float2(-1.0, -1.0);
                                                u_xlat1.x = dot(u_xlat1.xy, u_xlat1.xy);
                                                u_xlat6 = u_xlat1.x * -4.5;
                                                u_xlat6 = exp2(u_xlat6);
                                                u_xlat1.x = u_xlat1.x + -0.600000024;
                                                u_xlat1.x = u_xlat1.x * 2.50000024;
                                                u_xlat1.x = clamp(u_xlat1.x, 0.0, 1.0);
                                                u_xlat11.x = u_xlat1.x * -2.0 + 3.0;
                                                u_xlat1.x = u_xlat1.x * u_xlat1.x;
                                                u_xlat1.x = (-u_xlat11.x) * u_xlat1.x + 1.0;
                                                u_xlat1.x = u_xlat1.x * u_xlat6;
                                                u_xlat1.x = u_xlat1.x * input.vs_COLOR0.w;
                                                SV_Target0.xyz = input.vs_COLOR0.xyz;
                                                SV_Target0.w = u_xlat1.x;
                                            } else {
                                                u_xlat0 = tex2D(_MainTex, input.vs_TEXCOORD0.xy);
                                                u_xlat0 = u_xlat0 + _TextureSampleAdd;
                                                u_xlat0 = u_xlat0 * input.vs_COLOR0;
                                                SV_Target0 = u_xlat0;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
    return SV_Target0;
}
