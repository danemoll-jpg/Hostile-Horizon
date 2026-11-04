// Hostile Horizon (Fix 2.6): an additive glow for the route trail's markers: fades out by its colour going to black, seen
// from both sides.
Shader "HostileHorizon/GlowAdd"
{
    Properties { _Color ("Colour", Color) = (0.4, 1, 0.6, 1) }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend One One ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial) float4 _Color; CBUFFER_END
            float4 vert(float4 p : POSITION) : SV_POSITION { return TransformObjectToHClip(p.xyz); }
            half4 frag() : SV_Target { return half4(_Color.rgb, 1); }
            ENDHLSL
        }
    }
}
