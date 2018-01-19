//The MIT License(MIT)

//Copyright(c) 2015-2016 Huw Bowles, Daniel Zimmermann, Beibei Wang

//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.
// Adapted by Caleb Biasco (2018)

// Standard raymarching - samples are placed on parallel planes that are orthogonal to the view z axis. Samples
// are stationary in view space (move with the camera).

// An alternative would be Fixed-R sampling (samples placed on concentric spheres emanating from the viewer position).
// This layout works better for camera rotations but breaks down for sideways and up/down camera motion.

Shader "VolSample/Volume Render Custom" {
	Properties{
		_MainTex( "", 2D ) = "white" {}
	}
	
	CGINCLUDE;


	uniform sampler2D _MainTex;
	uniform sampler2D _CameraDepthTexture;	uniform int _UseDepthTexture;

	#include "UnityCG.cginc"

	#include "SceneClouds_Custom.cginc"
	#include "RayMarchCore_Custom.cginc"
	#include "Camera.cginc"

	struct v2f
	{
		float4 pos : SV_POSITION;
		float4 screenPos : TEXCOORD1;
	};

	v2f vert( appdata_base v )
	{
		v2f o;
		o.pos = UnityObjectToClipPos( v.vertex );
		o.screenPos = ComputeScreenPos( o.pos );
		return o;
	}
	float4 frag( v2f i ) : SV_Target
	{
		float2 q = i.screenPos.xy / i.screenPos.w;
		float2 p = 2.0*(q - 0.5);

		// camera
		float3 ro, rd;		computeCamera(p, ro, rd);

		// z buffer / scene depth for this pixel
		float depthValue = (_UseDepthTexture > .5) ? LinearEyeDepth( tex2Dproj( _CameraDepthTexture, UNITY_PROJ_COORD( i.screenPos ) ).r ) : _CloudDistanceThreshold;

		// march through volume
		// samples move with camera
		float4 clouds = RayMarchFixedZ( ro, rd, depthValue );

		return clouds;	}	/* Fragment shader for determining the stencil by the depth buffer */
	float4 stencilfrag(v2f i) : SV_Target	{
		float depthValue = LinearEyeDepth(tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(i.screenPos)).r);		if (depthValue < _ProjectionParams.z*.95) /* gives us a little space for checking for geometry in the scene */			return float4(1., 0., 1., 1.);		else			return (float4) 0.;	}		/* Fragment shader for drawing the stencil to the screen */	float4 stencilfragdraw( v2f i ) : SV_Target	{				if (tex2Dproj( _MainTex, UNITY_PROJ_COORD( i.screenPos ) ).x > .5)			return frag(i);		else			return (float4) 0.;	}

	ENDCG

	Subshader
	{
		// Pass 0: One blend weight
		Pass
		{
			ZTest Always Cull Off ZWrite Off

			CGPROGRAM
			#pragma target 3.0   
			#pragma vertex vert
			#pragma fragment frag
			ENDCG
		}		// Pass 1: Magenta stencil
		Pass
		{
			ZTest Always Cull Off ZWrite Off

			CGPROGRAM
			#pragma target 3.0   
			#pragma vertex vert
			#pragma fragment stencilfrag
			ENDCG
		}		// Pass 2: Utilize stencil		Pass				{		
			ZTest Always Cull Off ZWrite Off

			CGPROGRAM
			#pragma target 3.0   
			#pragma vertex vert
			#pragma fragment stencilfragdraw
			ENDCG		}
	}

	Fallback off

} // shader
