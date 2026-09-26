Shader "Museum/HorrorVideoGrade"
{
    Properties
    {
        [PerRendererData] _MainTex ("Video Texture", 2D) = "white" {}

        _Tint ("Color Tint", Color) = (0.72, 0.88, 0.76, 1)

        _Brightness ("Brightness", Range(0.1, 1.5)) = 0.70
        _Contrast ("Contrast", Range(0.5, 2.0)) = 1.30
        _Saturation ("Saturation", Range(0.0, 2.0)) = 0.55

        _VignetteStrength ("Vignette Strength", Range(0.0, 1.0)) = 0.55
        _VignetteSize ("Vignette Size", Range(0.1, 1.0)) = 0.65
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;

                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _Tint;
                float _Brightness;
                float _Contrast;
                float _Saturation;
                float _VignetteStrength;
                float _VignetteSize;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);

                // Unity 6 URP-safe initialization.
                Varyings output = (Varyings)0;

                // Pass the correct eye index through the vertex shader.
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionHCS =
                    TransformObjectToHClip(input.positionOS.xyz);

                output.uv = input.uv;
                output.color = input.color;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Restore the correct stereo eye index.
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half4 col =
                    SAMPLE_TEXTURE2D(
                        _MainTex,
                        sampler_MainTex,
                        input.uv
                    );

                // Desaturation
                half luminance =
                    dot(
                        col.rgb,
                        half3(0.2126, 0.7152, 0.0722)
                    );

                col.rgb =
                    lerp(
                        luminance.xxx,
                        col.rgb,
                        _Saturation
                    );

                // Contrast
                col.rgb =
                    (col.rgb - 0.5) *
                    _Contrast +
                    0.5;

                // Brightness / darkness
                col.rgb *= _Brightness;

                // Horror tint
                col.rgb *= _Tint.rgb;

                // Vignette
                float2 centeredUV =
                    input.uv - 0.5;

                float distanceFromCenter =
                    length(centeredUV);

                float vignette =
                    smoothstep(
                        _VignetteSize,
                        0.15,
                        distanceFromCenter
                    );

                col.rgb *=
                    lerp(
                        1.0,
                        vignette,
                        _VignetteStrength
                    );

                col.a *= input.color.a;

                return col;
            }

            ENDHLSL
        }
    }
}