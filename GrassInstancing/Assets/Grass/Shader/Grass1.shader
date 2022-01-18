Shader "Custom/Grass1_new"
{
    Properties
    {

        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}
        [MainColor] _BaseColor("Color", Color) = (1,1,1,1)

   _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

    }

    SubShader
    {
        // Universal Pipeline tag is required. If Universal render pipeline is not set in the graphics settings
        // this Subshader will fail. One can add a subshader below or fallback to Standard built-in to make this
        // material work with both Universal Render Pipeline and Builtin Unity Pipeline
        Tags{"RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True"}
        LOD 300

        // ------------------------------------------------------------------
        //  Forward pass. Shades all light in a single pass. GI + emission + Fog
        Pass
        {
            // Lightmode matches the ShaderPassName set in UniversalRenderPipeline.cs. SRPDefaultUnlit and passes with
            // no LightMode tag are also rendered by Universal Render Pipeline
            Name "ForwardLit"
            Tags{"LightMode" = "UniversalForward"}

            ZWrite On
            ZTest On
            Cull Off

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard SRP library
            // All shaders must be compiled with HLSLcc and currently only gles is not using HLSLcc by default
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0

            // #include "UnityCG.cginc"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature _RECEIVE_SHADOWS_OFF

            // -------------------------------------
            // Universal Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:setup


            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                float3 normalOS     : NORMAL;
                uint instanceID : SV_InstanceID;
            };

            struct Varyings
            {
                float2 uv           : TEXCOORD0;
                float4 positionCS   : SV_POSITION;
                float3 normalWS    : TEXCOORD1;
                float4 positionWS   : TEXCOORD2;
            };

            #pragma vertex PassVertex
            #pragma fragment PassFragment

            float _Cutoff;
            half4 _BaseColor;

            TEXTURE2D_X(_BaseMap);
            SAMPLER(sampler_BaseMap);
            float4x4 _LocalToWorld;

            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                struct GrassInfo{
                    float4x4 transform;
                };
                StructuredBuffer< GrassInfo> _GrassInfos;
            #endif

            void setup(){
            }

            Varyings PassVertex(Attributes input)
            {
                Varyings output;
                float2 uv = input.uv;
                float3 positionWS = input.positionOS.xyz;
                float3 normalWS = input.normalOS;
                uint instanceID = input.instanceID;
                #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                    GrassInfo grassInfo = _GrassInfos[instanceID];
                    positionWS = mul(grassInfo.transform, input.positionOS).xyz;
                    normalWS = mul((float3x3)grassInfo.transform, input.normalOS);       
                #endif
                output.uv = uv;
                output.positionWS = float4(positionWS,1);
                output.positionCS = mul(UNITY_MATRIX_VP, float4(positionWS,1));
                output.normalWS = normalWS;
                return output;
            }

            half4 PassFragment(Varyings input) : SV_Target
            {
                half4 diffuseColor = SAMPLE_TEXTURE2D_X(_BaseMap,sampler_BaseMap,input.uv);
                if(diffuseColor.a < _Cutoff){
                    discard;
                    return 0 ;
                }

                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                float3 lightDir = mainLight.direction;
                float3 lightColor = mainLight.color;
                float3 normalWS = input.normalWS;
                float4 color = float4(1,1,1,1);
                float minDotLN = 0.2;
                color.rgb = max(minDotLN,abs(dot(lightDir,normalWS))) * lightColor * diffuseColor.rgb * _BaseColor.rgb * mainLight.shadowAttenuation;
                return color;
            }

            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
