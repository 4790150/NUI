Shader "UI/TText"
{
	Properties
	{
		[PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
		_Color("Tint", Color) = (1,1,1,1)

		_StencilComp("Stencil Comparison", Float) = 8
		_Stencil("Stencil ID", Float) = 0
		_StencilOp("Stencil Operation", Float) = 0
		_StencilWriteMask("Stencil Write Mask", Float) = 255
		_StencilReadMask("Stencil Read Mask", Float) = 255

		_ColorMask("Color Mask", Float) = 15

		[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip("Use Alpha Clip", Float) = 0

		_OutlineSize("OutlineSize", Float) = 1
		_OutlineColor("OutlineColor", Color) = (0, 0, 0, 1)

		//_ShadowColor("Shadow Color", Color) = (0, 0, 0, 1)
		//_ShadowDistance("Shadow Distance", Color) = (0, 0, 0, 0)
	}

		SubShader
		{
			Tags
			{
				"Queue" = "Transparent"
				"IgnoreProjector" = "True"
				"RenderType" = "Transparent"
				"PreviewType" = "Plane"
				"CanUseSpriteAtlas" = "True"
			}

			Stencil
			{
				Ref[_Stencil]
				Comp[_StencilComp]
				Pass[_StencilOp]
				ReadMask[_StencilReadMask]
				WriteMask[_StencilWriteMask]
			}

			Cull Off
			Lighting Off
			ZWrite Off
			ZTest[unity_GUIZTestMode]
			Blend SrcAlpha OneMinusSrcAlpha
			ColorMask[_ColorMask]

			Pass
			{
				Name "Outline"
			CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma target 2.0

				#include "UnityCG.cginc"
				#include "UnityUI.cginc"

				#pragma multi_compile __ UNITY_UI_ALPHACLIP
				//#pragma multi_compile OUTLINE SHADOW

				struct appdata_t
				{
					float4 vertex   : POSITION;
					float4 color    : COLOR;
					float2 texcoord : TEXCOORD0;
					float2 uvBoound : TEXCOORD1;
					UNITY_VERTEX_INPUT_INSTANCE_ID
				};

				struct v2f
				{
					float4 vertex   : SV_POSITION;
					fixed4 color : COLOR;
					float2 texcoord  : TEXCOORD0;
					float2 uvMin : TEXCOORD1;
					float2 uvMax : TEXCOORD2;
					float4 worldPosition : TEXCOORD3;
					UNITY_VERTEX_OUTPUT_STEREO
				};

				fixed4 _Color;
				fixed4 _TextureSampleAdd;
				float4 _ClipRect;

				v2f vert(appdata_t IN)
				{
					v2f OUT;
					UNITY_SETUP_INSTANCE_ID(IN);
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
					OUT.worldPosition = IN.vertex;
					OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);

					OUT.texcoord = IN.texcoord;
					OUT.uvMin = frac(IN.uvBoound.xy);
					float2 wh = (IN.uvBoound.xy - OUT.uvMin) / (2 << 12);
					OUT.uvMax = OUT.uvMin + wh;

					OUT.color = IN.color * _Color;
					return OUT;
				}

				sampler2D _MainTex;

				float4 _MainTex_TexelSize;
				float _OutlineSize;
				float4 _OutlineColor;
				float4 _ShadowColor;
				float2 _ShadowDistance;

				float IsInRect(float2 fpoint, v2f IN)
				{
					//return 1;
					float2 inside = step(IN.uvMin.xy, fpoint) * step(fpoint, IN.uvMax.xy);
					return inside.x * inside.y;
				}

				fixed4 frag(v2f IN) : SV_Target
				{
					float2 texcoord;
					float isInRect;
					float hitCount = 0;
					half4 effectColor = half4(0, 0, 0, 0);
					half4 color = half4(0, 0, 0, 0);

					texcoord = IN.texcoord + _MainTex_TexelSize.xy * float2(-1, -1) * _OutlineSize;
					isInRect = IsInRect(texcoord, IN);
					effectColor += (tex2D(_MainTex, texcoord) + _TextureSampleAdd) * isInRect;
					hitCount += isInRect;

					texcoord = IN.texcoord + _MainTex_TexelSize.xy * float2(0, -1) * _OutlineSize;
					isInRect = IsInRect(texcoord, IN);
					effectColor += (tex2D(_MainTex, texcoord) + _TextureSampleAdd) * isInRect;
					hitCount += isInRect;

					texcoord = IN.texcoord + _MainTex_TexelSize.xy * float2(1, -1) * _OutlineSize;
					isInRect = IsInRect(texcoord, IN);
					effectColor += (tex2D(_MainTex, texcoord) + _TextureSampleAdd) * isInRect;
					hitCount += isInRect;

					texcoord = IN.texcoord + _MainTex_TexelSize.xy * float2(-1, 0) * _OutlineSize;
					isInRect = IsInRect(texcoord, IN);
					effectColor += (tex2D(_MainTex, texcoord) + _TextureSampleAdd) * isInRect;
					hitCount += isInRect;

					texcoord = IN.texcoord + _MainTex_TexelSize.xy * float2(1, 0) * _OutlineSize;
					isInRect = IsInRect(texcoord, IN);
					effectColor += (tex2D(_MainTex, texcoord) + _TextureSampleAdd) * isInRect;
					hitCount += isInRect;

					texcoord = IN.texcoord + _MainTex_TexelSize.xy * float2(-1, 1) * _OutlineSize;
					isInRect = IsInRect(texcoord, IN);
					effectColor += (tex2D(_MainTex, texcoord) + _TextureSampleAdd) * isInRect;
					hitCount += isInRect;

					texcoord = IN.texcoord + _MainTex_TexelSize.xy * float2(0, 1) * _OutlineSize;
					isInRect = IsInRect(texcoord, IN);
					effectColor += (tex2D(_MainTex, texcoord) + _TextureSampleAdd) * isInRect;
					hitCount += isInRect;

					texcoord = IN.texcoord + _MainTex_TexelSize.xy * float2(1, 1) * _OutlineSize;
					isInRect = IsInRect(texcoord, IN);
					effectColor += (tex2D(_MainTex, texcoord) + _TextureSampleAdd) * isInRect;
					hitCount += isInRect;

					effectColor.rgb /= max(1, hitCount);
					effectColor.a = clamp(effectColor.a, 0, 1);
					effectColor *= _OutlineColor;
					effectColor.a *= IN.color.a;

					color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color * IsInRect(IN.texcoord, IN);
					color.rgb = color.rgb * color.a + effectColor.rgb * (1 - color.a);
					color.a = max(color.a, effectColor.a);

//
//#if defined(SHADOW)
//					texcoord = IN.texcoord + _MainTex_TexelSize.xy * _ShadowDistance * -1;
//					isInRect = IsInRect(texcoord, IN);
//					effectColor = (tex2D(_MainTex, texcoord) + _TextureSampleAdd) * isInRect;
//
//					//color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color * IsInRect(IN.texcoord, IN);
//					color.rgb = color.rgb * color.a + _ShadowColor.rgb * (1 - color.a);
//					color.a = max(color.a, effectColor.a);
//#endif


					color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);

					#ifdef UNITY_UI_ALPHACLIP
					clip(color.a - 0.001);
					#endif

					return color;
				}
			ENDCG
			}
		}
}
