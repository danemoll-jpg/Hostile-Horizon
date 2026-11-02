// Hostile Horizon, Round 1 (item 2): the canyon rock. Triplanar detail (no UVs needed), sediment layers by height on the walls,
// dust on flat ground, darker canyon floors, the main light with shadows, and the shared height haze (HHCommon.hlsl).
Shader "HostileHorizon/Rock"
{
    Properties
    {
        _Detail ("Detail (R mottle, G grain, B cracks)", 2D) = "gray" {}
        _Strata ("Strata colours (1D)", 2D) = "white" {}
        _Dust ("Dust colour", Color) = (0.62, 0.48, 0.44, 1)
        _AmbientSky ("Ambient from the sky", Color) = (0.56, 0.64, 0.72, 1)
        _AmbientGround ("Ambient from the ground", Color) = (0.46, 0.34, 0.30, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        TEXTURE2D(_Detail); SAMPLER(sampler_Detail);
        TEXTURE2D(_Strata); SAMPLER(sampler_Strata);
        CBUFFER_START(UnityPerMaterial)
        float4 _Detail_ST, _Strata_ST, _Dust, _AmbientSky, _AmbientGround;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "HHCommon.hlsl"

            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct V { float4 positionCS : SV_POSITION; float3 posWS : TEXCOORD0; float3 nWS : TEXCOORD1; };

            V vert(A i)
            {
                V o; o.posWS = TransformObjectToWorld(i.positionOS.xyz); o.nWS = TransformObjectToWorldNormal(i.normalOS);
                o.positionCS = TransformWorldToHClip(o.posWS); return o;
            }

            float4 Tri(float3 p, float3 w, float scale)
            {
                return SAMPLE_TEXTURE2D(_Detail, sampler_Detail, p.zy * scale) * w.x
                     + SAMPLE_TEXTURE2D(_Detail, sampler_Detail, p.xz * scale) * w.y
                     + SAMPLE_TEXTURE2D(_Detail, sampler_Detail, p.xy * scale) * w.z;
            }

            half4 frag(V i) : SV_Target
            {
                float3 n = normalize(i.nWS); float3 p = i.posWS;
                float3 w = pow(abs(n), 4.0); w /= (w.x + w.y + w.z);
                float4 big = Tri(p, w, 1.0 / 61.0), fine = Tri(p, w, 1.0 / 9.0);
                float mottle = big.r, grain = fine.g, crack = saturate(fine.b * 1.3);

                // sediment layers: height, wobbled by the rock so the bands are not ruler-straight
                float h = p.y + (big.r - 0.5) * 14.0 + sin(p.x * 0.0021 + p.z * 0.0017) * 9.0;
                float3 strata = SAMPLE_TEXTURE2D(_Strata, sampler_Strata, float2(h / 190.0, 0.5)).rgb;
                float3 rock = strata * (0.72 + 0.4 * mottle) * (0.8 + 0.3 * grain) * lerp(0.7, 1.0, crack);
                float flat = smoothstep(0.74, 0.9, n.y + (mottle - 0.5) * 0.25);
                float3 dust = _Dust.rgb * (0.8 + 0.35 * grain) * (0.85 + 0.3 * mottle);
                float3 albedo = lerp(rock, dust, flat);

                Light L = GetMainLight(TransformWorldToShadowCoord(p));
                float ndl = saturate(dot(n, L.direction));
                float cavity = lerp(0.62, 1.0, saturate((p.y + 10.0) / 170.0)) * lerp(0.8, 1.0, crack);   // deep canyons and cracks get less sky
                float3 amb = lerp(_AmbientGround.rgb, _AmbientSky.rgb, n.y * 0.5 + 0.5) * cavity;
                float3 col = albedo * (L.color * ndl * L.shadowAttenuation + amb);

                float3 v = normalize(p - _WorldSpaceCameraPos);
                col = lerp(col, HHHazeColor(v), HHHazeAmount(p, _WorldSpaceCameraPos));
                return half4(col, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0 Cull Back
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            float3 _LightDirection; float3 _LightPosition;
            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            float4 vert(A i) : SV_POSITION
            {
                float3 p = TransformObjectToWorld(i.positionOS.xyz); float3 n = TransformObjectToWorldNormal(i.normalOS);
                float4 c = TransformWorldToHClip(ApplyShadowBias(p, n, _LightDirection));
                #if UNITY_REVERSED_Z
                c.z = min(c.z, UNITY_NEAR_CLIP_VALUE);
                #else
                c.z = max(c.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                return c;
            }
            half4 frag() : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            struct A { float4 positionOS : POSITION; };
            float4 vert(A i) : SV_POSITION { return TransformObjectToHClip(i.positionOS.xyz); }
            half frag() : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
