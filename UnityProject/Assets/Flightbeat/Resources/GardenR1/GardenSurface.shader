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
            sampler2D _GardenTex;
            float4 _GardenTex_TexelSize;
            float4 GardenFrag(FbVaryings input) : SV_Target {
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
