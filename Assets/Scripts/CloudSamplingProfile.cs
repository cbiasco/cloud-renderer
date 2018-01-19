//The MIT License(MIT)

//Copyright(c) 2018 Caleb Biasco

//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.



using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class CloudSamplingProfile : ScriptableObject
{

	[Tooltip("The texture sampled for generating clouds; must be generated in octaves of 37 by 17 to work (I believe).")]
	public Texture2D _textureNoise;


	
	public Shader _volShader;	public enum Resolution
	{
		QUARTER = 1,
		HALF = 2,
		FULL = 3
	}	[Space]	[Header("Quality Settings")]

	[Tooltip("Cloud rendering resolution; upsampled to screen size after render. Lower resolutions will reveal more artifacts and render less appealingly with dark cloud colors.")]	public Resolution _resolution = Resolution.QUARTER;	public enum CloudQuality
	{
		LOW = 1,
		MID = 2,
		HIGH = 3,
		ULTRA = 4
	}	[Space]

	[Tooltip("Cloud sampling quality; determines how deeply the noise texture is sampled for cloud generation.")]	public CloudQuality _cloudQuality = CloudQuality.ULTRA;


	public uint _stepCount = 1000;


	[Tooltip("Scales the factor that determines the step length by the distance from the camera. A higher value will render further but with less dense clouds.")]
	public float _cloudStepMultiplier = 1f;


	[Space]
	[Header("Cloud Settings")]

	[Tooltip("Increases the complexity of the clouds once they have been sampled from the noise. Causes tunnels and many little bits at high values.")]	public float _noiseMultiplier = 2.75f;


	[Tooltip("Increases the sample range of the texture by dividing the sample value. This causes the clouds to become \"bigger\" with large values, but also requires a greater vertical render range to get complete occlusion through the clouds.")]
	public float _cloudGranularity = 4f;


	[Tooltip("Range where the clouds render in vertical world space. X is the bottom of the clouds, Y is the top of the clouds.")]
	public Vector2 _cloudVerticalRange = new Vector2(-100f, 0f);


	[Tooltip("Maximum render range of the clouds from the camera. Essentially a far clip plane for the cloud renderer.")]
	public float _cloudDistanceThreshold = 5000f;
	[Space]

	[Tooltip("Fades the clouds gradually as they get closer to the bottom of the vertical render range.")]
	public bool _cloudFade = false;

	[Tooltip("Alters the size of the clouds as they get further away from the camera. Smaller values yield larger clouds, and subzero inclusive values don't modify the clouds based on distance at all." +
		"\n\nNOTE: Smaller values will cause the clouds to seemingly shift as the camera approaches them. This is by the nature of the modifier, and cannot be avoided when using it.")]
	public float _parallaxQuotient = -1f;


	[Tooltip("Displaces the clouds every frame. Can be altered at runtime without disturbing the current cloud state.")]
	public Vector3 _wind = new Vector3(.1f, 0f, 0f);


	[Space]
	[Header("Light Settings")]

	[Tooltip("Direction of the sunlight that hits the clouds.")]	public Vector3 _sunDirection = Vector3.forward;


	[Tooltip("Strength of the sunlight.")]	public float _sunStrength = 1f;


	[Tooltip("Color of the sunlight.")]
	[ColorUsage(false, true, 0f, 8f, 1f / 8f, 3f)]
	public Color _sunColor;	[Space]	[Header("Color Settings")]

	[Tooltip("Color used for sparser parts of the clouds.")]	[ColorUsage(false, true, 0f, 8f, 1f/8f, 3f)]	public Color _outerColor = new Color(1f, 1f, 1f);


	[Tooltip("Color used for denser parts of the clouds.")]
	[ColorUsage(false, true, 0f, 8f, 1f / 8f, 3f)]
	public Color _innerColor = new Color(.7f, .7f, .7f);	[Space]	[Header("Debug Settings")]

	[Tooltip("Forces the renderer to render the entire frame in the current quality setting.")]	public bool _disableAdaptiveRendering = false;


	[Tooltip("Draws the adaptive rendering stencil on top of the output frame. Shows nothing on full resolution because adaptive rendering is not needed at that level.")]	public bool _drawStencil = false;

}
