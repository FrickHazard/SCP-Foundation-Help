Shader "Unlit/SCP099Eye"
{
	Properties
	{
		[HideInInspector]_MainTex ("Texture", 2D) = "white" {}
	    _EyeLidTex("Eye Lid Texture", 2D) = "red" {}
		_ScleraTex("Sclera Texture", 2D) = "white" {}
		_IrisTex("Iris Texture", 2D) = "black" {}
        _EyeLidThickness("Eye Lid Thickness", Range(0, 0.5)) = 0.01
		_EyeIrisSize("Size of Iris", Range(0, 0.5)) = 0.01
		_EyeYScale("Eye Y scale", Range(0, 1.0)) = 0.1
		_EyeXScale("Eye X Scale", Range(0, 1.0)) = 0.5
		_EyeBlendAmount("Eye lid Blend", Range(0, 2.0)) = 1.2
		_BumpAmt("Normal Distortion Amount", range(0,128)) = 10
		_DistortionDiffuseTex("Texture", 2D) = "white" {}
		_BumpMap("Normalmap", 2D) = "bump" {}
	}
	SubShader
	{
		// to be rendered after geometry
		Tags { "RenderType" = "Transparent"}
		Blend SrcAlpha OneMinusSrcAlpha
		CUll Off
		ZWrite Off
		ZTest Off
		LOD 200

		GrabPass {
			Name "BASE"
			Tags{ "LightMode" = "Always" }
		}

		Pass
		{
			Name "BASE"
			Tags{ "LightMode" = "Always" }
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			#include "UnityCG.cginc"

			uniform sampler2D _MainTex;
			uniform sampler2D _EyeLidTex;
			uniform sampler2D _ScleraTex;
			uniform sampler2D _IrisTex;
			uniform sampler2D _DistortionDiffuseTex;
			uniform float4 _MainTex_ST;
			uniform float _EyeLidThickness;
			uniform float _EyeIrisSize;
			uniform float _EyeXScale;
			uniform float _EyeYScale;
			uniform float _EyeBlendAmount;
			float _BumpAmt;
			float4 _BumpMap_ST;
			sampler2D _GrabTexture;
			float4 _GrabTexture_TexelSize;
			sampler2D _BumpMap;

			// sin curve function
			float sinCurve(float value, float xOffset, float yOffset, float xScale, float yScale)
			{
				return (yScale * sin(((value * xScale) - ((xOffset * xScale) + 1.5)) * 3.14)) + yOffset;
			}

			struct input
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct output
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float2 uvbump : TEXCOORD1;
				float4 uvgrab : TEXCOORD2;
			};
			
			output vert (input v)
			{
				output o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.uvgrab = ComputeGrabScreenPos(o.vertex);
				o.uvbump = TRANSFORM_TEX(v.uv, _BumpMap);
				return o;
			}
			
			fixed4 frag (output o) : SV_Target
			{
				// this block is a normal distort based on grab amount
				#if UNITY_SINGLE_PASS_STEREO
				o.uvgrab.xy = TransformStereoScreenSpaceTex(o.uvgrab.xy, o.uvgrab.w);
				#endif
				half2 bump = UnpackNormal(tex2D(_BumpMap, o.uvbump)).rg;
				float2 offset = bump * _BumpAmt * _GrabTexture_TexelSize.xy;
				#ifdef UNITY_Z_0_FAR_FROM_CLIPSPACE 
				o.uvgrab.xy = offset * UNITY_Z_0_FAR_FROM_CLIPSPACE(o.uvgrab.z) + o.uvgrab.xy;
				#else
				o.uvgrab.xy = offset * o.uvgrab.z + o.uvgrab.xy;
				#endif
				half4 col = tex2Dproj(_GrabTexture, UNITY_PROJ_COORD(o.uvgrab));
				half4 distortColor = tex2D(_DistortionDiffuseTex, o.uv);
				distortColor.a = 0;

				fixed4 eyeLidColor = tex2D(_EyeLidTex, o.uv);
				eyeLidColor.a = 0;

				fixed4 scleraColor = tex2D(_ScleraTex, o.uv);
				scleraColor.a = 0.5;

			    fixed2 centerCoord = (0.5, 0.5);

				fixed invertAlpha = 0;

				float sinWaveValue = sinCurve(o.uv.x, centerCoord.x, centerCoord.y, (_EyeXScale * 2), _EyeYScale);

				float sinWaveValueNegative = sinCurve(o.uv.x, centerCoord.x, centerCoord.y, (_EyeXScale * 2), - _EyeYScale);

				// iris uvs based on iris thickness
				float2 irisCoords = float2((((o.uv.x - centerCoord.x) / _EyeIrisSize)/2) + 0.5,(((o.uv.y - centerCoord.y) / _EyeIrisSize)/2) + 0.5);

				fixed4 irisColor = tex2D(_IrisTex, irisCoords);
				irisColor.a = 1;

				//straight line fix for when yscale is approaching 0
				if (o.uv.x < (centerCoord.x - (_EyeXScale/2) - (_EyeLidThickness/2))) {
					return col;
				}

				if (o.uv.x > (centerCoord.x + (_EyeXScale / 2) + (_EyeLidThickness / 2))) {
					return col;
				}

				// sclera and iris
				if (o.uv.y > sinWaveValueNegative && o.uv.y < sinWaveValue) {
					if (distance(o.uv, centerCoord) < _EyeIrisSize) {
						if (irisCoords.x < 0) return fixed4(1, 0, 0, 1);
						return irisColor;
					}
					else {
						return scleraColor;
					}
				}
				// upper lid
				if (abs(o.uv.y - sinWaveValue) < _EyeLidThickness && o.uv.y > sinWaveValue) {
					if (o.uv.y + _EyeLidThickness > sinWaveValueNegative) {
						col.a = max((abs(o.uv.y - sinWaveValue) / _EyeLidThickness) / _EyeBlendAmount, col.a);
						col.xyz = eyeLidColor.xyz;
						invertAlpha = 1;
					}
				}

				// lower lid
				if (abs(o.uv.y - sinWaveValueNegative) < _EyeLidThickness  && o.uv.y < sinWaveValueNegative) {
					if (o.uv.y - _EyeLidThickness < sinWaveValue) {
						col.a = max((abs(o.uv.y - sinWaveValueNegative) / _EyeLidThickness)/ _EyeBlendAmount, col.a);
						col.xyz = eyeLidColor.xyz;
						invertAlpha = 1;
					}
				}

				// make edges less alpha
				if(invertAlpha) col.a = 1 - col.a;

				return col;
			}
			ENDCG
		}
	}
}
