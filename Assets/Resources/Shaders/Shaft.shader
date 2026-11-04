// Hostile Horizon, Round 4 (A2): a soft shaft of light into a forest clearing. Additive; fades out at both ends (mesh uv.y 0..1)
// and toward its silhouette, so it reads as light in hazy air rather than a solid tube.
Shader "HostileHorizon/Shaft"
{
    Properties { _Color ("Colour", Color) = (0.05, 0.045, 0.025, 1) }
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
            struct A { float4 p : POSITION; float3 n : NORMAL; float2 uv : TEXCOORD0; };
            struct V { float4 p : SV_POSITION; float3 n : TEXCOORD0; float3 w : TEXCOORD1; float v : TEXCOORD2; };
            V vert(A i) { V o; o.w = TransformObjectToWorld(i.p.xyz); o.p = TransformWorldToHClip(o.w); o.n = TransformObjectToWorldNormal(i.n); o.v = i.uv.y; return o; }
            half4 frag(V i) : SV_Target
            {
                float3 view = normalize(_WorldSpaceCameraPos - i.w);
                float face = pow(abs(dot(normalize(i.n), view)), 2.0);
                float along = pow(saturate(sin(i.v * 3.14159)), 1.5) * lerp(1.0, 0.4, i.v);
                float near = saturate(distance(_WorldSpaceCameraPos, i.w) / 40.0);
                return half4(_Color.rgb * face * along * near, 1);
            }
            ENDHLSL
        }
    }
}
