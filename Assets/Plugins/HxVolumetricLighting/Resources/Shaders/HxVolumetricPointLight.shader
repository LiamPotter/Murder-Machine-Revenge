// Upgrade NOTE: replaced '_CameraToWorld' with 'unity_CameraToWorld'
// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

// Upgrade NOTE: replaced '_CameraToWorld' with 'unity_CameraToWorld'
// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

Shader "Hidden/HxVolumetricPointLight"
{
	CGINCLUDE
#define SHADOWS_NATIVE
#define POINT
#define SHADOWS_CUBE
#include "UnityCG.cginc"
#include "UnityDeferredLibrary.cginc"
#include "UnityPBSLighting.cginc"
#include "HxVolumetricLightCore.cginc"





#pragma multi_compile __ DENSITYPARTICLES_ON 
#pragma multi_compile __ VTRANSPARENCY_ON
#pragma multi_compile __ FULL_ON
#pragma multi_compile __ SHADOWS_ON
#pragma multi_compile __ NOISE_ON 
#pragma multi_compile __ COOKIE_ON
#pragma multi_compile __ HEIGHTFOG_ON

	uniform float3 LightColour;
	uniform float3 LightColour2;
	float3 CameraFoward;
	uniform float4 _LightParams;
	uniform float4 _CustomLightPosition;

	uniform sampler2D Tile5x5;

	float4x4 InverseViewMatrix;
	float4x4 VolumetricMVP;
	float4x4 VolumetricMV;
	float TintPercent;

#ifdef FULL_ON

#else
float4x4 _InvViewProj;
sampler2D_float  VolumetricDepth;
#endif

#ifdef COOKIE_ON
samplerCUBE PointCookieTexture;
#endif

	float VolumeScale;
	uniform float VolumeRes;
	float4 _CameraDepthTexture_TexelSize;


	
	struct appdata {
		float4 vertex : POSITION;
		float3 texcoord : TEXCOORD0;

	};

	struct v2f {
		float4 pos : SV_POSITION;
		float4 uv : TEXCOORD0;
		#ifdef FULL_ON
		float3 ray : TEXCOORD1;
		#endif
	};


	float3 ShadowBias;
#ifdef SHADOWS_ON

	
	float ShadowDistance;
	//uniform samplerCUBE_float _ShadowMapTexture;

	inline float CustomSampleCubeDistance(float3 vec)
	{
		return UnityDecodeCubeShadowDepth(texCUBElod(_ShadowMapTexture, float4(vec.xyz,0)));
	}
	
	inline half CustomSampleShadowmap(float3 vec)
	{
		float mydist = ((length(vec) + ShadowBias.x) * _LightParams.y);
		
		float dist = CustomSampleCubeDistance(vec);
	
		return dist < mydist ? 0 : 1.0;
	}
#endif

	v2f vert(appdata v)
	{
		v2f o;
		o.pos = mul(VolumetricMVP, v.vertex);
		o.uv = ComputeScreenPos(o.pos);

		#ifdef FULL_ON
		o.ray = mul(VolumetricMV, v.vertex).xyz * float3(-1, -1, 1);
		#endif
		return o;
	}


	static const float PI = 3.14159265f;
	static const float PI4 = 12.5663706f;
	static const float G_SCATTERING = 0.8;

	float ComputeScattering(float lightDotView)
	{
		float result = 1.0f - G_SCATTERING;
		result *= result;
		result /= (4.0f * PI * pow(1.0f + G_SCATTERING * G_SCATTERING - (2.0f * G_SCATTERING) *      lightDotView, 1.5f));
		return result;
	}


	struct Intersection {
		bool hit;
		float t1, t2;
	};

	Intersection lineVsSphere(float3 linePos, float3 lineDir, float3 spherePos, float sphereRadius)
	{
		float3 sphereToLine = linePos - spherePos;
		float b = 2 * dot(lineDir, sphereToLine);
		float c = dot(sphereToLine, sphereToLine) - sphereRadius * sphereRadius;

		float sqrtInner = b * b - 4 * c;
		if (sqrtInner < 0) {
			clip(-1);
		}

		float sqrtProd = sqrt(sqrtInner);
		float t1 = (-b - sqrtProd) / 2;
		float t2 = (-b + sqrtProd) / 2;

		Intersection i;
		i.hit = true;
		i.t1 = t1;
		i.t2 = t2;

		return i;
	}



	vr MarchColor(float3 wpos, float index,float2 uv)
	{
		vr vrout;
		float3 dif = wpos - _CustomLightPosition;
		float3 dir = normalize(wpos - _WorldSpaceCameraPos);
		float rayDis = length(wpos - _WorldSpaceCameraPos);
		int NUM_SAMPLES = min(Density.y, 128);
		Intersection intersect = lineVsSphere(_WorldSpaceCameraPos, dir, _CustomLightPosition, _LightParams.z);

		float endT = min(max(intersect.t2, intersect.t1), rayDis);
		float startT = max(min(intersect.t2, intersect.t1), 0);

		clip(endT - startT - 0.0001f); //clip if near is behind depth //this shouldnt ever be called now

		float3 EnterPoint = _WorldSpaceCameraPos + dir * (startT);
		float3 ExitPoint = _WorldSpaceCameraPos + dir * (endT);

		float MarchLength = endT - startT;
		float stepSize = MarchLength / (NUM_SAMPLES);
		float3 dirScaled = dir * stepSize;
		
		float3 Offset = dirScaled * index;
	
		EnterPoint += Offset;
		#ifdef NOISE_ON		
		//todo heightfog...
		float extinction = (startT) * Density.x * Density.w;
		#else
		float extinction = (startT) * Density.x * Density.w;
		#endif
		float extinctionAdd = stepSize * Density.w;
		float3 AccumulatedColor = float3(0,0,0);
		

#if defined (VTRANSPARENCY_ON) ||  (DENSITYPARTICLES_ON)
		float zScale = (dot(CameraFoward, dir));
		float rayWorldDis = length(EnterPoint - _WorldSpaceCameraPos) * zScale;
		float zStep = stepSize / zScale;
#endif

#ifdef DENSITYPARTICLES_ON
		DensityMap dm = LoadSliceData(uv, endT);
#endif

#ifdef DENSITYPARTICLES_ON
		extinction += DensityToPoint(dm, rayWorldDis) * Density.w;
		float LowValue = 0;
		float HighValue = 0;
		float CurrentLow = -1;
#endif
		float colorBlend = _LightParams.y * length(cross(dir, EnterPoint - _CustomLightPosition));
		float3 rayColor = lerp(LightColour.rgb, LightColour2.rgb, colorBlend * colorBlend);
		float3 tolight;
		float3 ThisColor;
		for (int i = 0; i < NUM_SAMPLES; i++)
		{
			
			tolight = EnterPoint - _CustomLightPosition;

			float cd = 1;
			#ifdef SHADOWS_ON
			cd = CustomSampleShadowmap(tolight);
			#endif
			float3 ld = normalize(tolight);
			float fogDensity = GetFogDensity(EnterPoint);
#ifdef DENSITYPARTICLES_ON
			fogDensity = max(fogDensity + DensityFrom3DTexture(dm, rayWorldDis, LowValue, HighValue, CurrentLow),0);
#endif
			float phase = HenyeyPhase(dot(ld, -dir));


			float att = dot(tolight, tolight) * (1.0 / (_LightParams.z * _LightParams.z));
			float atten = tex2Dlod(_LightTextureB0, float4(att.r, att.r, 0, 0)).UNITY_ATTEN_CHANNEL; 
			
#ifdef COOKIE_ON

			atten *= texCUBElod(PointCookieTexture, float4(mul(unity_WorldToObject, float4(EnterPoint, 1)).xyz, 0)).a;
#endif
			
				ThisColor = lerp(LightColour2.rgb, LightColour.rgb, saturate(TintPercent * (1-att) /2)) * max(0,(phase * cd + (ShadowBias.z * (1 - cd))) * atten * exp(-extinction)* fogDensity * stepSize);
				AccumulatedColor += ThisColor;


#ifdef VTRANSPARENCY_ON
				float lum = Luminance(ThisColor);
				AddTransparency(vrout, lum, rayWorldDis);
#endif


				EnterPoint += dirScaled;
				
				extinction += extinctionAdd * fogDensity; 

#ifdef DENSITYPARTICLES_ON	
				rayWorldDis += zStep;
#endif
		}
		vrout.col0 = float4((AccumulatedColor), 0);
		return vrout;
	}


	vr frag(v2f i)
	{
		float2 uv = i.uv.xy / i.uv.w;

#ifdef FULL_ON 	
		float depth = Linear01Depth(SAMPLE_DEPTH_TEXTURE_LOD(_CameraDepthTexture, float4(uv.xy, 0, 0)));
		float3 wpos = mul(unity_CameraToWorld, float4(i.ray * (_ProjectionParams.z / i.ray.z) * depth, 1)).xyz;	
#else
		float4 depth = ((tex2Dlod(VolumetricDepth, float4(uv, 0, 0))));
		float3 wpos = mul(InverseViewMatrix, float4(depth.yzw * (depth.x), 1)).xyz;
		
#endif

		float index = tex2D(Tile5x5,  fmod((_ScreenParams.xy) * uv.xy, 5) / 5).r;
	
		return MarchColor(wpos,index, uv);// , near, far);
	
	}



		ENDCG
		SubShader
	{
		Fog{ Mode Off }
		Lighting Off
		ZWrite Off

		Blend One One



		Pass
		{
		Blend One One
			ZWrite Off
			ZTest Always
			Cull Front
			CGPROGRAM
#pragma target 3.0
#pragma vertex vert
#pragma fragment frag 
#pragma exclude_renderers nomrt
			ENDCG
		}

		Pass
			{
			Blend One One
				ZWrite Off
				ZTest LEqual
				Cull Back
				CGPROGRAM
#pragma target 3.0
#pragma vertex vert
#pragma fragment frag  
#pragma exclude_renderers nomrt
				ENDCG
			}
	}
}

