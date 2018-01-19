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

// Shared shader code for pixel view rays, given screen pos and camera frame vectors.

// Camera vectors are passed in as this shader is run from a post proc camera, so the unity built-in values are not useful.
//uniform float3 _CamPos;
uniform float3 _CamForward;
uniform float3 _CamRight;
uniform float  _TanFov;

void computeCamera( in float2 screenPos, out float3 ro, out float3 rd )
{
	float tanFovH = _TanFov;
	float tanFovV = _TanFov * _ScreenParams.y / _ScreenParams.x;
	float3 camUp = cross( _CamForward.xyz, _CamRight.xyz );
	ro = _WorldSpaceCameraPos;	rd = normalize( _CamForward.xyz + screenPos.y * tanFovV * camUp.xyz + screenPos.x * tanFovH * _CamRight.xyz );
}