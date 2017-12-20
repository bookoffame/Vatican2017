// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Special/TimePiece"
{
	Properties
	{
		_BumpMap("Normal", 2D) = "normal" {}
		_Refraction("Refraction Magnitude", Range(-0.02, 0.02)) = 0.015
		_NormalMagnitude("Normal Magnitude", Range(-1, 1)) = 1
	}
		SubShader
	{
		Tags{ "RenderType" = "Opaque" }
		LOD 100

		Pass
	{
		CGPROGRAM
#pragma vertex vert
#pragma fragment frag

#include "UnityCG.cginc"
#include "UnityStandardUtils.cginc"

		struct VertIn
	{
		float4 vertex : POSITION;
		float3 normal : NORMAL;
		float4 tangent : TANGENT;
		float2 uv : TEXCOORD0;
	};

	struct FragIn
	{
		float4 vertex : SV_POSITION;
		float3 normal : NORMAL;
		//float3 worldRefl : TEXCOORD0;
		float2 uv : TEXCOORD0;
		float3 screen_uv : TEXCOORD1;
		float3 tangentToWorld[3] : TEXCOORD2;//TEXCOORD3;TEXCOORD4;
		float3 worldPos : TEXCOORD5;
	};

	FragIn vert(VertIn v)
	{
		FragIn o;
		o.vertex = UnityObjectToClipPos(v.vertex);
		o.uv = v.uv;
		o.screen_uv = float3((o.vertex.xy + o.vertex.w) * 0.5, o.vertex.w);
		//jack up uvs
		//o.screen_uv = float3((o.vertex.xy / o.vertex.w + 1) * 0.5, o.vertex.w);
		o.normal = UnityObjectToWorldNormal(v.normal);
		o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

		float4 tangentWorld = float4(UnityObjectToWorldDir(v.tangent.xyz), v.tangent.w);
		float3x3 tangentToWorld = CreateTangentToWorldPerVertex(o.normal, tangentWorld.xyz, tangentWorld.w);
		o.tangentToWorld[0].xyz = tangentToWorld[0];
		o.tangentToWorld[1].xyz = tangentToWorld[1];
		o.tangentToWorld[2].xyz = tangentToWorld[2];

		return o;
	}

	sampler2D _BumpMap;
	sampler2D _TimeCrackTexture;
	float _Refraction;
	float _NormalMagnitude;
	float4 _BumpMap_ST;

	fixed4 frag(FragIn i) : SV_Target
	{
		float3 tangent = i.tangentToWorld[0].xyz;
		float3 binormal = i.tangentToWorld[1].xyz;
		float3 normal = i.tangentToWorld[2].xyz;
		float3 normalTangent = UnpackNormal(tex2D(_BumpMap, TRANSFORM_TEX(i.uv, _BumpMap)));
		float3 normalWorld = normalize(tangent * normalTangent.x + binormal * normalTangent.y + normal * normalTangent.z);
		normalWorld = lerp(i.normal, normalWorld, _NormalMagnitude);
		// Refraction Vector from world Normal
		float3 viewSpaceNormal = mul(UNITY_MATRIX_V, normalWorld);
		float2 refractionVector = viewSpaceNormal.xy * viewSpaceNormal.z  * _Refraction;

		// Perspective correction for screen uv coordinate
		float2 screen_uv = i.screen_uv.xy / i.screen_uv.z;
#if UNITY_UV_STARTS_AT_TOP
		screen_uv.y = 1 - screen_uv.y;
#endif

		fixed4 col = 0;


		// compute world space view direction
		float3 worldViewDir = normalize(UnityWorldSpaceViewDir(i.worldPos));
		float3 worldRefl = reflect(-worldViewDir, normalWorld);
		// sample the default reflection cubemap, using the reflection vector
		half4 skyData = UNITY_SAMPLE_TEXCUBE(unity_SpecCube0, worldRefl);
		// decode cubemap data into actual color
		half3 skyColor = DecodeHDR(skyData, unity_SpecCube0_HDR);
		float alpha = length(skyColor);
		col.rgb += skyColor * (alpha);

		// Read color from time crack camera buffer
		col += tex2D(_TimeCrackTexture, screen_uv + refractionVector);
		//col.rgb = normalWorld;
		return col;
	}
		ENDCG
	}
	}
}
