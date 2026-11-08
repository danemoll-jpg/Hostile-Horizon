// Hostile Horizon, Round 4 (A2, A6): the giant trees (and other instanced props). GPU instancing, vertex colours, triplanar detail
// (the rock's detail texture), a colour shift per instance (from its position), the main light with shadows, a little light through
// the leaves, and the shared haze (HHCommon.hlsl). Material: Resources/Materials/Prop.mat (made by PlayerBuild, instancing on).
// Round 7 (B3): _Crystal 1 = the Crystal Forest's spires and shards: glassy facets (a sharp sun glint, the sky by Fresnel), glowing
// from inside in the tint's cold colour (brighter towards the tip: vertex alpha), slow inner bands. Not real refraction.
Shader "HostileHorizon/Prop"
{
    Properties
    {
        _Detail ("Detail (R mottle, G grain, B cracks)", 2D) = "gray" {}
        _Tint ("Tint", Color) = (1, 1, 1, 1)
        _Vary ("Colour variation per instance", Color) = (0.25, 0.2, 0.1, 0)
        _AmbientSky ("Ambient from the sky", Color) = (0.36, 0.44, 0.30, 1)
        _AmbientGround ("Ambient from the ground", Color) = (0.16, 0.14, 0.08, 1)
        _Translucent ("Light through (leaves)", Float) = 0
        _Glow ("Glow spots", Float) = 0
        _Crystal ("Crystal (Round 7)", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        TEXTURE2D(_Detail); SAMPLER(sampler_Detail);
        CBUFFER_START(UnityPerMaterial)
        float4 _Detail_ST, _Tint, _Vary, _AmbientSky, _AmbientGround; float _Translucent, _Glow, _Crystal;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "HHCommon.hlsl"

            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; float4 color : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; float3 posWS : TEXCOORD0; float3 nWS : TEXCOORD1; float4 color : COLOR; float3 origin : TEXCOORD2; };

            V vert(A i)
            {
                UNITY_SETUP_INSTANCE_ID(i);
                V o; o.posWS = TransformObjectToWorld(i.positionOS.xyz); o.nWS = TransformObjectToWorldNormal(i.normalOS);
                o.positionCS = TransformWorldToHClip(o.posWS); o.color = i.color; o.origin = TransformObjectToWorld(float3(0, 0, 0)); return o;
            }

            float Hash(float3 p) { return frac(sin(dot(p, float3(12.9898, 78.233, 37.719))) * 43758.5453); }

            half4 frag(V i) : SV_Target
            {
                float3 n = normalize(i.nWS); float3 p = i.posWS;
                float3 w = pow(abs(n), 4.0); w /= (w.x + w.y + w.z);
                float4 d = SAMPLE_TEXTURE2D(_Detail, sampler_Detail, p.zy / 7.0) * w.x + SAMPLE_TEXTURE2D(_Detail, sampler_Detail, p.xz / 7.0) * w.y + SAMPLE_TEXTURE2D(_Detail, sampler_Detail, p.xy / 7.0) * w.z;
                float h = Hash(floor(i.origin * 0.37));
                float3 albedo = i.color.rgb * _Tint.rgb * (1.0 + (h - 0.5) * 2.0 * _Vary.rgb) * (0.7 + 0.5 * d.r) * lerp(0.75, 1.0, d.b);

                Light L = GetMainLight(TransformWorldToShadowCoord(p));
                float ndl = saturate(dot(n, L.direction));
                float through = _Translucent * saturate(dot(-n, L.direction)) * 0.5;   // sun through the leaves from behind
                float3 amb = lerp(_AmbientGround.rgb, _AmbientSky.rgb, n.y * 0.5 + 0.5) * lerp(0.55, 1.0, i.color.a);   // vertex alpha: occlusion
                float3 col = albedo * (L.color * (ndl + through) * L.shadowAttenuation + amb);
                // bioluminescent spots on the bark (the forest's glow)
                float spot = smoothstep(0.83, 0.9, d.g) * _Glow * (1 - i.color.a * 0.6);
                col += float3(0.25, 0.9, 0.7) * spot * (0.6 + 0.4 * sin(_Time.y * 0.8 + h * 30));

                float3 v = normalize(p - _WorldSpaceCameraPos);
                if (_Crystal > 0.5)
                {
                    float fres = pow(1.0 - saturate(abs(dot(n, -v))), 3.0);
                    float3 r = reflect(v, n);
                    float3 sky = lerp(_AmbientGround.rgb, _AmbientSky.rgb, saturate(r.y * 0.5 + 0.5)) * 1.4;
                    float3 hv = normalize(L.direction - v);
                    float glint = pow(saturate(dot(n, hv)), 120.0) * 3.0 * L.shadowAttenuation;
                    float bands = 0.65 + 0.35 * sin(dot(p, float3(0.05, 0.11, 0.07)) + _Time.y * 0.4 + h * 12.0);
                    float3 core = _Tint.rgb * lerp(0.25, 1.0, i.color.a) * bands * (0.85 + 0.15 * sin(_Time.y * 0.7 + h * 40.0));
                    col = core * (0.55 + 0.45 * d.g) + sky * fres * 0.9 + L.color * glint + albedo * L.color * ndl * L.shadowAttenuation * 0.15;
                }
                col = lerp(col, HHHazeColor(v), HHHazeAmount(p, _WorldSpaceCameraPos) * (_Crystal > 0.5 ? 0.85 : 1.0));
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
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            float3 _LightDirection; float3 _LightPosition;
            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            float4 vert(A i) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(i);
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
            #pragma multi_compile_instancing
            struct A { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            float4 vert(A i) : SV_POSITION { UNITY_SETUP_INSTANCE_ID(i); return TransformObjectToHClip(i.positionOS.xyz); }
            half frag() : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
