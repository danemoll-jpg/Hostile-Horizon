// Hostile Horizon, Round 7 (B2, B4): the liquid surfaces. _Mode 0 = the sea (moving waves from scrolling detail noise and long
// swells, the sky reflected by Fresnel, the sun's glint, foam where the drawn shore meets it comes from the rock shader); 1 = lava
// (a dark cooling crust drifting over glowing cracks, pulsing; emissive, so it lights nothing itself: the rock shader lights the
// walls above it). Opaque, one flat grid that follows the viewer (Terrain/Liquid.cs), the shared haze.
Shader "HostileHorizon/Liquid"
{
    Properties
    {
        _Detail ("Detail (R mottle, G grain, B cracks)", 2D) = "gray" {}
        _Mode ("0 sea, 1 lava", Float) = 0
        _Deep ("Deep colour", Color) = (0.03, 0.12, 0.16, 1)
        _Shallow ("Light colour", Color) = (0.10, 0.30, 0.34, 1)
        _SkyLow ("Sky near the horizon", Color) = (0.6, 0.7, 0.8, 1)
        _SkyHigh ("Sky overhead", Color) = (0.2, 0.4, 0.6, 1)
        _Hot ("Lava glow", Color) = (1.0, 0.42, 0.08, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry+1" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "HHCommon.hlsl"
            TEXTURE2D(_Detail); SAMPLER(sampler_Detail);
            CBUFFER_START(UnityPerMaterial)
            float4 _Detail_ST, _Deep, _Shallow, _SkyLow, _SkyHigh, _Hot; float _Mode;
            CBUFFER_END

            struct A { float4 positionOS : POSITION; };
            struct V { float4 positionCS : SV_POSITION; float3 posWS : TEXCOORD0; };
            V vert(A i) { V o; o.posWS = TransformObjectToWorld(i.positionOS.xyz); o.positionCS = TransformWorldToHClip(o.posWS); return o; }

            float H(float2 p) { return SAMPLE_TEXTURE2D(_Detail, sampler_Detail, p).r; }

            half4 frag(V i) : SV_Target
            {
                float3 p = i.posWS; float t = _Time.y;
                float3 v = normalize(p - _WorldSpaceCameraPos); float dist = distance(p, _WorldSpaceCameraPos);
                float3 col;
                if (_Mode < 0.5)
                {
                    // the sea: two scrolling noise layers + long swells give the normal; fewer details far away
                    float fade = saturate(1.0 - dist / 1400.0);
                    float2 a = p.xz / 47.0 + float2(t * 0.021, t * 0.013), b = p.xz / 19.0 + float2(-t * 0.031, t * 0.024);
                    float e = 0.004;
                    float2 g = float2(H(a + float2(e, 0)) - H(a - float2(e, 0)), H(a + float2(0, e)) - H(a - float2(0, e))) * 0.9
                             + float2(H(b + float2(e, 0)) - H(b - float2(e, 0)), H(b + float2(0, e)) - H(b - float2(0, e))) * 0.5;
                    float sw = cos(dot(p.xz, float2(0.011, 0.006)) - t * 0.9) * 0.08 + cos(dot(p.xz, float2(-0.004, 0.013)) - t * 0.7) * 0.06;
                    float3 n = normalize(float3(g.x * 9.0 * fade + sw, 1.0, g.y * 9.0 * fade + sw * 0.6));
                    float3 r = reflect(v, n);
                    float fres = 0.04 + 0.96 * pow(1.0 - saturate(dot(-v, n)), 5.0);
                    float3 sky = lerp(_SkyLow.rgb, _SkyHigh.rgb, saturate(r.y * 2.2));
                    sky = lerp(sky, HHHazeColor(r), 0.35);
                    Light L = GetMainLight(TransformWorldToShadowCoord(p));
                    float3 body = lerp(_Deep.rgb, _Shallow.rgb, saturate(0.35 + 0.5 * n.y - 0.5 + H(a * 0.3) * 0.6)) * (0.35 + 0.65 * L.shadowAttenuation) * (0.5 + 0.5 * saturate(L.direction.y + 0.3));
                    col = lerp(body, sky, fres);
                    float3 hv = normalize(L.direction - v);
                    col += L.color * pow(saturate(dot(n, hv)), 220.0) * 3.0 * L.shadowAttenuation;   // the sun's glint
                    col += L.color * pow(saturate(dot(n, hv)), 24.0) * 0.08 * L.shadowAttenuation;
                    // whitecaps: a few, moving
                    float cap = smoothstep(0.78, 0.86, H(p.xz / 31.0 + float2(t * 0.05, -t * 0.03)) * H(p.xz / 13.0 - float2(t * 0.04, t * 0.02)) * 2.2);
                    col = lerp(col, float3(0.85, 0.9, 0.92) * (0.5 + 0.5 * L.shadowAttenuation), cap * 0.5 * fade);
                }
                else
                {
                    // lava: a dark crust drifting slowly; the cracks between its plates glow and pulse
                    float2 a = p.xz / 38.0 + float2(t * 0.006, t * 0.004);
                    float4 d1 = SAMPLE_TEXTURE2D(_Detail, sampler_Detail, a), d2 = SAMPLE_TEXTURE2D(_Detail, sampler_Detail, p.xz / 9.0 - float2(t * 0.012, t * 0.008));
                    float crack = 1.0 - saturate(d1.b * 1.2);                       // the detail's crack lines
                    float heat = saturate(crack * 1.4 + (1.0 - d1.r) * 0.55 + d2.g * 0.25 - 0.35);
                    float pulse = 0.85 + 0.15 * sin(t * 1.3 + d1.r * 9.0);
                    float3 crust = float3(0.05, 0.035, 0.03) * (0.7 + 0.6 * d2.g);
                    float3 glow = lerp(_Hot.rgb * 0.6, float3(1.0, 0.85, 0.45), pow(heat, 3.0)) * pow(heat, 1.6) * 3.2 * pulse;
                    col = crust + glow;
                }
                float haze = HHHazeAmount(p, _WorldSpaceCameraPos);
                if (_Mode > 0.5) haze *= 0.75;   // the glow carries through the haze a little
                col = lerp(col, HHHazeColor(v), haze);
                return half4(col, 1);
            }
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A { float4 positionOS : POSITION; };
            float4 vert(A i) : SV_POSITION { return TransformObjectToHClip(i.positionOS.xyz); }
            half frag() : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
