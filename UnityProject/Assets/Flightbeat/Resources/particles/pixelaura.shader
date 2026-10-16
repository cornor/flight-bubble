// src: resources.assets Shader pathId 23; frozen serialized state and programs.
Shader "PixelWing/Soft Aura"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "IGNOREPROJECTOR"="true" "QUEUE"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha One
            BlendOp Add
            ColorMask RGBA
            ZWrite Off
            ZTest LEqual
            Cull Off
            ZClip On
            AlphaToMask Off
            Offset 0, 0
            Stencil
            {
                Ref 0
                ReadMask 255
                WriteMask 255
                Comp Always
                Pass Keep
                Fail Keep
                ZFail Keep
            }
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex FbVert
            #pragma fragment FbFrag
            #include "HLSLSupport.cginc"
            #include "UnityShaderVariables.cginc"
            #include "../../Rendering/Shaders/Shader23Program1.hlsl"
            ENDHLSL
        }
    }
    Fallback Off
}
