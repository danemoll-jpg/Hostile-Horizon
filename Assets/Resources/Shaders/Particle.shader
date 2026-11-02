// Hostile Horizon, Round 2: soft round particles for the explosion, fire and smoke (no texture: the soft disc is computed).
// Additive (fire, sparks): _SrcBlend One, _DstBlend One. Smoke: _SrcBlend SrcAlpha, _DstBlend OneMinusSrcAlpha.
Shader "HostileHorizon/Particle"
{
    Properties
    {
        _Color ("Tint", Color) = (1, 1, 1, 1)
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst", Float) = 1
        _Softness ("Edge softness", Float) = 1.6
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend [_SrcBlend] [_DstBlend]
            ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial) float4 _Color; float _SrcBlend, _DstBlend, _Softness; CBUFFER_END
            struct A { float4 p : POSITION; float4 c : COLOR; float2 uv : TEXCOORD0; };
            struct V { float4 p : SV_POSITION; float4 c : COLOR; float2 uv : TEXCOORD0; };
            V vert(A i) { V o; o.p = TransformObjectToHClip(i.p.xyz); o.c = i.c * _Color; o.uv = i.uv; return o; }
            half4 frag(V i) : SV_Target
            {
                float r = length(i.uv * 2 - 1);
                float a = pow(saturate(1 - r), _Softness);
                half4 c = i.c;
                // additive: colour scaled by the disc; blended: the disc in alpha
                return _DstBlend == 1 ? half4(c.rgb * c.a * a, 1) : half4(c.rgb, c.a * a);
            }
            ENDHLSL
        }
    }
}
