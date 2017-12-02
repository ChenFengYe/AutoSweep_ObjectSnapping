
Shader "Mirrors/Transparent Specular Sphere" {
Properties {
	_Transparency("Transparency", Range (0, 1)) = 1
	_Distortion ("Distortion", range (0,30)) = 10
	_Color ("Main Color", Color) = (1,1,1,1)
	_MainTex ("Base (RGB) TransGloss (A)", 2D) = "white" {}
	_BlendLevel("Main Material Blend Level",Range(0,1))=1
	_SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 0)
	_Shininess ("Shininess", Range (0.01, 1)) = 0.078125
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
sampler2D _Ref;
fixed4 _Color;
half _BlendLevel;
half _Transparency;
half _Shininess;
sampler2D _GrabTexture;
float4 _GrabTexture_TexelSize;
half _Distortion;



struct Input {
	float2 uv_MainTex;
	float4 screenPos;
};


void surf (Input IN, inout SurfaceOutput o) {
	fixed4 tex = tex2D(_MainTex, IN.uv_MainTex);
	
	#if UNITY_UV_STARTS_AT_TOP
	float scale = -1.0;
	#else
	float scale = 1.0;
	#endif
	
	float4 screenUV = IN.screenPos;
	float2 offset = (_Distortion * o.Normal) ;
    screenUV.xy  += offset ;
    float3 ref = tex2Dproj( _Ref, screenUV);
	
	float4 screenUV2 = IN.screenPos;
	screenUV2.y = (screenUV2.y - screenUV2.w*0.5)* scale+ screenUV2.w * 0.5;
	offset = _Distortion * o.Normal;
    screenUV2.xy  += offset  ;
	float3 trans = tex2Dproj( _GrabTexture, screenUV2);
	
	o.Albedo = tex.rgb * _Color.rgb * _BlendLevel;
	o.Emission = lerp(ref.rgb,trans.rgb,_Transparency);
	o.Gloss = tex.a;
	o.Alpha = tex.a * _Color.a;
	o.Specular = _Shininess;	
}
ENDCG
}

FallBack "Transparent/VertexLit"
}