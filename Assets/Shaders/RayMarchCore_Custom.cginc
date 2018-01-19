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
uniform uint _StepCount;uniform float _CloudStepMultiplier;

void RaymarchStep( in float3 pos, in float dt, in float wt, inout float4 sum, in float t )
{
	if( sum.a <= 0.9999 )
	{
		float4 col = VolumeSampleColor( pos, t );
		dt = dt;
		sum += wt * dt * col * (1.0 - sum.a);
	}
}

float4 RayMarchFixedZ( in float3 ro, in float3 rd, in float zbuf )
{
	float4 sum = (float4)0.;

	// setup sampling
	float dt = .1;		float t = dt ;

	bool rayUp = dot(rd, float3(0., 1., 0.)) > 0.;	float angleMultiplier = max(1., 1. - abs(dot(rd, float3(0., 1., 0.))));
	for( int i = 0; i < _StepCount * angleMultiplier; i++ )
	{

		float distToSurf = zbuf - t;		float rayPosY = ro.y + t * rd.y;		/* Calculate the cutoff planes for the top and bottom.		   Involves some hardcoding for our particular case. */		float topCutoff = (_CloudVerticalRange.y + _CloudGranularity*max(1., _ParallaxQuotient) + .06*t + max(0, ro.y)) - rayPosY;		float botCutoff = rayPosY - (_CloudVerticalRange.x - _CloudGranularity*max(1., _ParallaxQuotient) - t/.06 + min(0, ro.y));

		if( distToSurf <= 0.001 || (rayUp && topCutoff < 0) || (!rayUp && botCutoff < 0)) break;
		// Fade out the clouds near the max z distance
		float wt;		if (zbuf < _ProjectionParams.z - 10)
			wt = (distToSurf >= dt) ? 1. : distToSurf / dt;		else			wt = distToSurf / zbuf;
		RaymarchStep( ro + t * rd, dt, wt, sum, t );

		t += max(dt,_CloudStepMultiplier*t*0.0011);			}

	return saturate( sum );
}
