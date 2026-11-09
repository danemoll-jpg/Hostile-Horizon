// Hostile Horizon, Fix 8.2: the menu board in VR, drawn over everything (no depth test, last in the frame), so the cockpit never
// cuts through it (Dan: the Field Journal "clipped through the cockpit and I couldn't read it"). Same inputs as Unity's UI/Default
// (texture, tint, vertex colour, the font's alpha texture via _TextureSampleAdd); single-pass-instanced safe (CLAUDE.md rule 7).
Shader "HostileHorizon/UIOnTop"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "Queue"="Overlay" "IgnoreProjector"="True" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "CanUseSpriteAtlas"="True" }
        Cull Off ZWrite Off ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial) float4 _Color; float4 _MainTex_ST; CBUFFER_END
            float4 _TextureSampleAdd;
            struct A { float4 p : POSITION; float4 col : COLOR; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 c : SV_POSITION; float4 col : COLOR; float2 uv : TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            V vert(A i)
            {
                V o; UNITY_SETUP_INSTANCE_ID(i); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.c = TransformObjectToHClip(i.p.xyz); o.col = i.col * _Color; o.uv = i.uv; return o;
            }
            half4 frag(V i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                return (SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) + _TextureSampleAdd) * i.col;
            }
            ENDHLSL
        }
    }
}
