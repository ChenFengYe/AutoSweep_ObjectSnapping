Shader "Mirrors/Bumped Specular" {
Properties {
	_Color ("Main Color", Color) = (1,1,1,1)
	_MainTex ("Base (RGB) Gloss (A)", 2D) = "white" {}
	_BlendLevel("Main Material Blend Level",Range(0,1))=1
	_SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 1)
	_Shininess ("Shininess", Range (0.03, 1)) = 0.078125
	_BumpMap ("Normalmap", 2D) = "bump" {}
	_Bumpness ("Bump Rate",Range(0,1))= 0.5
	_Ref ("For Mirror reflection,don't set it!", 2D) = "white" {}
	_RefColor("Reflection Color",Color) = (1,1,1,1)
	_RefRate ("Reflective Rate", Range (0, 1)) = 1
	_Distortion ("Reflective Distortion", Range (0, 1)) = 0
	
}
SubShader { 
	Tags { "RenderType"="Opaque" }
	LOD 400
	
CGPROGRAM
#pragma surface surf BlinnPhong
#pragma target 3.0
#pragma debug

sampler2D _MainTex;
sampler2D _BumpMap;
fixed4 _Color;
half _Shininess;
half _RefRate;
half _Bumpness;
half _BlendLevel;
half _Distortion;
fixed4 _RefColor;
sampler2D _Ref;

struct Input {
	float2 uv_MainTex;
	float2 uv_BumpMap;
	float2 uv_Ref ;
	float4 screenPos;
};


void surf (Input IN, inout SurfaceOutput o) {
	fixed3 nor = UnpackNormal (tex2D(_BumpMap, IN.uv_BumpMap));
	fixed4 tex = tex2D(_MainTex, IN.uv_MainTex);
	float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
	screenUV += nor.xy * _Distortion;
	fixed4 ref = tex2D(_Ref, screenUV);
	o.Albedo = tex.rgb * _Color.rgb * _BlendLevel;
	o.Emission = ref.rgb * _RefColor.rgb * _RefRate;
	o.Normal = nor.rgb * _Bumpness;
	o.Gloss = tex.a;
	o.Alpha = tex.a * _Color.a;
	o.Specular = _Shininess;	
}
ENDCG
}

FallBack "Specular"
}
