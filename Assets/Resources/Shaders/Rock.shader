// Hostile Horizon, Round 1 (item 2): the canyon rock. Triplanar detail (no UVs needed), sediment layers by height on the walls,
// dust on flat ground, darker canyon floors, the main light with shadows, and the shared height haze (HHCommon.hlsl).
// Round 4 (A5): _Mode 0 canyon rock, 1 forest (moss and leaf litter on the flats), 2 dead city (concrete facades with a window grid
// and a few flickering dead lights on the steep faces, asphalt and rubble on the flats), 3 ice (glacial blue bands, snow on the
// flats, a cold sheen, deep blue down in the crevasses). Palettes and colours per biome: WorldLook.ApplyBiome.
Shader "HostileHorizon/Rock"
{
    Properties
    {
        _Detail ("Detail (R mottle, G grain, B cracks)", 2D) = "gray" {}
        _Strata ("Strata colours (1D)", 2D) = "white" {}
        _Dust ("Dust colour", Color) = (0.62, 0.48, 0.44, 1)
        _AmbientSky ("Ambient from the sky", Color) = (0.56, 0.64, 0.72, 1)
        _AmbientGround ("Ambient from the ground", Color) = (0.46, 0.34, 0.30, 1)
        _Mode ("Biome mode", Float) = 0
        _Flat ("Flat ground: from, to (n.y)", Vector) = (0.74, 0.9, 0, 0)
        _Deep ("Deep tint (ice crevasses, city streets)", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        TEXTURE2D(_Detail); SAMPLER(sampler_Detail);
        TEXTURE2D(_Strata); SAMPLER(sampler_Strata);
        CBUFFER_START(UnityPerMaterial)
        float4 _Detail_ST, _Strata_ST, _Dust, _AmbientSky, _AmbientGround, _Flat, _Deep; float _Mode;
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

            float Hash2(float2 q) { return frac(sin(dot(q, float2(127.1, 311.7))) * 43758.5453); }

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
                float flat = smoothstep(_Flat.x, _Flat.y, n.y + (mottle - 0.5) * 0.25);
                float3 dust = _Dust.rgb * (0.8 + 0.35 * grain) * (0.85 + 0.3 * mottle);
                if (_Mode > 0.5 && _Mode < 1.5) { dust = lerp(dust, float3(0.20, 0.15, 0.08) * (0.8 + 0.4 * grain), smoothstep(0.45, 0.7, big.g)); rock = lerp(rock, rock * float3(0.8, 1.0, 0.7), smoothstep(0.5, 0.8, fine.r)); }   // forest: leaf litter, moss on the rock
                if (_Mode > 2.5) rock = strata * (0.86 + 0.22 * mottle) * (0.93 + 0.1 * grain) * lerp(0.9, 1.0, crack);   // ice: smoother than rock
                float3 albedo = lerp(rock, dust, flat);
                float3 glow = 0; float spec = 0;
                float3 v = normalize(p - _WorldSpaceCameraPos);

                if (_Mode > 1.5 && _Mode < 2.5)
                {
                    // dead city: facades on the steep faces
                    float steep = 1.0 - smoothstep(0.3, 0.6, abs(n.y));
                    float2 fc = float2(abs(n.x) > abs(n.z) ? p.z : p.x, p.y);
                    float2 cellSize = float2(4.0, 3.6);
                    float2 cell = floor(fc / cellSize); float2 f = frac(fc / cellSize);
                    float win = step(0.16, f.x) * step(f.x, 0.84) * step(0.22, f.y) * step(f.y, 0.86);
                    float block = Hash2(floor(p.xz / 160.0));
                    float hs = Hash2(cell + block * 91.0 + (abs(n.x) > abs(n.z) ? 17.0 : 0.0));
                    float3 concrete = lerp(float3(0.20, 0.20, 0.21), float3(0.33, 0.31, 0.29), mottle) * (0.75 + 0.35 * grain) * lerp(0.7, 1.0, block);
                    float broken = step(0.82, hs);   // smashed windows show the dark inside
                    float3 glass = lerp(float3(0.05, 0.06, 0.07), float3(0.015, 0.012, 0.01), broken);
                    float floorLine = smoothstep(0.0, 0.08, f.y) * smoothstep(1.0, 0.92, f.y);
                    float3 facade = lerp(concrete * lerp(0.75, 1.0, floorLine), glass, win);
                    facade *= lerp(0.6, 1.0, saturate(big.g * 1.4));   // soot and streaks
                    albedo = lerp(albedo, facade, steep);
                    spec = win * steep * (1 - broken) * 0.25;
                    // a few dead lights still flicker
                    float lit = step(0.985, hs) * win * steep;
                    float flick = step(0.35, frac(sin(floor(_Time.y * (2.0 + hs * 9.0)) * (12.0 + hs * 40.0)) * 43758.5));
                    glow = lit * flick * lerp(float3(1.0, 0.62, 0.3), float3(0.5, 0.8, 1.0), step(0.993, hs)) * 1.6;
                }
                else if (_Mode > 2.5)
                {
                    // ice: a cold sheen on the steep ice (snow on the flats comes from _Dust)
                    spec = (1 - flat) * 0.55;
                }

                Light L = GetMainLight(TransformWorldToShadowCoord(p));
                float ndl = saturate(dot(n, L.direction));
                float cavity = lerp(0.62, 1.0, saturate((p.y + 10.0) / 170.0)) * lerp(_Mode > 2.5 ? 0.97 : 0.8, 1.0, crack);   // deep canyons and cracks get less sky
                float3 amb = lerp(_AmbientGround.rgb, _AmbientSky.rgb, n.y * 0.5 + 0.5) * cavity;
                amb *= lerp(_Deep.rgb, float3(1, 1, 1), saturate(p.y / 160.0));   // ice: blue down in the crevasses
                float3 col = albedo * (L.color * ndl * L.shadowAttenuation + amb);
                if (spec > 0)
                {
                    float3 hv = normalize(L.direction - v);
                    col += L.color * pow(saturate(dot(n, hv)), 48.0) * spec * L.shadowAttenuation;
                }
                col += glow;

                col = lerp(col, HHHazeColor(v), HHHazeAmount(p, _WorldSpaceCameraPos) * lerp(1.0, 0.7, saturate(length(glow))));
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
