Shader "Unlit/SCP099Eye"
{
	Properties
	{
		[HideInInspector]_MainTex ("Texture", 2D) = "white" {}
		_EyeYScale("Default Eye Y scale", Range(0, 0.4)) = 0.1
		_EyeXScale("Eye X Scale", Range(0, 3.0)) = 0.5
	    _EyeLidTex("Eye Lid Texture", 2D) = "red" {}
		_EyeLidThickness("Eye Lid Thickness", Range(0, 0.5)) = 0.01
		_ScleraTex("Sclera Texture", 2D) = "white" {}
		_IrisTex("Iris Texture", 2D) = "black" {}
		_IrisColor("Color OF Iris", Color) = (1,1,1,1)
		_EyeIrisSize("Size of Iris", Range(0, 0.5)) = 0.01
		_BumpAmt("Normal Distortion Scalar", range(0,128)) = 10
		_BumpMap("Normalmap", 2D) = "bump" {}
		_DistortRadius("DistortRadius", Range(0, 1.0)) = 0.1
		_DistortBlendRadius("Distort blend radius", Range(0, 0.3)) = 0.1
		[HideInInspector]_Fade("Fade", Range(0, 1.0)) = 0
		[HideInInspector]_LookDirX("Iris X Look Coord", Range(0, 1)) = 0.5
		[HideInInspector]_LookDirY("Iris Y Look Coord", Range(0, 1)) = 0.5
		_LookMaxRadius("Max Look radius of Iris", Range(0, 1)) = 0.1
	}
	SubShader
	{
		// to be rendered after geometry
		Tags { "RenderType" = "Transparent"}
		Blend SrcAlpha OneMinusSrcAlpha
		CUll Off
		ZWrite Off
		ZTest On
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
			uniform float4 _MainTex_ST;
			uniform float _EyeLidThickness;
			uniform float _EyeIrisSize;
			uniform float _EyeXScale;
			uniform float _EyeYScale;
			uniform float _DistortRadius;
			uniform float _DistortBlendRadius;
			uniform float _Fade;
			uniform float _LookDirX;
			uniform float _LookDirY;
			uniform float _LookMaxRadius;
			uniform fixed4 _IrisColor;
			uniform float _BumpAmt;
			uniform float4 _BumpMap_ST;
			uniform sampler2D _GrabTexture;
			uniform float4 _GrabTexture_TexelSize;
			uniform sampler2D _BumpMap;

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

				fixed2 centerCoord = fixed2(0.5, 0.5);

				fixed isBlendPixel = 0;

				float sinWaveValue = sinCurve(o.uv.x, centerCoord.x, centerCoord.y, (_EyeXScale), _EyeYScale);

				float sinWaveValueNegative = sinCurve(o.uv.x, centerCoord.x, centerCoord.y, (_EyeXScale), - _EyeYScale);

				if (distance(o.uv, centerCoord) > _DistortRadius) {
					return fixed4(0, 0, 0, 0);
				}

				fixed alpha = 0;

				// sclera and iris
				if (o.uv.y > sinWaveValueNegative && o.uv.y < sinWaveValue) {
					half2 lookCenter = half2(centerCoord.x + (_LookDirX *_LookMaxRadius), centerCoord.y + (_LookDirY * _LookMaxRadius));
					if (distance(o.uv, lookCenter) < _EyeIrisSize) {
						// iris uvs based on iris thickness
						float2 irisCoords = float2((((o.uv.x - lookCenter.x) / _EyeIrisSize) / 2) + 0.5, (((o.uv.y - lookCenter.y) / _EyeIrisSize) / 2) + 0.5);

						col = col * (tex2D(_IrisTex, irisCoords) *_IrisColor);
					}
					else {
						// sclera uvs based on sclera lrngth
						float2 scleraCoords = float2((((o.uv.x - centerCoord.x) / (1.0 / _EyeXScale)) ) + 0.5, (((o.uv.y - centerCoord.y) / (1.0 / _EyeXScale)) ) + 0.5);
						col = col * tex2D(_ScleraTex, scleraCoords);
					}
				}
				else {
					// upper lid
					if (abs(o.uv.y - sinWaveValue) < _EyeLidThickness && o.uv.y > sinWaveValue) {
						if (o.uv.y + _EyeLidThickness > sinWaveValueNegative) {
							alpha = max((abs(o.uv.y - sinWaveValue) / _EyeLidThickness), alpha);
							isBlendPixel = 1;
						}
					}

					// lower lid
					if (abs(o.uv.y - sinWaveValueNegative) < _EyeLidThickness  && o.uv.y < sinWaveValueNegative) {
						if (o.uv.y - _EyeLidThickness < sinWaveValue) {
							alpha = max((abs(o.uv.y - sinWaveValueNegative) / _EyeLidThickness), alpha);
							isBlendPixel = 1;
						}
					}
				}

				// make sure x scale is in bounds of sin function
				if (isBlendPixel && o.uv.x < (centerCoord.x - ((1.0 / _EyeXScale) / 2)) - (_EyeLidThickness / 1.5)) {
					isBlendPixel = 0;
				}

				if (isBlendPixel && o.uv.x >(centerCoord.x + ((1.0 / _EyeXScale) / 2)) + (_EyeLidThickness / 1.5)) {
					isBlendPixel = 0;
				}

				// make edges of eyelids blend into distortion map
				if (isBlendPixel  == 1) {
					float edgeDistanceRight = abs(o.uv.x - ((centerCoord.x - ((1.0 / _EyeXScale) / 2)) - (_EyeLidThickness / 1.5)));
					float edgeDistanceLeft = abs(o.uv.x - ((centerCoord.x + ((1.0 / _EyeXScale) / 2)) + (_EyeLidThickness / 1.5)));
					if (edgeDistanceRight < (_EyeLidThickness) ) {
						alpha = max(alpha, 1 - (edgeDistanceRight / _EyeLidThickness));
					}
					if (edgeDistanceLeft < (_EyeLidThickness )) {
						alpha = max(alpha, 1 -(edgeDistanceLeft / _EyeLidThickness));
					}

					col = col * lerp(tex2D(_EyeLidTex, o.uv), fixed4(1, 1, 1, 1), (alpha));
				}

				//distortion effect blending with pure transparency
				if (distance(o.uv, centerCoord) > (_DistortRadius - _DistortBlendRadius)) {
					alpha = (distance(o.uv, centerCoord) - (_DistortRadius - _DistortBlendRadius)) / _DistortBlendRadius;
					col = lerp(col, fixed4(col.x, col.y, col.z, 0), alpha);
				}

				col.a = col.a - _Fade;

				return col;
			}
			ENDCG
		}
	}
}
