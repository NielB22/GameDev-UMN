// Light VHS look as one full-screen pass (URP Full Screen Pass Renderer Feature, after post-processing).
// Keeps the picture clear: brightness (luma) stays sharp and blacks stay black, exactly like the
// source image. Only the COLOR is "taped":
// - color bleed: red shifted one way, blue the other (classic VHS fringe on bright edges)
// - color smear: the color is slightly smeared sideways (tape chroma is low resolution)
// - tiny per-line jitter
// Strength comes from the global _VhsIntensity (set by VhsEffect); 0 = image unchanged.
Shader "CyberWatch/VHS"
{
    Properties
    {
        _ChromaShift ("Color bleed (pixels)", Range(0, 6)) = 2.5
        _ChromaSmear ("Color smear (pixels)", Range(0, 6)) = 2
        _Jitter ("Line jitter (pixels)", Range(0, 3)) = 0.6
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off Cull Off ZTest Always

        Pass
        {
            Name "VHS"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _ChromaShift;
            float _ChromaSmear;
            float _Jitter;
            float _VhsIntensity; // global

            float Hash(float n) { return frac(sin(n) * 43758.5453); }

            float3 Fetch(float2 uv)
            {
                return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb;
            }

            float Luma(float3 c) { return dot(c, float3(0.299, 0.587, 0.114)); }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                float k = saturate(_VhsIntensity);
                if (k <= 0.0001)
                    return half4(Fetch(uv), 1);

                float px = 1.0 / _ScreenParams.x;

                // Each pair of lines wobbles a little, changing 24 times a second.
                float lineId = floor(uv.y * _ScreenParams.y * 0.5);
                float frame = floor(_Time.y * 24.0);
                uv.x += (Hash(lineId * 12.9898 + frame * 78.233) - 0.5) * 2.0 * _Jitter * k * px;

                // Sharp brightness from the pixel itself (keeps contrast and black levels).
                float3 center = Fetch(uv);
                float y = Luma(center);

                // Color taken from shifted, slightly smeared samples (red one way, blue the other).
                float s = _ChromaShift * k * px;
                float m = _ChromaSmear * k * px;
                float3 right = (Fetch(uv + float2(s, 0)) + Fetch(uv + float2(s + m, 0))) * 0.5;
                float3 left  = (Fetch(uv - float2(s, 0)) + Fetch(uv - float2(s + m, 0))) * 0.5;
                float3 mid   = (Fetch(uv + float2(m, 0)) + Fetch(uv - float2(m, 0))) * 0.5;

                // Rebuild: keep the sharp luma, use the bled color differences.
                float3 col;
                col.r = y + (right.r - Luma(right));
                col.g = y + (mid.g - Luma(mid));
                col.b = y + (left.b - Luma(left));

                return half4(max(col, 0.0), 1);
            }
            ENDHLSL
        }
    }
}
