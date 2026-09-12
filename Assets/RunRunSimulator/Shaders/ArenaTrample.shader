Shader "MoriMonchi/ArenaTrample"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "black" {}
        _FreshDecay ("Fresh Decay", Float) = 1
        _TrailDecay ("Trail Decay", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        ENDHLSL

        Pass
        {
            Name "Decay"
            ZTest Always
            ZWrite Off
            Cull Off
            Blend Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float _FreshDecay;
            float _TrailDecay;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                return col * half4(_FreshDecay, _FreshDecay, _FreshDecay, _TrailDecay);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Stamp"
            ZTest Always
            ZWrite Off
            Cull Off
            Blend One One
            BlendOp Add

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 d = input.uv * 2.0 - 1.0;
                float r = length(d);
                if (r > 1.0) discard;

                float f = saturate(1.0 - r);
                f = f * f;

                float2 push = normalize(d + 1e-5) * f * input.color.r;
                return float4(push, f * input.color.r, f * input.color.g);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
