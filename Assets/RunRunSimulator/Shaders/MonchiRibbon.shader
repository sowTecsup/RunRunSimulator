Shader "MoriMonchi/MonchiRibbon"
{
    Properties
    {
        _Color ("Tint", Color) = (1,1,1,1)
        _DashLength ("Dash Length", Float) = 0
        _DashGap ("Dash Gap", Float) = 0.2
        _DashOffset ("Dash Offset", Float) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 10
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }

        Pass
        {
            Name "Universal Forward"
            Tags { "LightMode"="UniversalForward" }

            Blend [_SrcBlend] [_DstBlend]
            ZWrite Off
            ZTest Always
            Cull Off
            Stencil
            {
                Ref 1
                Comp NotEqual
            }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _DashLength;
                float _DashGap;
                float _DashOffset;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float2 uv2 : TEXCOORD2;
                float2 uv3 : TEXCOORD3;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float2 uv2 : TEXCOORD2;
                float2 uv3 : TEXCOORD3;
                half4 color : COLOR;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.uv0 = input.uv0;
                output.uv1 = input.uv1;
                output.uv2 = input.uv2;
                output.uv3 = input.uv3;
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float s = input.uv0.x;
                float halfWidth = max(input.uv1.x, 0.0001);
                float meshHalfWidth = max(input.uv1.y, 0.0001);
                float y = input.uv0.y * meshHalfWidth;
                float tailRadius = max(input.uv2.x, 0.0001);
                float tipRadius = max(input.uv2.y, 0.0001);
                float totalLength = input.uv3.x;
                float headBase = input.uv3.y;

                float d = abs(y) - halfWidth;

                float dTail = length(float2(s - tailRadius, y)) - tailRadius;
                d = s < tailRadius ? max(d, dTail) : d;

                float tipCenter = totalLength - tipRadius;
                float dTip = length(float2(s - tipCenter, y)) - tipRadius;
                d = s > tipCenter ? max(d, dTip) : d;

                if (_DashLength > 0.0001 && s < headBase)
                {
                    float period = _DashLength + max(_DashGap, 0.0001);
                    float u = frac((headBase - s - _DashOffset) / period) * period;
                    float halfLen = _DashLength * 0.5;
                    float du = u - halfLen;
                    float cap = max(halfLen - halfWidth, 0.0);
                    float dDash = length(float2(max(abs(du) - cap, 0.0), y)) - halfWidth;
                    d = max(d, dDash);
                }

                float aa = max(fwidth(d), 0.00001);
                float coverage = saturate(0.5 - d / aa);

                half4 color = input.color * _Color;
                color.a *= coverage;
                return color;
            }
            ENDHLSL
        }
    }
}
