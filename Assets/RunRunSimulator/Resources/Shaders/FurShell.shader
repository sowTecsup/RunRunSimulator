Shader "MoriMonchi/FurShell"
{
    Properties
    {
        _BaseColor      ("Base Color",      Color)          = (0.55, 0.35, 0.15, 1)
        _TipColor       ("Tip Color",       Color)          = (1.0,  0.90, 0.65, 1)
        _FurLength      ("Fur Length",      Float)          = 0.12
        _ShellIndex     ("Shell Index",     Float)          = 0
        _ShellCount     ("Shell Count",     Float)          = 24
        _FurDensity     ("Fur Density",     Float)          = 90
        _StrandThick    ("Strand Thickness",Range(0.05,0.5))= 0.28
        _WindStrength   ("Wind Strength",   Float)          = 0.04
        _WindSpeed      ("Wind Speed",      Float)          = 1.2
        _GravityStrength("Gravity",         Float)          = 0.18
        _RimColor       ("Rim Color",       Color)          = (0.8, 0.6, 1.0, 1)
        _RimPower       ("Rim Power",       Float)          = 4.0
        _Specular       ("Specular",        Range(0,1))     = 0.25
        _Smoothness     ("Smoothness",      Range(0,1))     = 0.3
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "TransparentCutout"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "AlphaTest+10"
        }

        // ────────────────────────────── Forward Lit ──────────────────────────────
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _TipColor;
                float4 _RimColor;
                float  _FurLength;
                float  _ShellIndex;
                float  _ShellCount;
                float  _FurDensity;
                float  _StrandThick;
                float  _WindStrength;
                float  _WindSpeed;
                float  _GravityStrength;
                float  _RimPower;
                float  _Specular;
                float  _Smoothness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float  shellT     : TEXCOORD3;
                float  fogFactor  : TEXCOORD4;
            };

            // ── Helpers ──────────────────────────────────────────────────────────

            float hash21(float2 p)
            {
                p = frac(p * float2(234.34, 435.345));
                p += dot(p, p + 34.23);
                return frac(p.x * p.y);
            }

            // ── Vertex ───────────────────────────────────────────────────────────

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float t = _ShellIndex / max(_ShellCount - 1.0, 1.0);

                float3 normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);

                // Gravity: tips droop quadratically
                float3 gravity = float3(0, -1, 0) * _GravityStrength * t * t;

                // Wind: two sine waves offset in phase + position for organics feel
                float windBase  = dot(positionWS.xz, float2(1.3, 0.7)) + _Time.y * _WindSpeed;
                float3 wind     = float3(
                    sin(windBase)       * 0.6 + sin(windBase * 2.1 + 1.2) * 0.4,
                    0,
                    cos(windBase * 0.8) * 0.35
                ) * _WindStrength * t * t;

                positionWS += normalWS * _FurLength * t + gravity + wind;

                OUT.positionCS = TransformWorldToHClip(positionWS);
                OUT.uv         = IN.uv;
                OUT.normalWS   = normalWS;
                OUT.positionWS = positionWS;
                OUT.shellT     = t;
                OUT.fogFactor  = ComputeFogFactor(OUT.positionCS.z);

                return OUT;
            }

            // ── Fragment ─────────────────────────────────────────────────────────

            half4 frag(Varyings IN) : SV_Target
            {
                float t = IN.shellT;

                // ── Strand shape via procedural noise ────────────────────────────
                float2 furCell    = IN.uv * _FurDensity;
                float2 cellID     = floor(furCell);
                float2 cellUV     = frac(furCell);

                float  strandRand = hash21(cellID);
                float2 strandCtr  = float2(hash21(cellID + 0.1), hash21(cellID + 0.2));

                float dist        = length(cellUV - strandCtr);
                float radius      = _StrandThick * (1.0 - t * 0.85);   // taper to tip

                // Root shell always solid; upper shells: clip non-strand pixels
                if (t > 0.01 && dist > radius) discard;

                // ── Color: gradient root→tip with per-strand micro-hue shift ─────
                float  hueNoise = (strandRand - 0.5) * 0.12;
                float3 furColor = lerp(_BaseColor.rgb, _TipColor.rgb + hueNoise, t);

                // ── Ambient occlusion: darkest at root ───────────────────────────
                furColor *= lerp(0.15, 1.0, t);

                // ── URP Lighting ─────────────────────────────────────────────────
                float3 normalWS  = normalize(IN.normalWS);
                float3 viewDir   = normalize(GetWorldSpaceViewDir(IN.positionWS));

                InputData inputData = (InputData)0;
                inputData.positionWS            = IN.positionWS;
                inputData.normalWS              = normalWS;
                inputData.viewDirectionWS       = viewDir;
                inputData.shadowCoord           = TransformWorldToShadowCoord(IN.positionWS);
                inputData.fogCoord              = IN.fogFactor;
                inputData.vertexLighting        = half3(0, 0, 0);
                inputData.bakedGI               = SampleSH(normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);
                inputData.shadowMask            = half4(1, 1, 1, 1);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo      = furColor;
                surfaceData.alpha       = 1.0;
                surfaceData.specular    = _Specular * t;          // tips are shinier
                surfaceData.smoothness  = _Smoothness + t * 0.2;
                surfaceData.occlusion   = 1.0;

                half4 color = UniversalFragmentBlinnPhong(inputData, surfaceData);

                // ── Rim glow: tips shimmer with a custom color ────────────────────
                float rim      = 1.0 - saturate(dot(normalWS, viewDir));
                rim            = pow(rim, _RimPower) * t * t;
                color.rgb     += _RimColor.rgb * rim * 0.55;

                // ── Sparkle: random strand tips catch light ───────────────────────
                float sparkle  = step(0.97, strandRand) * step(0.85, t);
                color.rgb     += sparkle * _RimColor.rgb * 0.8;

                color.rgb = MixFog(color.rgb, IN.fogFactor);
                return color;
            }
            ENDHLSL
        }

        // ────────────────────────────── Shadow Caster ─────────────────────────────
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex   vertShadow
            #pragma fragment fragShadow

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _TipColor;
                float4 _RimColor;
                float  _FurLength;
                float  _ShellIndex;
                float  _ShellCount;
                float  _FurDensity;
                float  _StrandThick;
                float  _WindStrength;
                float  _WindSpeed;
                float  _GravityStrength;
                float  _RimPower;
                float  _Specular;
                float  _Smoothness;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float shellT : TEXCOORD1; };

            float hash21(float2 p) { p = frac(p * float2(234.34,435.345)); p += dot(p,p+34.23); return frac(p.x*p.y); }

            Varyings vertShadow(Attributes IN)
            {
                Varyings OUT;
                float t         = _ShellIndex / max(_ShellCount - 1.0, 1.0);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
                float3 posWS    = TransformObjectToWorld(IN.positionOS.xyz);
                posWS          += normalWS * _FurLength * t;
                posWS          += float3(0,-1,0) * _GravityStrength * t * t;
                OUT.positionCS  = TransformWorldToHClip(ApplyShadowBias(posWS, normalWS, _MainLightPosition.xyz));
                OUT.uv          = IN.uv;
                OUT.shellT      = t;
                return OUT;
            }

            half4 fragShadow(Varyings IN) : SV_Target
            {
                float t = IN.shellT;
                if (t > 0.01)
                {
                    float2 cellID   = floor(IN.uv * _FurDensity);
                    float2 cellUV   = frac(IN.uv * _FurDensity);
                    float2 ctr      = float2(hash21(cellID+0.1), hash21(cellID+0.2));
                    float  r        = _StrandThick * (1.0 - t * 0.85);
                    if (length(cellUV - ctr) > r) discard;
                }
                return 0;
            }
            ENDHLSL
        }
    }
}
