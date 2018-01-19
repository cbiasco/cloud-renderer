// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Modified from Michal Skalsky's original BlitAdd by Caleb Biasco (2018)

//  Copyright(c) 2016, Michal Skalsky
//  All rights reserved.
//
//  Redistribution and use in source and binary forms, with or without modification,
//  are permitted provided that the following conditions are met:
//
//  1. Redistributions of source code must retain the above copyright notice,
//     this list of conditions and the following disclaimer.
//
//  2. Redistributions in binary form must reproduce the above copyright notice,
//     this list of conditions and the following disclaimer in the documentation
//     and/or other materials provided with the distribution.
//
//  3. Neither the name of the copyright holder nor the names of its contributors
//     may be used to endorse or promote products derived from this software without
//     specific prior written permission.
//
//  THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY
//  EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
//  OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.IN NO EVENT
//  SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
//  SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT
//  OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
//  HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR
//  TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE,
//  EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.



Shader "Hidden/BlitCombine" 
{
	Properties
	{
		_MainTex("Texture", any) = "" {}
		_Stencil("Stencil", 2D) = "white" {}
	}

	CGINCLUDE;

#include "UnityCG.cginc"

		sampler2D _MainTex;
	sampler2D _Source;
	sampler2D _Stencil;
	uniform float4 _MainTex_ST;

	struct appdata_t 
	{
		float4 vertex : POSITION;
		float2 texcoord : TEXCOORD0;
	};

	struct v2f 
	{
		float4 vertex : SV_POSITION;
		float2 texcoord : TEXCOORD0;
	};

	v2f vert(appdata_t v)
	{
		v2f o;
		o.vertex = UnityObjectToClipPos(v.vertex);
		o.texcoord = TRANSFORM_TEX(v.texcoord.xy, _MainTex);
		return o;
	}

	fixed4 frag(v2f i) : SV_Target
	{
		float4 clouds = tex2D(_MainTex, i.texcoord);

		return fixed4(tex2Dlod(_Source,float4(i.texcoord, 0., 0.))*(1.0 - clouds.w) + clouds.xyz * clouds.w,1.0);
	}

	fixed4 fragreplace(v2f i) : SV_Target
	{
		float4 stencil = tex2D(_Stencil, i.texcoord);

		if (stencil.w > .001)
			return tex2D(_MainTex, i.texcoord);
		else
			return tex2Dlod(_Source,float4(i.texcoord, 0., 0.));
	}

	fixed4 fraghalf(v2f i) : SV_Target
	{
		float4 clouds = tex2D(_MainTex, i.texcoord);

		if (clouds.w > .001)
			return fixed4(tex2Dlod(_Source,float4(i.texcoord, 0., 0.))*(.5) + clouds.xyz * .5,1.0);
		else
			return tex2Dlod(_Source,float4(i.texcoord, 0., 0.));

	}

	ENDCG

	SubShader
	{
		
		// Pass 0: regular combine

		Pass
		{
			ZTest Always Cull Off ZWrite Off
			Blend One Zero

			CGPROGRAM
	#pragma vertex vert
	#pragma fragment frag
			ENDCG
		}

		// Pass 1: complete replacement combine

		Pass
		{
			ZTest Always Cull Off ZWrite Off
			Blend One Zero

			CGPROGRAM
	#pragma vertex vert
	#pragma fragment fragreplace
			ENDCG
		}

		// Pass 2: half-blend combine

			Pass
		{
			ZTest Always Cull Off ZWrite Off
			Blend One Zero

			CGPROGRAM
#pragma vertex vert
#pragma fragment fraghalf
			ENDCG
		}
	}
	Fallback Off
}