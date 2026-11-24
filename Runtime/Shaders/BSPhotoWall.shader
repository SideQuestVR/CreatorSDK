// UV0 card UV, UV1 picture rect in the atlas, UV2 (kind: 0 board, 1 card, 2 string; arrival time; random).
Shader "BS/Photo Wall"
{
    Properties
    {
        [NoScaleOffset] _Atlas("Atlas", 2D) = "grey" {}
        _Window("Picture on the card (x min, y min, x max, y max)", Vector) = (0.04, 0.04, 0.96, 0.96)
        _CardAspect("Card width / height", Float) = 1.5
        _Shape("Shape (0 rect, 1 rounded, 2 circle)", Float) = 0
        _Corner("Corner radius (card heights)", Float) = 0.06
        _PaperColor("Frame", Color) = (0.96, 0.95, 0.92, 1)
        _BackingColor("Backing", Color) = (0.55, 0.42, 0.3, 1)
        _StringColor("String", Color) = (0.12, 0.1, 0.09, 1)
        _DevelopSeconds("Develop seconds", Float) = 1.5
        _SelfLight("Card Self Light", Range(0, 1)) = 0.35
        _MinLight("Min Light", Range(0, 1)) = 0
        _PhotoUnlit("Photo Unlit", Range(0, 1)) = 0.8
    }

    SubShader
    {
        Tags { "RenderType" = "TransparentCutout" "RenderPipeline" = "UniversalPipeline" "Queue" = "AlphaTest" }

        HLSLINCLUDE
        #define _ALPHATEST_ON 1
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _Window;
            float _CardAspect;
            float _Shape;
            float _Corner;
            half4 _PaperColor;
            half4 _BackingColor;
            half4 _StringColor;
            float _DevelopSeconds;
            half _SelfLight;
            half _MinLight;
            half _PhotoUnlit;
        CBUFFER_END

        float BoxSdf(float2 p, float2 halfSize, float radius)
        {
            float2 q = abs(p) - halfSize + radius;
            return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - radius;
        }

        float OutlineSdf(float2 uv, float aspect)
        {
            float2 p = (uv - 0.5) * float2(aspect, 1.0);
            if (_Shape > 1.5)
                return length(p) - 0.5 * min(aspect, 1.0);
            float radius = _Shape > 0.5 ? _Corner : 0.0;
            return BoxSdf(p, float2(aspect, 1.0) * 0.5, radius);
        }

        float WindowSdf(float2 uv, float aspect)
        {
            float2 p = (uv - 0.5) * float2(aspect, 1.0);
            float2 middle = ((_Window.xy + _Window.zw) * 0.5 - 0.5) * float2(aspect, 1.0);
            float2 halfSize = (_Window.zw - _Window.xy) * 0.5 * float2(aspect, 1.0);
            if (_Shape > 1.5)
                return length(p - middle) - min(halfSize.x, halfSize.y);
            float inset = _Window.x * aspect;
            float radius = _Shape > 0.5 ? max(_Corner - inset, 0.0) : 0.0;
            return BoxSdf(p - middle, halfSize, radius);
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On
            AlphaToMask On

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_Atlas);
            SAMPLER(sampler_Atlas);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                float4 cell       : TEXCOORD1;
                float4 info       : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3  normalWS   : TEXCOORD1;
                float2 uv         : TEXCOORD2;
                float4 cell       : TEXCOORD3;
                float4 info       : TEXCOORD4;
                half   fogFactor  : TEXCOORD5;
                half3  light      : TEXCOORD6;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.uv = input.uv;
                o.cell = input.cell;
                o.info = input.info;
                o.fogFactor = ComputeFogFactor(p.positionCS.z);

                half3 n = normalize(o.normalWS);
            #if defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2)
                half3 ambient;
                float4 probeOcclusion;
                if (_EnableProbeVolumes)
                    EvaluateAdaptiveProbeVolume(GetAbsolutePositionWS(p.positionWS), n, GetWorldSpaceNormalizeViewDir(p.positionWS),
                        float2(0, 0), GetMeshRenderingLayer(), ambient, probeOcclusion);
                else
                    ambient = EvaluateAmbientProbe(n);
            #else
                half3 ambient = SampleSH(n);
            #endif
                Light light = GetMainLight();
                o.light = ambient + light.color * saturate(dot(n, light.direction));
                return o;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half3 albedo;
                half alpha = 1;
                half self = 0;
                half3 shown = 0;   // the picture as taken, drawn mostly unlit so photos read in any light
                half unlit = 0;
                if (input.info.x < 0.5)
                {
                    albedo = _BackingColor.rgb;
                }
                else if (input.info.x > 1.5)
                {
                    albedo = _StringColor.rgb;
                }
                else
                {
                    // Derivatives before the clip.
                    float outline = OutlineSdf(input.uv, _CardAspect);
                    float window = WindowSdf(input.uv, _CardAspect);
                    half picture = saturate(0.5 - window / max(fwidth(window), 1e-5));
                    float2 inWindow = saturate((input.uv - _Window.xy) / max(_Window.zw - _Window.xy, 1e-5));
                    half3 photo = SAMPLE_TEXTURE2D(_Atlas, sampler_Atlas, input.cell.xy + inWindow * input.cell.zw).rgb;
                    alpha = AlphaClip(saturate(0.5 - outline / max(fwidth(outline), 1e-5)), 0.5);
                    half develop = saturate((_Time.y - input.info.y) / max(_DevelopSeconds, 1e-3));
                    photo *= lerp(0.12, 1.0, develop * develop * (3.0 - 2.0 * develop));
                    half3 paper = _PaperColor.rgb * (0.97 + 0.03 * input.info.z);
                    albedo = lerp(paper, photo, picture);
                    self = _SelfLight;
                    shown = photo;
                    unlit = picture * _PhotoUnlit;
                }

                half3 lit = albedo * (max(input.light, _MinLight) + self);
                half3 color = MixFog(lerp(lit, shown, unlit), input.fogFactor);
                return half4(color, alpha);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 info       : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float  card       : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings DepthVert(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.uv = input.uv;
                o.card = input.info.x;
                return o;
            }

            half DepthFrag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                if (input.card > 0.5 && input.card < 1.5)
                    clip(-OutlineSdf(input.uv, _CardAspect));
                return input.positionCS.z;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
