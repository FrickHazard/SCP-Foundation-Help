// because shader is for post procsessing hide
Shader "Hidden/Blink"
{
		Properties {
			// This is the camera render texture
			[HideInInspector]_MainTex("Base (RGB)", 2D) = "white" {}
			// Used like a bool to tell if blinking
		    [HideInInspector]_IsBlinking("Can See", Range(0, 1)) = 0.0
			// blink animation percent 0 - 1
			[HideInInspector]_EyeClosedPercent("Blink Animation Percentage", Range(0, 1)) = 0.0
			// Eye Texture
			_EyeTex("Eye Texture", 2D) = "black" {}
			// curve properties for hoizontal scale
            _HorizontalScale("Horizontal Scale", Float) = 1
			// curve properties for vertical scale
			_VerticalScale("Vertical Scale", Float) = 1.2
		}
			SubShader {
			Pass {
			CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

			uniform sampler2D _MainTex;
		    uniform sampler2D _EyeTex;
		    uniform float _IsBlinking;
			uniform float _EyeClosedPercent;
			uniform float _HorizontalScale;
			uniform float _VerticalScale;

		    float4 frag(v2f_img i) : COLOR {
              // camera render texture
			  float4 cameraViewTexture = tex2D(_MainTex, i.uv);
			  // eye texture
			  float4 eyeTexture = tex2D(_EyeTex, i.uv);

			  // not blinking show default screen
			  if (_IsBlinking == 0) {
				return cameraViewTexture;
			  }

			  // rough pi estimate, enough for what we need
			  half ROUGHPI = 3.14592625;

			  float y = i.uv.y;

			  // scale sinusoidal wave by shader property
			  float scaledViewportX = (i.uv.x / _HorizontalScale) + (((_HorizontalScale - 1) / _HorizontalScale) / 2);

			  // apply pi to viewport space
			  float sinXValue = sin(scaledViewportX * ROUGHPI);

			  // inverted percent for sinusoidal curve
			  float percent = (1 - _EyeClosedPercent);

			  // upper lid curve
			  if (y > (percent * sinXValue * _VerticalScale) + 0.5) {
				return eyeTexture;
			  }

			  // lower lid curve
			  if (y < (percent * -sinXValue * _VerticalScale) + 0.5 ) {
				return eyeTexture;
			  }

			  return cameraViewTexture;
		  }
		  ENDCG
		}
	}
}
