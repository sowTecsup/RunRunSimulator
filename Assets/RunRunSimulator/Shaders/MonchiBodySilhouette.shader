Shader "MoriMonchi/MonchiBodySilhouette"
{
    Properties
    {
        _Color ("Silhouette", Color) = (0.62, 0.86, 1, 0.55)
        _EdgeColor ("Edge", Color) = (1, 1, 1, 0.9)
        _EdgePower ("Edge Power", Range(0.5, 8)) = 2.5
        _BaseColor ("Body Tint", Color) = (1, 1, 1, 1)
        _BaseMix ("Body Tint Mix", Range(0, 1)) = 0.6
        _RimLightColor ("Team Rim", Color) = (1, 1, 1, 1)
        _RimLight_Power ("Team Rim Mix", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }

        Pass
        {
            Name "BodySilhouette"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Greater
            Cull Back
            Stencil
            {
                Ref 1
                Comp NotEqual
                Pass Replace
            }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _EdgeColor;
                float _EdgePower;
                half4 _BaseColor;
                float _BaseMix;
                half4 _RimLightColor;
                float _RimLight_Power;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewWS : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewWS = GetWorldSpaceViewDir(positionWS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 n = normalize(input.normalWS);
                float3 v = normalize(input.viewWS);
                float edge = pow(1.0 - saturate(dot(n, v)), _EdgePower);

                half3 body = lerp(_Color.rgb, _Color.rgb * _BaseColor.rgb, _BaseMix);
                body = lerp(body, _RimLightColor.rgb, _RimLight_Power);
                half3 rgb = lerp(body, _EdgeColor.rgb, edge);
                half a = lerp(_Color.a, _EdgeColor.a, edge);
                return half4(rgb, a);
            }
            ENDHLSL
        }
    }
}
