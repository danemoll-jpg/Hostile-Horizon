// Hostile Horizon, Round 1 (item 2): the canyon rock. Triplanar detail (no UVs needed), sediment layers by height on the walls,
// dust on flat ground, darker canyon floors, the main light with shadows, and the shared height haze (HHCommon.hlsl).
// Round 4 (A5): _Mode 0 canyon rock, 1 forest (moss and leaf litter on the flats), 2 dead city (concrete facades with a window grid
// and a few flickering dead lights on the steep faces, asphalt and rubble on the flats), 3 ice (glacial blue bands, snow on the
// flats, a cold sheen, deep blue down in the crevasses). Palettes and colours per biome: WorldLook.ApplyBiome.
// Round 7: 4 volcanic (black basalt, ash on the flats, the walls lit orange from the lava below, glowing cracks near it), 5 crystal
// forest (dark ground, luminous moss / fungus patches on the flats), 6 ocean islands (sea cliffs, sand just above the water, wet
// rock and moving surf where the shore meets the sea).
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
        _Hot ("Lava light / moss glow (Round 7)", Color) = (1, 0.42, 0.08, 1)
        _RockTex ("Rock colour (Round 11)", 2D) = "gray" {}
        _RockNrm ("Rock normal", 2D) = "bump" {}
        _GroundTex ("Ground colour", 2D) = "gray" {}
        _GroundNrm ("Ground normal", 2D) = "bump" {}
        _ScreeTex ("Scree colour", 2D) = "gray" {}
        _ScreeNrm ("Scree normal", 2D) = "bump" {}
        _Photo ("Photo textures on (Round 11)", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        TEXTURE2D(_Detail); SAMPLER(sampler_Detail);
        TEXTURE2D(_Strata); SAMPLER(sampler_Strata);
        TEXTURE2D(_RockTex); SAMPLER(sampler_RockTex); TEXTURE2D(_RockNrm); SAMPLER(sampler_RockNrm);
        TEXTURE2D(_GroundTex); SAMPLER(sampler_GroundTex); TEXTURE2D(_GroundNrm); SAMPLER(sampler_GroundNrm);
        TEXTURE2D(_ScreeTex); SAMPLER(sampler_ScreeTex); TEXTURE2D(_ScreeNrm); SAMPLER(sampler_ScreeNrm);
        CBUFFER_START(UnityPerMaterial)
        float4 _Detail_ST, _Strata_ST, _Dust, _AmbientSky, _AmbientGround, _Flat, _Deep, _Hot; float _Mode;
        float4 _RockTex_ST, _RockNrm_ST, _GroundTex_ST, _GroundNrm_ST, _ScreeTex_ST, _ScreeNrm_ST; float _Photo;
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

            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; float3 posWS : TEXCOORD0; float3 nWS : TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };

            V vert(A i)
            {
                V o; UNITY_SETUP_INSTANCE_ID(i); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); o.posWS = TransformObjectToWorld(i.positionOS.xyz); o.nWS = TransformObjectToWorldNormal(i.normalOS);
                o.positionCS = TransformWorldToHClip(o.posWS); return o;
            }

            float4 Tri(float3 p, float3 w, float scale)
            {
                return SAMPLE_TEXTURE2D(_Detail, sampler_Detail, p.zy * scale) * w.x
                     + SAMPLE_TEXTURE2D(_Detail, sampler_Detail, p.xz * scale) * w.y
                     + SAMPLE_TEXTURE2D(_Detail, sampler_Detail, p.xy * scale) * w.z;
            }

            float Hash2(float2 q) { return frac(sin(dot(q, float2(127.1, 311.7))) * 43758.5453); }

            // Round 11: triplanar colour and normals of the photo sets
            float3 TriTex(TEXTURE2D_PARAM(t, st), float3 p, float3 w, float scale)
            {
                return SAMPLE_TEXTURE2D(t, st, p.zy * scale).rgb * w.x + SAMPLE_TEXTURE2D(t, st, p.xz * scale).rgb * w.y + SAMPLE_TEXTURE2D(t, st, p.xy * scale).rgb * w.z;
            }
            // whiteout blend (the tangent normals laid on each projection plane and summed by the weights)
            float3 TriNrm(TEXTURE2D_PARAM(t, st), float3 p, float3 n, float3 w, float scale, float strength)
            {
                float3 tx = UnpackNormalScale(SAMPLE_TEXTURE2D(t, st, p.zy * scale), strength);
                float3 ty = UnpackNormalScale(SAMPLE_TEXTURE2D(t, st, p.xz * scale), strength);
                float3 tz = UnpackNormalScale(SAMPLE_TEXTURE2D(t, st, p.xy * scale), strength);
                tx = float3(tx.xy + n.zy, abs(tx.z) * n.x);
                ty = float3(ty.xy + n.xz, abs(ty.z) * n.y);
                tz = float3(tz.xy + n.xy, abs(tz.z) * n.z);
                return normalize(tx.zyx * w.x + ty.xzy * w.y + tz.xyz * w.z);
            }
            // a planar (top-down) normal on the ground
            float3 TopNrm(TEXTURE2D_PARAM(t, st), float3 p, float3 n, float scale, float strength)
            {
                float3 ty = UnpackNormalScale(SAMPLE_TEXTURE2D(t, st, p.xz * scale), strength);
                ty = float3(ty.xy + n.xz, abs(ty.z) * n.y);
                return normalize(ty.xzy);
            }

            half4 frag(V i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
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
                if (_Mode > 2.5 && _Mode < 3.5) rock = strata * (0.86 + 0.22 * mottle) * (0.93 + 0.1 * grain) * lerp(0.9, 1.0, crack);   // ice: smoother than rock
                float3 albedo = lerp(rock, dust, flat);
                float3 nl = n;   // the normal the light sees (Round 11: with the photo sets' normal maps)
                if (_Photo > 0.5)
                {
                    float dist = distance(p, _WorldSpaceCameraPos);
                    bool canyon = _Mode < 0.5, ice = _Mode > 2.5 && _Mode < 3.5, city = _Mode > 1.5 && _Mode < 2.5;
                    // walls: two scales mixed by a large noise (no visible repeat), the strata bands from the height
                    float3 photoA = TriTex(TEXTURE2D_ARGS(_RockTex, sampler_RockTex), p, w, 1.0 / 7.3);
                    float3 photoB = TriTex(TEXTURE2D_ARGS(_RockTex, sampler_RockTex), p + 17.0, w, 1.0 / 21.0);
                    float3 photo = lerp(photoA, photoB, smoothstep(0.35, 0.65, big.g));
                    float lum = dot(photo, float3(0.3, 0.59, 0.11));
                    float3 wall = canyon ? lerp(photo * 1.55, strata * (0.35 + lum * 2.9), 0.62) * (0.85 + 0.3 * mottle)
                                         : strata * lerp(1.0, lum * 2.4, ice ? 0.35 : 0.85) * (0.86 + 0.28 * mottle);
                    // desert varnish: dark streaks running down the steep faces (from the ledges), stronger low on the walls
                    float2 vu = float2(abs(n.x) > abs(n.z) ? p.z : p.x, p.y);
                    float streak = SAMPLE_TEXTURE2D(_Detail, sampler_Detail, float2(vu.x / 37.0, vu.y / 520.0)).r * smoothstep(0.45, 0.7, SAMPLE_TEXTURE2D(_Detail, sampler_Detail, float2(vu.x / 160.0 + 0.3, vu.y / 90.0)).g);
                    float steep = 1.0 - saturate(abs(n.y) * 1.6);
                    wall *= lerp(1.0, canyon ? 0.62 : 0.82, smoothstep(0.32, 0.55, streak) * steep * (ice ? 0.3 : 1.0));
                    // scree: the slopes between wall and floor and the foot of the walls (rubble)
                    float slope = smoothstep(_Flat.x - 0.3, _Flat.x - 0.08, n.y) * (1.0 - smoothstep(_Flat.x, _Flat.y, n.y));
                    float foot = slope * saturate(1.15 - p.y / 260.0);
                    float3 scree = SAMPLE_TEXTURE2D(_ScreeTex, sampler_ScreeTex, p.xz / 6.0).rgb;
                    float sl = dot(scree, float3(0.3, 0.59, 0.11));
                    float3 screeCol = canyon ? lerp(scree * 1.2, strata * (0.3 + sl * 2.4), 0.55) : strata * sl * 2.3;
                    // flats: the dusty ground set
                    float3 gr = SAMPLE_TEXTURE2D(_GroundTex, sampler_GroundTex, p.xz / 5.0).rgb;
                    float gl = dot(gr, float3(0.3, 0.59, 0.11));
                    float3 gnd = canyon ? lerp(gr * 1.25, _Dust.rgb * gl * 2.4, 0.45) : _Dust.rgb * lerp(1.0, gl * 2.3, ice ? 0.25 : 0.8);
                    gnd *= (0.85 + 0.3 * mottle);
                    if (!city)
                    {
                        // keep each biome's own flats on top (forest litter, ice snow, ash) but in the photo's light and shade
                        float3 dustPhoto = (_Mode > 0.5 && _Mode < 1.5) ? dust * lerp(1.0, gl * 2.2, 0.7) : gnd;
                        float3 wallPhoto = (_Mode > 0.5 && _Mode < 1.5) ? lerp(wall, rock, 0.5) : wall;
                        albedo = lerp(lerp(wallPhoto, screeCol, foot * 0.85), dustPhoto, flat);
                    }
                    else albedo = lerp(albedo, albedo * lerp(1.0, gl * 2.2, 0.6), flat);
                    // normals: the rock everywhere within 700 m, the ground / scree on the flats, a fine detail normal close up
                    if (dist < 700.0)
                    {
                        float fade = 1.0 - smoothstep(350.0, 700.0, dist);
                        float3 rn = TriNrm(TEXTURE2D_ARGS(_RockNrm, sampler_RockNrm), p, n, w, 1.0 / 7.3, 1.0);
                        float3 gn = TopNrm(TEXTURE2D_ARGS(_GroundNrm, sampler_GroundNrm), p, n, 1.0 / 5.0, 1.0);
                        float3 sn = TopNrm(TEXTURE2D_ARGS(_ScreeNrm, sampler_ScreeNrm), p, n, 1.0 / 6.0, 1.2);
                        float3 m = lerp(lerp(rn, sn, foot * 0.85), gn, flat);
                        if (dist < 60.0) m = normalize(m + (TriNrm(TEXTURE2D_ARGS(_RockNrm, sampler_RockNrm), p, n, w, 1.0 / 1.9, 0.8) - n) * (1.0 - flat) * (1.0 - dist / 60.0));
                        nl = normalize(lerp(n, m, fade * (city ? 0.4 : 1.0)));
                    }
                }
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
                else if (_Mode > 2.5 && _Mode < 3.5)
                {
                    // ice: a cold sheen on the steep ice (snow on the flats comes from _Dust)
                    spec = (1 - flat) * 0.55;
                }
                else if (_Mode > 3.5 && _Mode < 4.5)
                {
                    // volcanic: basalt with a faint sheen; lit from the lava below (falls off over 25 m); glowing cracks just above it
                    spec = (1 - flat) * 0.12;
                    float low = exp(-max(p.y, 0.0) / 25.0);
                    glow = _Hot.rgb * low * (0.55 + 0.45 * (1.0 - n.y)) * 0.9 * albedo * 3.0;
                    glow += _Hot.rgb * smoothstep(0.55, 0.95, 1.0 - crack) * exp(-max(p.y, 0.0) / 4.0) * 1.4 * (0.8 + 0.2 * sin(_Time.y * 1.7 + p.x * 0.05));
                }
                else if (_Mode > 4.5 && _Mode < 5.5)
                {
                    // crystal forest: luminous moss and fungus in patches on the flats, slowly breathing
                    float patchy = smoothstep(0.6, 0.72, big.g) * smoothstep(0.3, 0.7, fine.r);
                    glow = _Hot.rgb * patchy * flat * (0.7 + 0.3 * sin(_Time.y * 0.8 + big.r * 12.0)) * 0.55;
                }
                else if (_Mode > 5.5)
                {
                    // ocean islands: sand on the low flats, wet dark rock and moving surf at the waterline (y 0)
                    float sand = smoothstep(5.0, 1.5, p.y) * smoothstep(-1.5, 0.3, p.y) * smoothstep(0.55, 0.8, n.y);
                    albedo = lerp(albedo, float3(0.62, 0.55, 0.42) * (0.85 + 0.25 * grain), sand);
                    float wet = smoothstep(3.0, 0.4, p.y);
                    albedo *= lerp(1.0, 0.55, wet * (1 - sand * 0.5));
                    float wave = sin(_Time.y * 1.6 + dot(p.xz, float2(0.06, 0.045))) * 0.45 + sin(_Time.y * 0.9 - p.x * 0.03) * 0.3;
                    float surf = smoothstep(1.4, 0.2, p.y - wave) * smoothstep(-0.8, 0.0, p.y) * (0.6 + 0.4 * fine.g);
                    albedo = lerp(albedo, float3(0.92, 0.95, 0.96), surf * 0.85);
                    spec = wet * 0.4;
                }

                Light L = GetMainLight(TransformWorldToShadowCoord(p));
                float ndl = saturate(dot(nl, L.direction));
                float cavity = lerp(0.62, 1.0, saturate((p.y + 10.0) / 170.0)) * lerp(_Mode > 2.5 && _Mode < 3.5 ? 0.97 : 0.8, 1.0, crack);   // deep canyons and cracks get less sky
                float3 amb = lerp(_AmbientGround.rgb, _AmbientSky.rgb, n.y * 0.5 + 0.5) * cavity;
                amb *= lerp(_Deep.rgb, float3(1, 1, 1), saturate(p.y / 160.0));   // ice: blue down in the crevasses
                float3 col = albedo * (L.color * ndl * L.shadowAttenuation + amb);
                if (spec > 0)
                {
                    float3 hv = normalize(L.direction - v);
                    col += L.color * pow(saturate(dot(nl, hv)), 48.0) * spec * L.shadowAttenuation;
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
            struct A { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct D { float4 c : SV_POSITION; UNITY_VERTEX_OUTPUT_STEREO };
            D vert(A i) { D o; UNITY_SETUP_INSTANCE_ID(i); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); o.c = TransformObjectToHClip(i.positionOS.xyz); return o; }
            half frag(D i) : SV_Target { UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i); return 0; }
            ENDHLSL
        }
    }
}
