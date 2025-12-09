// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "RoadArchitect/TranShadow" { 

	Properties 
	{ 
		// Usual stuffs
		_Color ("Main Color", Color) = (1,1,1,1)
		//_SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 0)
		//_Shininess ("Shininess", Range (0.01, 1)) = 0.078125
		_MainTex ("Base (RGB) TransGloss (A)", 2D) = "white" {}
		
		// Shadow Stuff
		_ShadowIntensity ("Shadow Intensity", Range (0, 1)) = 0.6
	} 
	
	
	SubShader 
	{ 
		Tags {
			"Queue"="AlphaTest" 
			"IgnoreProjector"="True" 
			"RenderType"="Transparent"
		}
	
		LOD 300
	
	// ===== Main Surface Pass (Handles Spot/Point lights) =====
	CGPROGRAM
			#pragma surface surf BlinnPhong alpha vertex:vert fullforwardshadows approxview
			#pragma exclude_renderers d3d11 xbox360
			#pragma target 3.0
	
			sampler2D _MainTex;
			float4 _Color;
	
			struct Input {
				float2 uv_MainTex;
			};
	
			// ✅ 正确的 surface-vertex 修饰函数签名（返回 void，输出 Input）
			void vert (inout appdata_full v, out Input o)
			{
				UNITY_INITIALIZE_OUTPUT(Input, o);
				o.uv_MainTex = v.texcoord.xy; // 如需偏移/缩放可在此修改
			}
	
			void surf (Input IN, inout SurfaceOutput o) {
				fixed4 tex = tex2D(_MainTex, IN.uv_MainTex);
				o.Albedo = tex.rgb * _Color.rgb;
				o.Gloss  = tex.a;
				o.Alpha  = tex.a * _Color.a;
			}
	ENDCG
	
		// ===== Shadow Pass : 手动叠加方向光阴影衰减 =====
		Pass {
			Blend SrcAlpha OneMinusSrcAlpha 
			Name "ShadowPass"
			Tags {"LightMode" = "ForwardBase"}
			  
			CGPROGRAM 
			#pragma exclude_renderers d3d11 xbox360
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase
			#pragma fragmentoption ARB_fog_exp2
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma target 3.0
			#include "UnityCG.cginc"
			#include "AutoLight.cginc"
	 
			struct v2f { 
				float2 uv_MainTex : TEXCOORD1;
				float4 pos        : SV_POSITION;
				LIGHTING_COORDS(3,4)
				float3 lightDir   : TEXCOORD2;   // ✅ 补上语义；或者直接删掉此成员（下方同时删相关代码）
			};
	 
			float4 _MainTex_ST;
			sampler2D _MainTex;
			float4 _Color;
			float _ShadowIntensity;
	 
			v2f vert (appdata_full v)
			{
				v2f o;
				o.uv_MainTex = TRANSFORM_TEX(v.texcoord, _MainTex);
				o.pos        = UnityObjectToClipPos (v.vertex);
				o.lightDir   = ObjSpaceLightDir( v.vertex );
				TRANSFER_VERTEX_TO_FRAGMENT(o);
				return o;
			}
	
			// ✅ Metal 下用 SV_Target
			float4 frag (v2f i) : SV_Target
			{
				float atten = LIGHT_ATTENUATION(i);
				half4 c;
				c.rgb = 0;
				c.a   = (1-atten) * _ShadowIntensity * (tex2D(_MainTex, i.uv_MainTex).a); 
				return c;
			}
			ENDCG
		}
	}
	
	FallBack "Transparent/Specular"
	}
	