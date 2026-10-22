// src: resources.assets Shader pathId 22; frozen serialized state and programs.
Shader "Garden/R1 Surface"
{
    Properties
    {
        [PerRendererData] _MainTex ("Atlas", 2D) = "white" {}
        _AssistantTex ("Assistant", 2D) = "black" {}
        _AssistantTalkTex ("Talking", 2D) = "black" {}
        _AssistantBlinkTex ("Blinking", 2D) = "black" {}
        _VictoryTex ("Victory", 2D) = "black" {}
        _RescueTex ("Rescue", 2D) = "black" {}
        _DefeatTex ("Defeat", 2D) = "black" {}
        _BridgeTex ("Command Bridge", 2D) = "black" {}
        _DeckSheetTex ("Generated Icons", 2D) = "black" {}
        _ShipTex ("Exploded Fighter", 2D) = "black" {}
        _BombTex ("Generated Bomb", 2D) = "black" {}
        _LaunchTex ("Generated Launch Plate", 2D) = "black" {}
        _PlateTex ("Generated UI Plate", 2D) = "black" {}
        _SpaceTex ("Pixel Space", 2D) = "black" {}
        _CodexTex ("Codex Thumbnails", 2D) = "black" {}
        _GardenTex ("Garden Atlas", 2D) = "white" {}
        _GardenTex2 ("Creature Atlas", 2D) = "white" {}
        _GardenBarsTex ("Hint Glass and Rounded Type", 2D) = "white" {}
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags { "CanUseSpriteAtlas"="true" "IGNOREPROJECTOR"="true" "QUEUE"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            BlendOp Add
            ColorMask [_ColorMask]
            ZWrite Off
            ZTest [unity_GUIZTestMode]
            Cull Off
            ZClip On
            AlphaToMask Off
            Offset 0, 0
            Stencil
            {
                Ref [_Stencil]
                ReadMask [_StencilReadMask]
                WriteMask [_StencilWriteMask]
                Comp [_StencilComp]
                Pass [_StencilOp]
                Fail Keep
                ZFail Keep
            }
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex FbVert
            #pragma fragment GardenFrag
            #pragma multi_compile_local __ UNITY_UI_ALPHACLIP
            #pragma multi_compile_local __ UNITY_UI_CLIP_RECT
            #include "HLSLSupport.cginc"
            #include "UnityShaderVariables.cginc"
            // The retired codex branch is unreachable. Reuse its sampler declaration
            // for creatures to stay within WebGL's 16 fragment-sampler limit.
            #define _CodexTex _GardenTex2
            // R1 replaces the original launch plate (DeckArt 18); reuse that sampler too.
            #define _LaunchTex _GardenBarsTex
            #define FbFrag OriginalFrag
            #if defined(UNITY_UI_ALPHACLIP) && defined(UNITY_UI_CLIP_RECT)
            #include "../../Rendering/Shaders/Shader22Program5.hlsl"
            #elif defined(UNITY_UI_CLIP_RECT)
            #include "../../Rendering/Shaders/Shader22Program4.hlsl"
            #elif defined(UNITY_UI_ALPHACLIP)
            #include "../../Rendering/Shaders/Shader22Program3.hlsl"
            #else
            #include "../../Rendering/Shaders/Shader22Program2.hlsl"
            #endif
            #undef FbFrag
            #undef _CodexTex
            #undef _LaunchTex
            sampler2D _GardenTex;
            float4 _GardenTex_TexelSize;
            float4 _GardenTex2_TexelSize;
            float4 GardenFrag(FbVaryings input) : SV_Target {
                if (input.vs_TEXCOORD1.x > 29.5) {
                    // Recover pixel dimensions from the normalized UV derivatives, keeping the existing float2
                    // vertex layout and sampler budget intact. The second mode value is radius / shorter half-side.
                    float2 halfSize = 1. / max(fwidth(input.vs_TEXCOORD0), .0001);
                    float radius = input.vs_TEXCOORD1.y * min(halfSize.x, halfSize.y);
                    float2 q = abs(input.vs_TEXCOORD0) * halfSize - (halfSize - radius);
                    float distance = length(max(q, 0.)) + min(max(q.x, q.y), 0.) - radius;
                    float aa = max(fwidth(distance), .35);
                    float4 color = input.vs_COLOR0;
                    color.a *= 1. - smoothstep(-aa, aa, distance);
                    return color;
                }
                if (input.vs_TEXCOORD1.x > 27.5) {
                    float4 color = input.vs_COLOR0;
                    color.a *= tex2D(_GardenTex2, input.vs_TEXCOORD0).a;
                    return color;
                }
                if (input.vs_TEXCOORD1.x > 23.5) {
                    float4 art = tex2D(_GardenBarsTex, input.vs_TEXCOORD0);
                    if (input.vs_TEXCOORD1.x > 25.5) {
                        // Recolour the cyan glass while preserving white reflections, detail and alpha.
                        float low = min(art.r, min(art.g, art.b));
                        float high = max(art.r, max(art.g, art.b));
                        float3 edge = input.vs_COLOR0.rgb / max(.001, max(input.vs_COLOR0.r, max(input.vs_COLOR0.g, input.vs_COLOR0.b)));
                        art.rgb = low + (high - low) * edge;
                        // Keep bright rim/reflections and saturated edges visible in see-through hints.
                        float rim = max(smoothstep(.85, 1., low), smoothstep(.45, .7, high - low));
                        art.a *= lerp(input.vs_TEXCOORD1.y, 1., rim) * input.vs_COLOR0.a;
                        return art;
                    }
                    return art * input.vs_COLOR0;
                }
                if (input.vs_TEXCOORD1.x > 21.5) {
                    float2 uv = input.vs_TEXCOORD0;
                    if (input.vs_TEXCOORD1.x > 22.5) uv = (floor(uv * _GardenTex2_TexelSize.zw) + .5) * _GardenTex2_TexelSize.xy;
                    return tex2D(_GardenTex2, uv) * input.vs_COLOR0;
                }
                if (input.vs_TEXCOORD1.x > 19.5) {
                    float2 uv = input.vs_TEXCOORD0;
                    if (input.vs_TEXCOORD1.x > 20.5) uv = (floor(uv * _GardenTex_TexelSize.zw) + .5) * _GardenTex_TexelSize.xy;
                    return tex2D(_GardenTex, uv) * input.vs_COLOR0;
                }
                return OriginalFrag(input);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
