Shader "MoriMonchi/ArenaGrass"
{
    Properties
    {
        _Ramp ("Palette Ramp", 2D) = "white" {}
        _RampInfluence ("Ramp Influence", Range(0,1)) = 1
        _BaseColor ("Base Color", Color) = (0.32,0.55,0.24,1)
        _Tint ("Tint", Color) = (1,1,1,1)
        _RootShade ("Root Shade", Range(0,1)) = 0.45
        _TipLift ("Tip Lift", Range(0,1)) = 0.95
        _WindStrength ("Wind Strength", Float) = 0.12
        _WindSpeed ("Wind Speed", Float) = 1.2
        _WindScale ("Wind Scale", Float) = 0.35
        _TrampleBend ("Trample Bend", Float) = 0.9
        _TrampleFlatten ("Trample Flatten", Range(0,1)) = 0.75
        _TrampleShade ("Trample Shade", Range(0,1)) = 0.25
        _ShadowStrength ("Shadow Strength", Range(0,1)) = 0.75
        _ToonStep ("Toon Step", Range(0,1)) = 0.45
        _ToonFeather ("Toon Feather", Range(0.001,0.5)) = 0.2
        _ShadeTint ("Shade Tint", Color) = (0.58,0.56,0.68,1)
        _ShadeLight ("Shade Light", Range(0,1)) = 0.45
        _LitLight ("Lit Light", Range(0,2)) = 0.72
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        Cull Off

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _Tint;
                half _RampInfluence;
                half _RootShade;
                half _TipLift;
                half _WindStrength;
                half _WindSpeed;
                half _WindScale;
                half _TrampleBend;
                half _TrampleFlatten;
                half _TrampleShade;
                half _ShadowStrength;
                half _ToonStep;
                half _ToonFeather;
                half4 _ShadeTint;
                half _ShadeLight;
                half _LitLight;
            CBUFFER_END

            float4 _ArenaFogCenter;
            float4 _ArenaFogColor;
            float _ArenaFogInner;
            float _ArenaFogOuter;
            float _ArenaFogStrength;
            float _ArenaFogDim;

            float4 _ArenaTrampleArea;

            TEXTURE2D(_Ramp);
            SAMPLER(sampler_Ramp);
            TEXTURE2D(_ArenaTrampleTex);
            SAMPLER(sampler_ArenaTrampleTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float fogFactor : TEXCOORD3;
                half2 blade : TEXCOORD4;
            };

            float4 SampleTrample(float3 positionWS)
            {
                float2 uv = (positionWS.xz - _ArenaTrampleArea.xy) * _ArenaTrampleArea.zw;
                if (any(uv < 0.0) || any(uv > 1.0)) return 0.0;
                return SAMPLE_TEXTURE2D_LOD(_ArenaTrampleTex, sampler_ArenaTrampleTex, uv, 0);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float height = max(input.positionOS.y, 0.0);
                float along = saturate(input.uv.y);
                float curve = along * along;

                float4 trample = SampleTrample(positionWS);
                float flatten = saturate(trample.b);

                float phase = _Time.y * _WindSpeed + positionWS.x * _WindScale + positionWS.z * _WindScale * 0.7 + input.color.r * 6.2831;
                float sway = sin(phase) * 0.6 + sin(phase * 2.3 + 1.3) * 0.4;
                float breeze = 1.0 - flatten * 0.8;

                positionWS.x += sway * _WindStrength * curve * breeze;
                positionWS.z += cos(phase * 0.8) * _WindStrength * 0.5 * curve * breeze;

                positionWS.xz += clamp(trample.rg, -1.0, 1.0) * _TrampleBend * curve;
                positionWS.y -= flatten * _TrampleFlatten * height;

                output.positionWS = positionWS;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                output.blade = half2(input.color.r, flatten);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half along = saturate(input.uv.y);
                half variation = lerp(0.95, 1.05, input.blade.x);
                half key = saturate(lerp(_RootShade, _TipLift, along) * variation);

                half3 ramp = SAMPLE_TEXTURE2D(_Ramp, sampler_Ramp, float2(key, 0.5)).rgb;
                half3 albedo = lerp(_BaseColor.rgb * key, ramp, _RampInfluence) * _Tint.rgb;
                albedo *= lerp(1.0, 1.0 - _TrampleShade, input.blade.y);

                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                half3 normalWS = normalize(input.normalWS);
                half attenuation = lerp(1.0, mainLight.shadowAttenuation * mainLight.distanceAttenuation, _ShadowStrength);
                half wrapped = dot(normalWS, mainLight.direction) * 0.5 + 0.5;
                half lit = smoothstep(_ToonStep - _ToonFeather, _ToonStep + _ToonFeather, wrapped * attenuation);
                half3 ambient = SampleSH(normalWS);

                half3 color = lerp(albedo * _ShadeTint.rgb * (mainLight.color * _ShadeLight + ambient), albedo * (mainLight.color * _LitLight + ambient), lit);

                float dFog = distance(input.positionWS.xz, _ArenaFogCenter.xz);
                float tFog = 0.0;
                if (_ArenaFogOuter > _ArenaFogInner)
                {
                    tFog = saturate((dFog - _ArenaFogInner) / (_ArenaFogOuter - _ArenaFogInner));
                    tFog = smoothstep(0.0, 1.0, tFog);
                }
                color *= lerp(1.0, 1.0 - _ArenaFogDim, tFog);
                color = lerp(color, _ArenaFogColor.rgb, tFog * _ArenaFogStrength);

                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
