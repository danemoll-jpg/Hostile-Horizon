// Hostile Horizon: the canopy glass. Transparent, tinted, brighter at grazing angles (a fresnel sheen). Drawn from outside only
// (its faces point out), so the view from the cockpit is clear.
Shader "HostileHorizon/Glass"
{
    Properties { _Color ("Tint", Color) = (0.12, 0.2, 0.24, 0.35) _Sheen ("Sheen", Color) = (0.8, 0.75, 0.7, 0.6) }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Back
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial) float4 _Color, _Sheen; CBUFFER_END
            struct A { float4 p : POSITION; float3 n : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 c : SV_POSITION; float3 w : TEXCOORD0; float3 n : TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
            V vert(A i) { V o; UNITY_SETUP_INSTANCE_ID(i); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); o.w = TransformObjectToWorld(i.p.xyz); o.n = TransformObjectToWorldNormal(i.n); o.c = TransformWorldToHClip(o.w); return o; }
            half4 frag(V i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float f = pow(1.0 - saturate(abs(dot(normalize(i.n), normalize(_WorldSpaceCameraPos - i.w)))), 3.0);   // Fix 2.5: abs: a face whose normal points away no longer goes white
                return half4(lerp(_Color.rgb, _Sheen.rgb, f), saturate(_Color.a + f * _Sheen.a));
            }
            ENDHLSL
        }
    }
}
