Shader "Hidden/HxTransparencyBlur"
{
	Properties
	{
		_MainTex("-", 2D) = "white" {}
	}

		CGINCLUDE

#include "UnityCG.cginc"

		sampler2D _MainTex;
	float4 _MainTex_TexelSize;

	// 9-tap Gaussian filter with linear sampling
	// http://rastergrid.com/blog/2010/09/efficient-gaussian-blur-with-linear-sampling/
	half4 gaussian_filter(float2 uv, float2 stride)
	{
		half4 s = tex2D(_MainTex, uv) * 0.227027027;

		float2 d1 = stride * 1.3846153846;
		s += tex2D(_MainTex, uv + d1) * 0.3162162162;
		s += tex2D(_MainTex, uv - d1) * 0.3162162162;

		float2 d2 = stride * 3.2307692308;
		s += tex2D(_MainTex, uv + d2) * 0.0702702703;
		s += tex2D(_MainTex, uv - d2) * 0.0702702703;

		return s;
	}

	// Separable Gaussian filters
	half4 frag_blur_h(v2f_img i) : SV_Target
	{
		return gaussian_filter(i.uv, float2(_MainTex_TexelSize.x, 0));
	}

		half4 frag_blur_v(v2f_img i) : SV_Target
	{
		return gaussian_filter(i.uv, float2(0, _MainTex_TexelSize.y));
	}

	half4 frag_blur_v_LDR(v2f_img i) : SV_Target
	{
		float4 texColor =  gaussian_filter(i.uv, float2(0, _MainTex_TexelSize.y));
	
	//this creates too many artifacts...
			//float lumTm = texColor.r * 1;
			//float scale = lumTm / (1 + lumTm);
			//texColor.r = texColor.r * scale / texColor.r;
			//
			// lumTm = texColor.g * 1;
			// scale = lumTm / (1 + lumTm);
			//texColor.g = texColor.g * scale / texColor.g;
			//
			//lumTm = texColor.b * 1;
			//scale = lumTm / (1 + lumTm);
			//texColor.b = texColor.b * scale / texColor.b;
			//
			//
			//lumTm = texColor.a * 1;
			//scale = lumTm / (1 + lumTm);
			//texColor.a = texColor.a * scale / texColor.a;

			return texColor;
	}


	half4 frag_HDR_LDR(v2f_img i) : SV_Target
	{
		float4 texColor = tex2D(_MainTex, i.uv);
		float lum = max(Luminance(texColor.rgb), 0.00001f);
		float lumTm = lum * 1;
		float scale = lumTm / (1 + lumTm);

	


		return half4((texColor.rgb * scale / lum), texColor.a);
	}

	
		ENDCG

		Subshader
	{

		Pass
		{
			ZTest Always Cull Off ZWrite Off
			CGPROGRAM
#pragma vertex vert_img
#pragma fragment frag_blur_h
#pragma target 3.0
			ENDCG
		}
			Pass
		{
			ZTest Always Cull Off ZWrite Off
			CGPROGRAM
#pragma vertex vert_img
#pragma fragment frag_blur_v
#pragma target 3.0
			ENDCG
		}

			Pass
		{
			ZTest Always Cull Off ZWrite Off
			CGPROGRAM
#pragma vertex vert_img
#pragma fragment frag_blur_v_LDR
#pragma target 3.0
			ENDCG
		}
			Pass
		{
			ZTest Always Cull Off ZWrite Off
			CGPROGRAM
#pragma vertex vert_img
#pragma fragment frag_HDR_LDR
#pragma target 3.0
			ENDCG
		}
	}
}
