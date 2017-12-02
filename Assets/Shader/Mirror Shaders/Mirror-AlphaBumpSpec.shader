
Shader "Mirrors/Transparent Bumped Specular Flat" {
Properties {
	_Transparency("Transparency", Range (0, 1)) = 1
	_Distortion ("Distortion", range (0,1)) = 0
	_Color ("Main Color", Color) = (1,1,1,1)
	_MainTex ("Base (RGB) TransGloss (A)", 2D) = "white" {}
	_BlendLevel("Main Material Blend Level",Range(0,1))=1
	_SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 0)
	_Shininess ("Shininess", Range (0.01, 1)) = 0.078125
	_BumpMap ("Normalmap", 2D) = "bump" {}
	_Bumpness ("Bump Rate",Range(0,1))= 1
	_Ref ("For Mirror reflection,don't set it!", 2D) = "white" {}
}

SubShader {
	Tags { "Queue"="Transparent" "RenderType"="Opaque" }

	GrabPass {							
			Name "BASE"
			Tags { "LightMode" = "Always" }
 		}

CGPROGRAM
#pragma surface surf BlinnPhong
#pragma target 3.0
#pragma debug

sampler2D _MainTex;
sampler2D _BumpMap;
sampler2D _Ref;
fixed4 _Color;
half _BlendLevel;
half _Transparency;
half _Bumpness;
half _Shininess;
sampler2D _GrabTexture;
float4 _GrabTexture_TexelSize;
half _Distortion;



struct Input {
	float2 uv_MainTex;
	float2 uv_BumpMap;
	float4 screenPos;
};


void surf (Input IN, inout SurfaceOutput o) {
	fixed3 nor = UnpackNormal (tex2D(_BumpMap, IN.uv_BumpMap));
	fixed4 tex = tex2D(_MainTex, IN.uv_MainTex);
	
	float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
	screenUV += nor.xy * _Distortion ;
	fixed4 ref = tex2D(_Ref, screenUV);
		
	float4 screenUV2 = IN.screenPos;
	#if UNITY_UV_STARTS_AT_TOP
	float scale = -1.0;
	#else
	float scale = 1.0;
	#endif
	screenUV2.y = (screenUV2.y - screenUV2.w*0.5)* scale+ screenUV2.w * 0.5;
	screenUV2.xy = screenUV2.xy / screenUV2.w;
	screenUV2.xy += nor.xy * _Distortion;
	
	fixed4 trans = tex2D(_GrabTexture,screenUV2.xy);
		
	o.Albedo = tex.rgb * _Color.rgb * _BlendLevel;
	o.Emission = lerp(ref.rgb,trans.rgb,_Transparency);
	o.Normal = nor.rgb * _Bumpness;
	o.Gloss = tex.a;
	o.Alpha = tex.a * _Color.a;
	o.Specular = _Shininess;	
}
ENDCG
}

FallBack "Transparent/VertexLit"
}