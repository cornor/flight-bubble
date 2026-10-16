// src: resources.assets Shader 22, GLSL blob 3, sha256 2fe9563abc4edbd0baea76509c695e9c401be157b76948ddc21e2af9752f1b29
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
    float4 u_xlat16_1;
    bool u_xlatb1;
    float4 u_xlat2;
    float4 u_xlat3;
    bool u_xlatb3;
    float4 u_xlat4;
    bool2 u_xlatb4;
    float2 u_xlat5;
    float2 u_xlat6;
    float4 u_xlat7;
    bool4 u_xlatb7;
    float2 u_xlat8;
    float u_xlat16_9;
    bool u_xlatb14;
    float u_xlat17;
    float u_xlat18;
    bool2 u_xlatb25;
    float2 u_xlat27;
    float u_xlat28;
    float u_xlat30;
    u_xlatb0 = 14.5<input.vs_TEXCOORD1.x;
    if(u_xlatb0){
        u_xlat0 = tex2D(_CodexTex, input.vs_TEXCOORD0.xy);
        u_xlat0 = u_xlat0 * input.vs_COLOR0;
        u_xlat16_0 = u_xlat0;
    } else {
        u_xlatb1 = 13.5<input.vs_TEXCOORD1.x;
        if(u_xlatb1){
            u_xlat2.x = tex2D(_ShipTex, input.vs_TEXCOORD0.xy).w;
            u_xlat30 = u_xlat2.x * input.vs_COLOR0.w;
            u_xlat16_0.xyz = input.vs_COLOR0.xyz;
            u_xlat16_0.w = u_xlat30;
        } else {
            u_xlatb3 = 12.5<input.vs_TEXCOORD1.x;
            if(u_xlatb3){
                u_xlat1 = tex2D(_SpaceTex, input.vs_TEXCOORD0.xy);
                u_xlat1 = u_xlat1 * input.vs_COLOR0;
                u_xlat16_0 = u_xlat1;
            } else {
                u_xlatb3 = 11.5<input.vs_TEXCOORD1.x;
                if(u_xlatb3){
                    u_xlat1 = tex2D(_PlateTex, input.vs_TEXCOORD0.xy);
                    u_xlat0 = u_xlat1 * input.vs_COLOR0;
                    u_xlat16_0 = u_xlat0;
                } else {
                    u_xlatb3 = 10.5<input.vs_TEXCOORD1.x;
                    if(u_xlatb3){
                        u_xlat1 = tex2D(_LaunchTex, input.vs_TEXCOORD0.xy);
                        u_xlat0 = u_xlat1 * input.vs_COLOR0;
                        u_xlat16_0 = u_xlat0;
                    } else {
                        u_xlatb4.x = 9.5<input.vs_TEXCOORD1.x;
                        if(u_xlatb4.x){
                            u_xlat1 = tex2D(_BombTex, input.vs_TEXCOORD0.xy);
                            u_xlat0 = u_xlat1 * input.vs_COLOR0;
                            u_xlat16_0 = u_xlat0;
                        } else {
                            u_xlatb4.x = 8.5<input.vs_TEXCOORD1.x;
                            if(u_xlatb4.x){
                                u_xlat1 = tex2D(_ShipTex, input.vs_TEXCOORD0.xy);
                                u_xlat0 = u_xlat1 * input.vs_COLOR0;
                                u_xlat16_0 = u_xlat0;
                            } else {
                                u_xlatb4.x = 7.5<input.vs_TEXCOORD1.x;
                                if(u_xlatb4.x){
                                    u_xlat1 = tex2D(_DeckSheetTex, input.vs_TEXCOORD0.xy);
                                    u_xlat0 = u_xlat1 * input.vs_COLOR0;
                                    u_xlat16_0 = u_xlat0;
                                } else {
                                    u_xlatb4.x = 2.5<input.vs_TEXCOORD1.x;
                                    if(u_xlatb4.x){
                                        u_xlatb4.x = 6.5<input.vs_TEXCOORD1.x;
                                        if(u_xlatb4.x){
                                            u_xlat1 = tex2D(_BridgeTex, input.vs_TEXCOORD0.xy);
                                            u_xlat16_1 = u_xlat1;
                                        } else {
                                            u_xlatb4.x = input.vs_TEXCOORD1.x<3.5;
                                            if(u_xlatb4.x){
                                                u_xlat2 = tex2D(_AssistantTex, input.vs_TEXCOORD0.xy);
                                                u_xlat4.x = input.vs_TEXCOORD1.y * 0.5;
                                                u_xlatb14 = u_xlat4.x>=(-u_xlat4.x);
                                                u_xlat4.x = frac(abs(u_xlat4.x));
                                                u_xlat4.x = (u_xlatb14) ? u_xlat4.x : (-u_xlat4.x);
                                                u_xlatb4.x = 0.25<u_xlat4.x;
                                                if(u_xlatb4.x){
                                                    u_xlatb4.xy = FbGreaterEqual(input.vs_TEXCOORD0.xyxx, float4(0.423000008, 0.437999994, 0.0, 0.0)).xy;
                                                    u_xlat5.x = u_xlatb4.x ? float(1.0) : 0.0;
                                                    u_xlat5.y = u_xlatb4.y ? float(1.0) : 0.0;
;
                                                    u_xlatb25.xy = FbGreaterEqual(float4(0.566999972, 0.52700001, 0.566999972, 0.52700001), input.vs_TEXCOORD0.xyxy).xy;
                                                    u_xlat6.x = u_xlatb25.x ? float(1.0) : 0.0;
                                                    u_xlat6.y = u_xlatb25.y ? float(1.0) : 0.0;
;
                                                    u_xlat6.x = u_xlat5.x * u_xlat6.x;
                                                    u_xlat6.x = u_xlat5.y * u_xlat6.x;
                                                    u_xlat7.x = u_xlat6.y * u_xlat6.x;
                                                    u_xlat3 = tex2D(_AssistantTalkTex, input.vs_TEXCOORD0.xy);
                                                    u_xlat3 = (-u_xlat2) + u_xlat3;
                                                    u_xlat1 = u_xlat7.xxxx * u_xlat3 + u_xlat2;
                                                    u_xlat16_1 = u_xlat1;
                                                } else {
                                                    u_xlat16_1 = u_xlat2;
                                                }
                                                u_xlatb7.x = 1.5<input.vs_TEXCOORD1.y;
                                                if(u_xlatb7.x){
                                                    u_xlatb7.xy = FbGreaterEqual(input.vs_TEXCOORD0.xyxx, float4(0.291999996, 0.578000009, 0.0, 0.0)).xy;
                                                    u_xlatb7.zw = FbGreaterEqual(float4(0.0, 0.0, 0.640999973, 0.720000029), input.vs_TEXCOORD0.xxxy).zw;
                                                    u_xlat7.x = u_xlatb7.x ? float(1.0) : 0.0;
                                                    u_xlat7.y = u_xlatb7.y ? float(1.0) : 0.0;
                                                    u_xlat7.z = u_xlatb7.z ? float(1.0) : 0.0;
                                                    u_xlat7.w = u_xlatb7.w ? float(1.0) : 0.0;
;
                                                    u_xlat7.x = u_xlat7.z * u_xlat7.x;
                                                    u_xlat7.x = u_xlat7.y * u_xlat7.x;
                                                    u_xlat7.x = u_xlat7.w * u_xlat7.x;
                                                    u_xlat2 = tex2D(_AssistantBlinkTex, input.vs_TEXCOORD0.xy);
                                                    u_xlat2 = (-u_xlat16_1) + u_xlat2;
                                                    u_xlat1 = u_xlat7.xxxx * u_xlat2 + u_xlat16_1;
                                                    u_xlat16_1 = u_xlat1;
                                                }
                                            } else {
                                                u_xlat2 = tex2D(_VictoryTex, input.vs_TEXCOORD0.xy);
                                                u_xlatb7.xy = FbLessThan(input.vs_TEXCOORD1.xxxx, float4(4.5, 5.5, 0.0, 0.0)).xy;
                                                u_xlat3 = tex2D(_RescueTex, input.vs_TEXCOORD0.xy);
                                                u_xlat4 = tex2D(_DefeatTex, input.vs_TEXCOORD0.xy);
                                                u_xlat3 = (u_xlatb7.y) ? u_xlat3 : u_xlat4;
                                                u_xlat1 = (u_xlatb7.x) ? u_xlat2 : u_xlat3;
                                                u_xlat16_1 = u_xlat1;
                                            }
                                        }
                                        u_xlat16_0 = u_xlat16_1 * input.vs_COLOR0;
                                    } else {
                                        u_xlatb7.x = 1.5<input.vs_TEXCOORD1.x;
                                        if(u_xlatb7.x){
                                            u_xlat7.xy = input.vs_TEXCOORD0.xy * float2(1024.0, 2048.0) + float2(-0.5, -0.5);
                                            u_xlat27.xy = frac(u_xlat7.xy);
                                            u_xlat7.xy = floor(u_xlat7.xy);
                                            u_xlat7.xy = u_xlat7.xy + float2(0.5, 0.5);
                                            u_xlat8.xy = u_xlat7.xy * float2(0.0009765625, 0.00048828125);
                                            u_xlat8.x = tex2D(_MainTex, u_xlat8.xy).w;
                                            u_xlat1 = u_xlat7.xyxy * float4(0.0009765625, 0.00048828125, 0.0009765625, 0.00048828125) + float4(0.0009765625, 0.0, 0.0, 0.00048828125);
                                            u_xlat18 = tex2D(_MainTex, u_xlat1.xy).w;
                                            u_xlat28 = tex2D(_MainTex, u_xlat1.zw).w;
                                            u_xlat7.xy = u_xlat7.xy * float2(0.0009765625, 0.00048828125) + float2(0.0009765625, 0.00048828125);
                                            u_xlat7.x = tex2D(_MainTex, u_xlat7.xy).w;
                                            u_xlat17 = (-u_xlat8.x) + u_xlat18;
                                            u_xlat17 = u_xlat27.x * u_xlat17 + u_xlat8.x;
                                            u_xlat7.x = (-u_xlat28) + u_xlat7.x;
                                            u_xlat7.x = u_xlat27.x * u_xlat7.x + u_xlat28;
                                            u_xlat7.x = (-u_xlat17) + u_xlat7.x;
                                            u_xlat7.x = u_xlat27.y * u_xlat7.x + u_xlat17;
                                            u_xlat17 = ddx(u_xlat7.x);
                                            u_xlat27.x = ddy(u_xlat7.x);
                                            u_xlat17 = abs(u_xlat27.x) + abs(u_xlat17);
                                            u_xlat17 = u_xlat17 * 0.649999976;
                                            u_xlat17 = max(u_xlat17, 0.0120000001);
                                            u_xlat27.x = (-u_xlat17) + 0.5;
                                            u_xlat7.y = u_xlat17 + 0.5;
                                            u_xlat7.xy = (-u_xlat27.xx) + u_xlat7.xy;
                                            u_xlat17 = float(1.0) / u_xlat7.y;
                                            u_xlat7.x = u_xlat17 * u_xlat7.x;
                                            u_xlat7.x = clamp(u_xlat7.x, 0.0, 1.0);
                                            u_xlat17 = u_xlat7.x * -2.0 + 3.0;
                                            u_xlat7.x = u_xlat7.x * u_xlat7.x;
                                            u_xlat7.x = u_xlat7.x * u_xlat17;
                                            u_xlat30 = u_xlat7.x * input.vs_COLOR0.w;
                                            u_xlat16_0.xyz = input.vs_COLOR0.xyz;
                                            u_xlat16_0.w = u_xlat30;
                                        } else {
                                            u_xlatb7.x = 0.5<input.vs_TEXCOORD1.x;
                                            if(u_xlatb7.x){
                                                u_xlat7.xy = input.vs_TEXCOORD0.xy * float2(2.0, 2.0) + float2(-1.0, -1.0);
                                                u_xlat7.x = dot(u_xlat7.xy, u_xlat7.xy);
                                                u_xlat17 = u_xlat7.x * -4.5;
                                                u_xlat17 = exp2(u_xlat17);
                                                u_xlat7.x = u_xlat7.x + -0.600000024;
                                                u_xlat7.x = u_xlat7.x * 2.50000024;
                                                u_xlat7.x = clamp(u_xlat7.x, 0.0, 1.0);
                                                u_xlat27.x = u_xlat7.x * -2.0 + 3.0;
                                                u_xlat7.x = u_xlat7.x * u_xlat7.x;
                                                u_xlat7.x = (-u_xlat27.x) * u_xlat7.x + 1.0;
                                                u_xlat7.x = u_xlat7.x * u_xlat17;
                                                u_xlat30 = u_xlat7.x * input.vs_COLOR0.w;
                                                u_xlat16_0.xyz = input.vs_COLOR0.xyz;
                                                u_xlat16_0.w = u_xlat30;
                                            } else {
                                                u_xlat1 = tex2D(_MainTex, input.vs_TEXCOORD0.xy);
                                                u_xlat1 = u_xlat1 + _TextureSampleAdd;
                                                u_xlat0 = u_xlat1 * input.vs_COLOR0;
                                                u_xlat16_0 = u_xlat0;
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
    u_xlat16_9 = u_xlat16_0.w + -0.00100000005;
    u_xlatb7.x = u_xlat16_9<0.0;
    if(u_xlatb7.x){discard;}
    SV_Target0 = u_xlat16_0;
    return SV_Target0;
}
