Shader "UI/URP/LungPurpleAtmosphere"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)

        [Header(Base Gradient)]
        _StartColor ("Start Color", Color) = (0.0353, 0.0157, 0.0667, 1)
        _MidColor ("Mid Color", Color) = (0.1059, 0.0588, 0.1961, 1)
        _EndColor ("End Color", Color) = (0.1922, 0.0706, 0.3333, 1)
        _MidStop ("Mid Stop", Range(0.01, 0.99)) = 0.42
        _CssAngleDeg ("CSS Angle (Deg)", Range(0, 360)) = 150

        [Header(Glow 1)]
        _Glow1Color ("Glow 1 Color", Color) = (0.5529, 0.3020, 1.0, 1)
        _Glow1Center ("Glow 1 Center UV", Vector) = (0.08, 0.90, 0, 0)
        _Glow1Radius ("Glow 1 Radius", Range(0.01, 1.5)) = 0.50
        _Glow1Softness ("Glow 1 Softness", Range(0.001, 1.0)) = 0.20
        _Glow1Intensity ("Glow 1 Intensity", Range(0, 2)) = 0.32

        [Header(Glow 2)]
        _Glow2Color ("Glow 2 Color", Color) = (0.4078, 0.1647, 0.8392, 1)
        _Glow2Center ("Glow 2 Center UV", Vector) = (0.92, 1.00, 0, 0)
        _Glow2Radius ("Glow 2 Radius", Range(0.01, 1.5)) = 0.54
        _Glow2Softness ("Glow 2 Softness", Range(0.001, 1.0)) = 0.22
        _Glow2Intensity ("Glow 2 Intensity", Range(0, 2)) = 0.28

        [Header(UI Stencil Support)]
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "CanUseSpriteAtlas" = "True"
            "PreviewType" = "Plane"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "UIAtmosphere"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 color       : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;

                float4 _StartColor;
                float4 _MidColor;
                float4 _EndColor;
                float _MidStop;
                float _CssAngleDeg;

                float4 _Glow1Color;
                float4 _Glow1Center;
                float _Glow1Radius;
                float _Glow1Softness;
                float _Glow1Intensity;

                float4 _Glow2Color;
                float4 _Glow2Center;
                float _Glow2Radius;
                float _Glow2Softness;
                float _Glow2Intensity;
            CBUFFER_END

            float3 ComputeThreeStopGradient(float2 uv)
            {
                float mathRad = radians(90.0 - _CssAngleDeg);
                float2 dir = float2(cos(mathRad), sin(mathRad));

                float t = dot(uv - 0.5, dir) + 0.5;
                t = saturate(t);

                float midSafe = max(_MidStop, 0.0001);
                float endSafe = max(1.0 - _MidStop, 0.0001);

                float firstLerp = saturate(t / midSafe);
                float secondLerp = saturate((t - _MidStop) / endSafe);

                float3 first = lerp(_StartColor.rgb, _MidColor.rgb, firstLerp);
                float3 second = lerp(_MidColor.rgb, _EndColor.rgb, secondLerp);

                float useSecond = step(_MidStop, t);
                return lerp(first, second, useSecond);
            }

            float ComputeRadialGlow(
                float2 uv,
                float2 center,
                float radius,
                float softness
            )
            {
                float d = distance(uv, center);
                float inner = max(radius - softness, 0.0);
                float mask = 1.0 - smoothstep(inner, radius, d);
                return saturate(mask);
            }

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv * _MainTex_ST.xy + _MainTex_ST.zw;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;

                float3 baseGradient = ComputeThreeStopGradient(uv);

                float glow1 = ComputeRadialGlow(
                    uv,
                    _Glow1Center.xy,
                    _Glow1Radius,
                    _Glow1Softness
                ) * _Glow1Intensity;

                float glow2 = ComputeRadialGlow(
                    uv,
                    _Glow2Center.xy,
                    _Glow2Radius,
                    _Glow2Softness
                ) * _Glow2Intensity;

                float3 finalRgb = baseGradient
                    + (_Glow1Color.rgb * glow1)
                    + (_Glow2Color.rgb * glow2);

                finalRgb = saturate(finalRgb);

                float spriteAlpha = SAMPLE_TEXTURE2D(
                    _MainTex,
                    sampler_MainTex,
                    uv
                ).a;

                float alpha = spriteAlpha * IN.color.a;
                return half4(finalRgb, alpha);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
