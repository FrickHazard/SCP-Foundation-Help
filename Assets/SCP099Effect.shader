// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/SCP099Effect"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	    _ViewportEyeSizeX("Eye Size", Float) = 0.4
		_ViewportEyeSizeY("Eye Size", Float) = 0.4
	}
		SubShader
		{
			Pass
			{
				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#include "UnityCG.cginc"
			// set by effect
			int _EyeCount;
			fixed4 _EyePositions[10];
			// camer color and depth texture
			uniform sampler2D _MainTex;
			uniform sampler2D _CameraDepthTexture;

			uniform float _ViewportEyeSizeX;
			uniform float _ViewportEyeSizeY;

			// sin curve function
			float sinCurve(float value, float xOffset, float yOffset, float xScale, float yScale)
			{
			  return (yScale * sin( ((value * xScale) - ((xOffset * xScale) + 1.5)) * 3.14)) + yOffset;
			}

			struct input
			{
				float4 pos : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct output
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			output vert(input i)
			{
				output o;
				o.pos = UnityObjectToClipPos(i.pos);
				o.uv = i.uv;
				return o;
			}

			fixed4 frag(output o) : COLOR
			{
				// default camera view
				fixed4 cameraView = tex2D(_MainTex, o.uv);

				// in world units
				float depth = LinearEyeDepth(UNITY_SAMPLE_DEPTH(tex2D(_CameraDepthTexture, o.uv)));

				if (depth > 999.7) return cameraView;

				// draw eyes
				for (int i = 0; i < _EyeCount; i++) {

					// scale uv by aspect to get pixel coord
					float2 pixelCoord = float2(o.uv.x * _ScreenParams.x, o.uv.y * _ScreenParams.y);

					float2 eyeCoord = float2(_EyePositions[i].x * _ScreenParams.x, _EyePositions[i].y * _ScreenParams.y);

					half2 eyeBox = half2(_ViewportEyeSizeX * _ScreenParams.x, _ViewportEyeSizeY * _ScreenParams.y);

					//bound test
					if (pixelCoord.x < (eyeCoord.x + eyeBox.x)  && pixelCoord.x > (eyeCoord.x - eyeBox.x)) {
						//upper lid test
						if (abs(pixelCoord.y - sinCurve(pixelCoord.x, eyeCoord.x, eyeCoord.y, (0.5 / eyeBox.x), eyeBox.y)) < 3) {
							return (cameraView + fixed4(0.299, 0.587, 0.114, 0))/2;
						}
						//lower lid test
						if (abs(pixelCoord.y - sinCurve(pixelCoord.x, eyeCoord.x, eyeCoord.y, (0.5 / eyeBox.x), -eyeBox.y)) < 3) {
							return (cameraView + fixed4(0.299, 0.587, 0.114, 0))/2;
						}
						if (distance(eyeCoord, pixelCoord) < eyeBox.x / 2.1) {
							return (cameraView + fixed4(0.999, 0.287, 0.314, 0)) / 2;
						}
					}

					
				}

				// default return camera view pixel
				return cameraView;
			}

			ENDCG
		}
	}
}
