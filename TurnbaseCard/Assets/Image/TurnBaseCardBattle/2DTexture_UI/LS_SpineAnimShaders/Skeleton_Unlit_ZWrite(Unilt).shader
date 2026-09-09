Shader "LostStar/Ink_Shader/Spine_Skeleton_Unlit_ZWrite(Unilt)"
{
    Properties {
        _Cutoff ("Depth alpha cutoff", Range(0,1)) = 0.1
        [NoScaleOffset] _MainTex ("Main Texture", 2D) = "white" {}
        [Toggle(_STRAIGHT_ALPHA_INPUT)] _StraightAlphaInput("Straight Alpha Texture", Int) = 0
        [HideInInspector] _StencilRef("Stencil Reference", Float) = 1.0
        [HideInInspector][Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp("Stencil Comparison", Float) = 8 // Default Always
    }

    SubShader {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        LOD 100

        Stencil {
            Ref[_StencilRef]
            Comp[_StencilComp]
            Pass Keep
        }

        Pass {
            Name "Unlit"

            Tags { "Queue"="Transparent" "IgnoreProjector"="true" "RenderType"="Transparent" }

            ZWrite On
            Cull Off
            Blend One OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature _ _STRAIGHT_ALPHA_INPUT
            #pragma target 2.0

            sampler2D _MainTex;
            float _Cutoff;

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                fixed4 col = tex2D(_MainTex, i.uv);
                #ifdef _STRAIGHT_ALPHA_INPUT
                    col.rgb *= col.a; // Premultiplied Alpha handling
                #endif
                clip(col.a - _Cutoff); // Alpha cutoff
                return col;
            }
            ENDCG
        }

        Pass {
            Name "DepthWrite"

            Tags { "LightMode"="ShadowCaster" }
            Offset 1, 1

            ZWrite On
            ZTest LEqual
            Cull Off

            CGPROGRAM
            #pragma vertex vertShadow
            #pragma fragment fragShadow
            #pragma multi_compile_shadowcaster
            #pragma fragmentoption ARB_precision_hint_fastest

            #include "Assets/Spine/Runtime/spine-unity/Shaders/CGIncludes/Spine-Skeleton-Lit-Common-Shadow.cginc"

            struct v2fShadow {
                V2F_SHADOW_CASTER;
            };

            v2fShadow vertShadow(appdata_base v) {
                v2fShadow o;
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                return o;
            }

            fixed4 fragShadow(v2fShadow i) : SV_Target {
                return 0;
            }
            ENDCG
        }
    }
}
