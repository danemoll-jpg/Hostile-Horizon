// Hostile Horizon, Round 1 (item 2): the alien sky. A teal zenith over a dusty amber horizon, the sun (direction from the main
// light), and a big banded gas giant hanging over the canyons. The horizon colour is the rock's haze colour (HHCommon.hlsl).
Shader "HostileHorizon/Sky"
{
    Properties
    {
        _Zenith ("Zenith", Color) = (0.17, 0.38, 0.48, 1)
        _SunColor ("Sun", Color) = (1.0, 0.94, 0.82, 1)
        _PlanetDir ("Planet direction", Vector) = (-0.55, 0.42, 0.72, 0)
        _PlanetA ("Planet colour A", Color) = (0.72, 0.62, 0.52, 1)
        _PlanetB ("Planet colour B", Color) = (0.42, 0.30, 0.30, 1)
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "HHCommon.hlsl"
            float4 _Zenith, _SunColor, _PlanetDir, _PlanetA, _PlanetB;
            struct A { float4 positionOS : POSITION; };
            struct V { float4 positionCS : SV_POSITION; float3 dir : TEXCOORD0; };
            V vert(A i) { V o; o.positionCS = TransformObjectToHClip(i.positionOS.xyz); o.dir = i.positionOS.xyz; return o; }

            float Hash(float n) { return frac(sin(n) * 43758.5453); }

            half4 frag(V i) : SV_Target
            {
                float3 v = normalize(i.dir); float3 sun = normalize(_HHSunDir.xyz);
                float up = saturate(v.y);
                float3 col = lerp(HHHazeColor(v), _Zenith.rgb, pow(up, 0.6));
                if (v.y < 0) col = HHHazeColor(v) * lerp(1.0, 0.85, saturate(-v.y * 4));

                // gas giant: a lit disc with bands and a thin bright rim, behind the haze near the horizon
                float3 pd = normalize(_PlanetDir.xyz); float R = 0.16;   // angular radius about 9 degrees
                float3 rgt = normalize(cross(float3(0, 1, 0), pd)); float3 upp = cross(pd, rgt);
                float3 off = v - pd * dot(v, pd); float2 q = float2(dot(off, rgt), dot(off, upp)) / R;
                float r2 = dot(q, q);
                if (dot(v, pd) > 0 && r2 < 1.0)
                {
                    float z = sqrt(1.0 - r2); float3 nrm = normalize(rgt * q.x + upp * q.y - pd * z);   // the face turned to us
                    float lat = q.y * 0.94 + q.x * 0.34;
                    float bands = sin(lat * 23.0) * 0.5 + 0.5; bands = lerp(bands, sin(lat * 61.0 + bands * 3.0) * 0.5 + 0.5, 0.35);
                    float3 pc = lerp(_PlanetB.rgb, _PlanetA.rgb, bands);
                    float lit = saturate(dot(nrm, sun) * 0.9 + 0.15);
                    float3 p = pc * lit + _Zenith.rgb * 0.25;
                    float edge = smoothstep(1.0, 0.96, r2);
                    float haze = 1.0 - smoothstep(0.0, 0.35, v.y);
                    col = lerp(col, lerp(p, col, haze * 0.75), edge);
                }
                float rim = smoothstep(1.12, 1.0, r2) * smoothstep(0.9, 1.0, r2) * step(0, dot(v, pd));
                col += rim * 0.12 * _PlanetA.rgb;

                // sun: disc, glow, and a faint ring of haze
                float s = dot(v, sun);
                col += _SunColor.rgb * (smoothstep(0.99955, 0.99975, s) * 6.0 + pow(saturate(s), 900.0) * 1.5 + pow(saturate(s), 40.0) * 0.18);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
