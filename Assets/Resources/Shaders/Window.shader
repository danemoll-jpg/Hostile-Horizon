// Hostile Horizon, Fix 8.1: the floor window's thick glass. Transparent with a slight tint, the sky's reflection at grazing
// angles (URP's reflection probe), and fine scratches (procedural, in the ship's own space so they stay on the glass).
// Single-pass-instanced safe (VR: both eyes), like every shader of the game.
Shader "HostileHorizon/Window"
{
    Properties
    {
        _Color ("Tint", Color) = (0.10, 0.16, 0.15, 0.22)
        _Reflect ("Reflection", Float) = 0.55
        _Scratch ("Scratches", Float) = 0.35
    }
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial) float4 _Color; float _Reflect, _Scratch; CBUFFER_END
            struct A { float4 p : POSITION; float3 n : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 c : SV_POSITION; float3 w : TEXCOORD0; float3 n : TEXCOORD1; float3 o : TEXCOORD2; UNITY_VERTEX_OUTPUT_STEREO };
            V vert(A i)
            {
                V o; UNITY_SETUP_INSTANCE_ID(i); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.w = TransformObjectToWorld(i.p.xyz); o.n = TransformObjectToWorldNormal(i.n); o.o = i.p.xyz; o.c = TransformWorldToHClip(o.w); return o;
            }
            float Hash(float2 q) { return frac(sin(dot(q, float2(127.1, 311.7))) * 43758.5453); }
            // a thin scratch per 6 cm cell in about a third of the cells: a short straight line at a random angle
            float Scratches(float2 p)
            {
                float s = 0;
                for (int k = 0; k < 2; k++)
                {
                    float2 q = p / (0.06 + k * 0.035) + k * 17.3; float2 c = floor(q), f = frac(q) - 0.5;
                    float h = Hash(c); if (h > 0.34) continue;
                    float a = Hash(c + 3.1) * 6.2832; float2 d = float2(cos(a), sin(a));
                    float along = dot(f, d), across = abs(dot(f, float2(-d.y, d.x)));
                    float len = 0.25 + 0.25 * Hash(c + 7.7);
                    s += (1 - smoothstep(0.004, 0.012, across)) * (1 - smoothstep(len * 0.7, len, abs(along))) * (0.5 + h);
                }
                return saturate(s);
            }
            half4 frag(V i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float3 n = normalize(i.n), v = normalize(_WorldSpaceCameraPos - i.w);
                float nv = saturate(abs(dot(n, v)));
                float fres = 0.06 + 0.94 * pow(1 - nv, 5);
                half3 refl = GlossyEnvironmentReflection(reflect(-v, n), 0.08, 1.0);
                float sc = Scratches(i.o.xz) * _Scratch;
                float3 col = lerp(_Color.rgb, refl, saturate(fres * _Reflect * 2)) + sc * 0.35;
                float a = saturate(_Color.a + fres * _Reflect + sc * 0.4);
                return half4(col, a);
            }
            ENDHLSL
        }
    }
}
