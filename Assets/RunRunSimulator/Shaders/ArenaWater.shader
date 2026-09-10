Shader "MoriMonchi/ArenaWater"
{
    Properties
    {
        _Ramp ("Palette Ramp", 2D) = "white" {}
        _RampInfluence ("Ramp Influence", Range(0,1)) = 0
        _DeepColor ("Deep Color", Color) = (0.08, 0.22, 0.42, 1)
        _ShallowColor ("Shallow Color", Color) = (0.2, 0.5, 0.75, 1)
        _FoamColor ("Foam Color", Color) = (0.85, 0.95, 1, 1)
        _Opacity ("Opacity", Range(0,1)) = 0.92
        _DepthFade ("Depth Fade (m)", Float) = 1.6
        _FoamDistance ("Foam Distance (m)", Float) = 0.45
        _FoamNoise ("Foam Noise", Range(0,1)) = 0.5
        _WaveScale ("Wave Scale", Float) = 0.18
        _WaveSpeed ("Wave Speed", Float) = 0.35
        _WaveStrength ("Wave Strength", Range(0,1)) = 0.35
        _Sparkle ("Sparkle", Range(0,2)) = 0.6
        _SparklePower ("Sparkle Power", Float) = 48
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half _RampInfluence;
                half4 _DeepColor;
                half4 _ShallowColor;
                half4 _FoamColor;
                half _Opacity;
                half _DepthFade;
                half _FoamDistance;
                half _FoamNoise;
                half _WaveScale;
                half _WaveSpeed;
                half _WaveStrength;
                half _Sparkle;
                half _SparklePower;
            CBUFFER_END

            TEXTURE2D(_Ramp);
            SAMPLER(sampler_Ramp);

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
                float fogFactor : TEXCOORD2;
            };

            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = Hash(i);
                float b = Hash(i + float2(1, 0));
                float c = Hash(i + float2(0, 1));
                float d = Hash(i + float2(1, 1));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float Waves(float2 xz, float t)
            {
                float n1 = ValueNoise(xz * _WaveScale + float2(t * 0.7, t * 0.4));
                float n2 = ValueNoise(xz * _WaveScale * 2.3 - float2(t * 0.5, -t * 0.6));
                return n1 * 0.65 + n2 * 0.35;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                output.screenPos = ComputeScreenPos(positions.positionCS);
                output.fogFactor = ComputeFogFactor(positions.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float sceneEye = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                float surfaceEye = input.screenPos.w;
                float depthDiff = max(sceneEye - surfaceEye, 0.0);

                float t = _Time.y * _WaveSpeed;
                float wave = Waves(input.positionWS.xz, t);
                float ripple = (wave - 0.5) * _WaveStrength;

                float deep = saturate(depthDiff / max(_DepthFade, 0.01) + ripple * 0.5);
                float rampX = lerp(0.5, 0.0, deep) + ripple * 0.3;
                rampX = saturate(rampX);

                half3 plain = lerp(_ShallowColor.rgb, _DeepColor.rgb, deep);
                half3 fromRamp = SAMPLE_TEXTURE2D(_Ramp, sampler_Ramp, float2(rampX, 0.5)).rgb;
                half3 water = lerp(plain, fromRamp, _RampInfluence);

                half3 foamPlain = _FoamColor.rgb;
                half3 foamRamp = SAMPLE_TEXTURE2D(_Ramp, sampler_Ramp, float2(1.0, 0.5)).rgb;
                half3 foamColor = lerp(foamPlain, foamRamp, _RampInfluence);

                float shore = 1.0 - saturate(depthDiff / max(_FoamDistance, 0.01));
                float foamNoise = ValueNoise(input.positionWS.xz * 1.7 + float2(t * 1.3, -t * 0.9));
                float foam = smoothstep(0.35, 0.75, shore + (foamNoise - 0.5) * _FoamNoise);
                float foamRing = smoothstep(0.02, 0.0, abs(depthDiff - _FoamDistance * 1.9)) * (0.4 + 0.6 * foamNoise);
                foam = saturate(foam + foamRing * 0.6);

                Light mainLight = GetMainLight();
                float3 viewDir = normalize(GetWorldSpaceViewDir(input.positionWS));
                float eps = 0.35;
                float hx = Waves(input.positionWS.xz + float2(eps, 0), t) - wave;
                float hz = Waves(input.positionWS.xz + float2(0, eps), t) - wave;
                float3 normal = normalize(float3(-hx * _WaveStrength * 6.0, 1.0, -hz * _WaveStrength * 6.0));
                float3 halfDir = normalize(mainLight.direction + viewDir);
                float sparkle = pow(saturate(dot(normal, halfDir)), _SparklePower) * _Sparkle;

                half3 color = lerp(water, foamColor, foam) * (0.85 + 0.15 * mainLight.color.rgb) + sparkle * mainLight.color.rgb;
                half alpha = max(_Opacity, foam);

                color = MixFog(color, input.fogFactor);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
