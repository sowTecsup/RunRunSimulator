Shader "MiniShapes/Shape"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _ColorB ("Color B", Color) = (1,1,1,1)
        _Shape ("Shape", Float) = 0
        _Center ("Center", Vector) = (0,0,0,0)
        _Radius ("Radius", Float) = 1
        _Thickness ("Thickness", Float) = 0.1
        _PointA ("Point A", Vector) = (0,0,0,0)
        _PointB ("Point B", Vector) = (0,0,0,0)
        _HeadLength ("Head Length", Float) = 0.5
        _HeadWidth ("Head Width", Float) = 0.4
        _DashCount ("Dash Count", Float) = 24
        _DashRatio ("Dash Ratio", Range(0,1)) = 0.55
        _Rotation ("Rotation", Float) = 0
        _ArcStart ("Arc Start", Float) = 0
        _ArcSweep ("Arc Sweep", Float) = 1.57
        _DashLength ("Dash Length", Float) = 0.3
        _DashGap ("Dash Gap", Float) = 0.2
        _DashOffset ("Dash Offset", Float) = 0
        _InnerAlpha ("Inner Alpha", Range(0,1)) = 1
        _OuterAlpha ("Outer Alpha", Range(0,1)) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 10
        [IntRange] _StencilRef ("Stencil Ref", Range(0,255)) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp ("Stencil Comp", Float) = 8
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest", Float) = 8
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
            ZTest [_ZTest]
            Cull Off
            Stencil
            {
                Ref [_StencilRef]
                Comp [_StencilComp]
            }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _ColorB;
                float _Shape;
                float4 _Center;
                float _Radius;
                float _Thickness;
                float4 _PointA;
                float4 _PointB;
                float _HeadLength;
                float _HeadWidth;
                float _DashCount;
                float _DashRatio;
                float _Rotation;
                float _ArcStart;
                float _ArcSweep;
                float _DashLength;
                float _DashGap;
                float _DashOffset;
                float _InnerAlpha;
                float _OuterAlpha;
            CBUFFER_END

            #include "Packages/com.sowtank.minishapes/Shaders/MiniShapeSDF.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = input.positionWS.xz;
                float d = 0;
                float t = 0;
                float radialT = 0;

                if (_Shape < 0.5) ShapeRing(p, d, t, radialT);
                else if (_Shape < 1.5) ShapeDisc(p, d, t, radialT);
                else if (_Shape < 2.5) ShapeSegment(p, d, t, radialT);
                else if (_Shape < 3.5) ShapeArrow(p, d, t, radialT);
                else if (_Shape < 4.5) ShapeDashedRing(p, d, t, radialT);
                else if (_Shape < 5.5) ShapeArc(p, d, t, radialT);
                else if (_Shape < 6.5) ShapeDashedSegment(p, d, t, radialT);
                else if (_Shape < 7.5) ShapeSector(p, d, t, radialT);
                else if (_Shape < 8.5) ShapeDashedArc(p, d, t, radialT);
                else if (_Shape < 9.5) ShapeCapsuleOutline(p, d, t, radialT);

                float aa = fwidth(d);
                float coverage = 1 - smoothstep(-aa, aa, d);
                clip(coverage - 0.001);

                half4 color = lerp(_Color, _ColorB, t);
                float alphaMul = lerp(_InnerAlpha, _OuterAlpha, radialT);
                half alpha = color.a * coverage * alphaMul;

                return half4(color.rgb * alpha, alpha);
            }
            ENDHLSL
        }
    }
}
