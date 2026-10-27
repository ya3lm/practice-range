// Measured grid for floor and walls. URP unlit, fog-aware, planar projection on the dominant normal axis.
Shader "PracticeRange/Grid"
{
    Properties
    {
        _BaseColor ("Base", Color) = (0.08, 0.09, 0.11, 1)
        [HDR] _LineColor ("Line (HDR)", Color) = (0, 1, 1, 1)
        _MinorSpacing ("Minor spacing (m)", Float) = 1
        _MajorSpacing ("Major spacing (m)", Float) = 5
        _LineWidth ("Line width (m)", Float) = 0.02
        _MinorStrength ("Minor strength", Range(0, 1)) = 0.25
        _FadeStart ("Fade start (m)", Float) = 25
        _FadeEnd ("Fade end (m)", Float) = 100
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _LineColor;
                float _MinorSpacing;
                float _MajorSpacing;
                float _LineWidth;
                float _MinorStrength;
                float _FadeStart;
                float _FadeEnd;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float  fogFactor  : TEXCOORD2;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.fogFactor = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            float GridLine(float2 uv, float spacing, float width)
            {
                float2 g = abs(frac(uv / spacing - 0.5) - 0.5) * spacing;
                float2 fw = fwidth(uv);
                float2 l = 1.0 - smoothstep(width - fw, width + fw, g);
                return max(l.x, l.y);
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 n = abs(normalize(i.normalWS));
                float2 uv = n.y > 0.5 ? i.positionWS.xz : (n.x > 0.5 ? i.positionWS.zy : i.positionWS.xy);
                float minor = GridLine(uv, _MinorSpacing, _LineWidth) * _MinorStrength;
                float major = GridLine(uv, _MajorSpacing, _LineWidth * 1.6);
                float lines = saturate(max(minor, major));
                float d = distance(i.positionWS, _WorldSpaceCameraPos);
                float fade = 1.0 - smoothstep(_FadeStart, _FadeEnd, d);
                float3 col = lerp(_BaseColor.rgb, _LineColor.rgb, lines * fade);
                col = MixFog(col, i.fogFactor);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Unlit"
}
