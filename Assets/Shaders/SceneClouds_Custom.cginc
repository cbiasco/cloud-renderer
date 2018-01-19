// License Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License.
// Adapted by Caleb Biasco (2018) from the VolSample repository from Huw Bowles, Daniel Zimmermann, Beibei Wang//		https://github.com/huwb/volsample
// Originated from two Shadertoy masterpieces:
//		Clouds by iq: https://www.shadertoy.com/view/XslGRr
//		Cloud Ten by nimitz: https://www.shadertoy.com/view/XtS3DD

uniform sampler2D _NoiseTex;uniform int _Quality;uniform float _CloudGranularity;uniform float2 _CloudVerticalRange;uniform float _CloudDistanceThreshold;uniform int _CloudFade;uniform float3 _WindDisplacement;uniform float _NoiseMultiplier;uniform float _ParallaxQuotient;uniform float3 _SunDirection;uniform float3 _SunColor;uniform float _SunStrength;uniform float3 _OuterColor;uniform float3 _InnerColor;
float noise( in float3 x )
{
	float3 p = floor( x );
	float3 f = frac( x );
	f = f*f*(3.0 - 2.0*f);

	float2 uv2 = (p.xy + float2(37.0, 17.0)*p.z) + f.xy;
	float2 rg = tex2Dlod( _NoiseTex, float4((uv2 + 0.5) / 256.0, 0.0, 0.0) ).yx;
	return lerp( rg.x, rg.y, f.z );
}

float4 map( in float3 p, in float t )
{	float3 pos = p;	/* Increase size of distant clouds based on the parallax quotient */	if (_ParallaxQuotient > 0) 	{		pos.y += _CloudGranularity + t/_ParallaxQuotient;		pos /= _CloudGranularity + t/_ParallaxQuotient;	}	else	{		pos.y += _CloudGranularity;		pos /= _CloudGranularity;	}	/* Bound the clouds by the top and bottom planes */
	float d = -max(0.0, pos.y - _CloudVerticalRange.y / _CloudGranularity);
	d = min(d, pos.y - (_CloudVerticalRange.x + 2*_CloudGranularity) / _CloudGranularity);

	/* Shift the clouds by the "wind" */

	float3 q = pos - _WindDisplacement;
	/* Sample the noise texture by multiple octaves depending on the "quality" of the clouds. */

	float f;
	f = 0.5000*noise( q ); q = q*2.02;	if (_Quality > 1) f += 0.2500*noise( q );	q = q*2.03;	if (_Quality > 2) f += 0.1250*noise( q ); 	q = q*2.01;	if (_Quality > 3) f += 0.0625*noise( q );	/* Multiply our sampled noise by some factor to modify the differences in density. */	d += _NoiseMultiplier * f;

	/* If the cloud fade parameter is set, fade the clouds to zero as they near the bottom plane. */

	if (_CloudFade > .5)
		d -= 1.0 - (p.y - _CloudVerticalRange.x) / (_CloudVerticalRange.y - _CloudVerticalRange.x);

	/* Clamp the density of the clouds between 0 and 1. */	d = saturate(d);

	float4 res = (float4)d;
	/* Color the clouds based on their density. */
	float3 col = _OuterColor;;
	res.xyz = lerp( col, _InnerColor, res.x*res.x );
	return res;
}

float4 VolumeSampleColor( in float3 pos, in float t )
{
	// Sample the color of the clouds at pos.
	float4 col = map( pos, t );

	// Color the clouds by the sunlight.
	float dif = clamp( (col.w - map( pos - 0.6*_SunDirection, t ).w), 0.0, 1.0 );	float3 lin = float3(1., 1., 1.) + _SunStrength*_SunColor*dif;
	col.xyz *= col.xyz;
	col.xyz *= lin;	// Reduce the strength of the cloud sample and clamp it.	col.a *= 0.35;	col.a = saturate(col.a);
	col.rgb *= col.a;

	return col;
}
