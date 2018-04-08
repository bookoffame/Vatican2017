Shader "Hidden/DepthHack"
{
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			ZTest Off
			Cull Off
			//ColorMask 0
	
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
			};


			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.vertex.z = 0;
				return o;
			}
                    
			float frag (v2f i) : DEPTH
			{

	#if defined(UNITY_REVERSED_Z)
				return (0,0,0,0);
    #else
				return (1,1,1,1);
	#endif
			}
			ENDCG
		}
	}
}
