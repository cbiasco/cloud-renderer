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



using UnityEngine;
using UnityEngine.Rendering;
using System;





#if UNITY_EDITOR
using UnityEditor;
#endif



/// <summary>

/// Drives the volume render.

/// </summary>

[ExecuteInEditMode]

[RequireComponent( typeof( Camera ) )]

public class CloudSampling : UnityStandardAssets.ImageEffects.PostEffectsBase

{

	Camera _camera;
	CommandBuffer _preDepthPass;



    public CloudSamplingProfile _profile;


    Material _volMaterial = null;

	Material _blitCombineMaterial;
	Material _bilateralBlurMaterial;

	

	CloudSamplingProfile.Resolution _currentResolution = CloudSamplingProfile.Resolution.QUARTER;

	RenderTexture _fullCloudTexture;
	RenderTexture _halfCloudTexture;
	RenderTexture _quarterCloudTexture;

	RenderTexture _fullDepthBuffer;
	RenderTexture _halfDepthBuffer;
	RenderTexture _quarterDepthBuffer;

	
	private Vector3 _windDisplacement;


	void Awake()
	{

		_camera = GetComponent<Camera>();
		if (_camera.actualRenderingPath == RenderingPath.Forward)
			_camera.depthTextureMode = DepthTextureMode.Depth; // ???

		_currentResolution = _profile._resolution;


		ChangeResolution();


		_volMaterial = CheckShaderAndCreateMaterial(_profile._volShader, _volMaterial);

		_blitCombineMaterial = CheckShaderAndCreateMaterial(Shader.Find("Hidden/BlitCombine"), _blitCombineMaterial);

		_bilateralBlurMaterial = CheckShaderAndCreateMaterial(Shader.Find("Hidden/BilateralBlur"), _bilateralBlurMaterial);


		_preDepthPass = new CommandBuffer();
		_preDepthPass.name = "PreDepth";

	}


	void OnEnable()

    {
        _camera.depthTextureMode |= DepthTextureMode.Depth;

    }


	void Update()
	{

		if (_currentResolution != _profile._resolution)
		{
			_currentResolution = _profile._resolution;
			ChangeResolution();
		}
		else if ((_fullCloudTexture.width != _camera.pixelWidth || _fullCloudTexture.height != _camera.pixelHeight))
			ChangeResolution();

	}



    void SetKeyword( string keyword, bool en )

    {

        if( _volMaterial == null )

            return;



        if( en )

            _volMaterial.EnableKeyword( keyword );

        else

            _volMaterial.DisableKeyword( keyword );

    }



    [ImageEffectOpaque]

    void OnRenderImage(RenderTexture source, RenderTexture destination)

    {

		if (_profile == null)

		{

			Graphics.Blit(source, destination);

			return;

		}

		_volMaterial = CheckShaderAndCreateMaterial(_profile._volShader, _volMaterial );

		_blitCombineMaterial = CheckShaderAndCreateMaterial(Shader.Find("Hidden/BlitCombine"), _blitCombineMaterial);

		_bilateralBlurMaterial = CheckShaderAndCreateMaterial(Shader.Find("Hidden/BilateralBlur"), _bilateralBlurMaterial);
		


		// check for no shader / shader compile error

		if ( _volMaterial == null )

        {

			Graphics.Blit(source, destination);

            return;

        }



        // we can't just read these from the matrices because the clouds are rendered with a post proc camera

        _volMaterial.SetVector( "_CamPos", transform.position );

        _volMaterial.SetVector( "_CamForward", transform.forward );

        _volMaterial.SetVector( "_CamRight", transform.right );



        // noise texture

        _volMaterial.SetTexture( "_NoiseTex", _profile._textureNoise );


		// cloud quality

		_volMaterial.SetInt("_Quality", (int)_profile._cloudQuality);


		// draw distance

		_volMaterial.SetFloat("_StepCount", _profile._stepCount);


		// cloud step multiplier

		_volMaterial.SetFloat("_CloudStepMultiplier", _profile._cloudStepMultiplier);


		// cloud density

		_volMaterial.SetFloat("_CloudGranularity", _profile._cloudGranularity);


		// noise multiplier

		_volMaterial.SetFloat("_NoiseMultiplier", _profile._noiseMultiplier);


		// parallax quotient

		_volMaterial.SetFloat("_ParallaxQuotient", _profile._parallaxQuotient);


		// cloud thresholds

		_volMaterial.SetVector("_CloudVerticalRange", _profile._cloudVerticalRange);

		_volMaterial.SetFloat("_CloudDistanceThreshold",
			_profile._cloudDistanceThreshold);


		// cloud fade

		_volMaterial.SetInt("_CloudFade", (_profile._cloudFade) ? 1 : 0);

		// cloud wind

		_windDisplacement += _profile._wind * Time.deltaTime;
		_volMaterial.SetVector("_WindDisplacement", _windDisplacement);

		// colors

		_volMaterial.SetVector("_OuterColor", _profile._outerColor);
		_volMaterial.SetVector("_InnerColor", _profile._innerColor);


		// sun

		_volMaterial.SetVector("_SunDirection", (_profile._sunDirection == Vector3.zero) ? Vector3.forward : _profile._sunDirection.normalized);
		_volMaterial.SetVector("_SunColor", _profile._sunColor);
		_volMaterial.SetFloat("_SunStrength", _profile._sunStrength);


		// for generating rays
		
		_volMaterial.SetFloat( "_TanFov", Mathf.Tan(halfFov_horiz_rad) );



		if (!_profile._disableAdaptiveRendering && _profile._resolution != CloudSamplingProfile.Resolution.FULL)
		{

			RenderTexture stencil = RenderTexture.GetTemporary(_fullCloudTexture.width, _fullCloudTexture.height, 0, RenderTextureFormat.ARGBHalf);
			stencil.filterMode = FilterMode.Bilinear;
			RenderTexture highres = RenderTexture.GetTemporary(_fullCloudTexture.width, _fullCloudTexture.height, 0, RenderTextureFormat.ARGBHalf);
			highres.filterMode = FilterMode.Bilinear;
			RenderTexture output = RenderTexture.GetTemporary(_fullCloudTexture.width, _fullCloudTexture.height, 0, RenderTextureFormat.ARGBHalf);
			output.filterMode = FilterMode.Bilinear;

			// write to the stencil
			Graphics.Blit(source, stencil, _volMaterial, 1);
			// write to the screen using the stencil
			_volMaterial.SetInt("_UseDepthTexture", 1);
			Graphics.Blit(stencil, highres, _volMaterial, 2);

			// render the upsampled clouds into _fullCloudTexture
			_volMaterial.SetInt("_UseDepthTexture", 0);
			UpsampleRender(source, destination);

			// set the upsampled clouds as the background texture for the combine pass
			Graphics.Blit(_fullCloudTexture, output);
			_blitCombineMaterial.SetTexture("_Source", output);
			_blitCombineMaterial.SetTexture("_Stencil", stencil);

			Graphics.Blit(highres, _fullCloudTexture, _blitCombineMaterial, 1);

			if (_profile._drawStencil)
			{
				// set the completed clouds as the background texture for the combine pass
				Graphics.Blit(_fullCloudTexture, output);
				_blitCombineMaterial.SetTexture("_Source", output);

				Graphics.Blit(stencil, _fullCloudTexture, _blitCombineMaterial, 2);
			}

			RenderTexture.ReleaseTemporary(stencil);
			RenderTexture.ReleaseTemporary(highres);
			RenderTexture.ReleaseTemporary(output);

		}
		else
		{

			_volMaterial.SetInt("_UseDepthTexture", 1);
			UpsampleRender(source, destination);

		}

		FinalizeRender(source, destination);

	}


	void UpsampleRender(RenderTexture source, RenderTexture destination)
	{
		if (_profile._resolution == CloudSamplingProfile.Resolution.QUARTER)
		{
			Graphics.Blit(_quarterDepthBuffer, _quarterCloudTexture, _volMaterial, 0);

			// upscale to full res
			Graphics.Blit(_quarterCloudTexture, _fullCloudTexture, _bilateralBlurMaterial, 7);
		}
		else if (_profile._resolution == CloudSamplingProfile.Resolution.HALF)
		{
			Graphics.Blit(_halfDepthBuffer, _halfCloudTexture, _volMaterial, 0);

			// upscale to full res
			Graphics.Blit(_halfCloudTexture, _fullCloudTexture, _bilateralBlurMaterial, 5);
		}
		else
		{
			Graphics.Blit(_fullDepthBuffer, _fullCloudTexture, _volMaterial, 0);
		}
	}


	
	void FinalizeRender(RenderTexture source, RenderTexture destination)
	{
		_blitCombineMaterial.SetTexture("_Source", source);
		Graphics.Blit(_fullCloudTexture, destination, _blitCombineMaterial, 0);
	}



	void ChangeResolution()
	{
		int width = _camera.pixelWidth;
		int height = _camera.pixelHeight;

		if (_fullCloudTexture != null)
			DestroyImmediate(_fullCloudTexture);

		_fullCloudTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBHalf);
		_fullCloudTexture.name = "CloudRenderBuffer";
		_fullCloudTexture.filterMode = FilterMode.Bilinear;


		if (_halfCloudTexture != null)
			DestroyImmediate(_halfCloudTexture);
		if (_halfDepthBuffer != null)
			DestroyImmediate(_halfDepthBuffer);

		if (_profile._resolution == CloudSamplingProfile.Resolution.HALF || _profile._resolution == CloudSamplingProfile.Resolution.QUARTER)
		{
			_halfCloudTexture = new RenderTexture(width / 2, height / 2, 0, RenderTextureFormat.ARGBHalf);
			_halfCloudTexture.name = "CloudRenderBufferHalf";
			_halfCloudTexture.filterMode = FilterMode.Bilinear;

			_halfDepthBuffer = new RenderTexture(width / 2, height / 2, 0, RenderTextureFormat.ARGBHalf);
			_halfDepthBuffer.name = "CloudDepthBufferHalf";
			_halfDepthBuffer.Create();
			_halfDepthBuffer.filterMode = FilterMode.Point;
		}


		if (_quarterCloudTexture != null)
			DestroyImmediate(_quarterCloudTexture);
		if (_quarterDepthBuffer != null)
			DestroyImmediate(_quarterDepthBuffer);

		if (_profile._resolution == CloudSamplingProfile.Resolution.QUARTER)
		{
			_quarterCloudTexture = new RenderTexture(width / 4, height / 4, 0, RenderTextureFormat.ARGBHalf);
			_quarterCloudTexture.name = "CloudRenderBufferQuarter";
			_quarterCloudTexture.filterMode = FilterMode.Bilinear;

			_quarterDepthBuffer = new RenderTexture(width / 4, height / 4, 0, RenderTextureFormat.ARGBHalf);
			_quarterDepthBuffer.name = "CloudDepthBufferQuarter";
			_quarterDepthBuffer.Create();
			_quarterDepthBuffer.filterMode = FilterMode.Point;
		}
	}



	public void OnPreRender()
	{

		/*_preDepthPass.Clear();

		bool dx11 = SystemInfo.graphicsShaderLevel > 40;

		if (_resolution == Resolution.QUARTER)
		{
			Texture nullTexture = null;
			// downsample to half first
			_preDepthPass.Blit(nullTexture, _halfDepthBuffer, _bilateralBlurMaterial, dx11 ? 4 : 10);
			// now downsample to quarter
			_preDepthPass.Blit(nullTexture, _quarterDepthBuffer,
				_bilateralBlurMaterial, dx11 ? 6 : 11);

			_preDepthPass.SetRenderTarget(_quarterCloudTexture);
		}
		else if (_resolution == Resolution.HALF)
		{
			Texture nullTexture = null;
			// downsample to half
			_preDepthPass.Blit(nullTexture, _halfDepthBuffer, _bilateralBlurMaterial, dx11 ? 4 : 10);

			_preDepthPass.SetRenderTarget(_halfCloudTexture);
		}
		else
		{
			_preDepthPass.SetRenderTarget(_fullCloudTexture);
		}

		_preDepthPass.ClearRenderTarget(false, true, new Color(0, 0, 0, 1));*/

		UpdateMaterialParameters();
	}


	private void UpdateMaterialParameters()
	{
		_bilateralBlurMaterial.SetTexture("_HalfResDepthBuffer", _halfDepthBuffer);
		_bilateralBlurMaterial.SetTexture("_HalfResColor", _halfCloudTexture);
		_bilateralBlurMaterial.SetTexture("_QuarterResDepthBuffer", _quarterDepthBuffer);
		_bilateralBlurMaterial.SetTexture("_QuarterResColor", _quarterCloudTexture);
	}



	static float halfFov_vert_rad  { get { return Camera.main.fieldOfView * Mathf.Deg2Rad / 2.0f; } }

    static float halfFov_horiz_rad { get { return Mathf.Atan(Mathf.Tan(halfFov_vert_rad) * Camera.main.aspect); } }


	/// \brief Stores the normalized rays representing the camera frustum in a 4x4 matrix.  Each row is a vector.
	/// 
	/// The following rays are stored in each row (in eyespace, not worldspace):
	/// Top Left corner:     row=0
	/// Top Right corner:    row=1
	/// Bottom Right corner: row=2
	/// Bottom Left corner:  row=3
	private Matrix4x4 GetFrustumCorners(Camera cam)
	{
		float camFov = cam.fieldOfView;
		float camAspect = cam.aspect;

		Matrix4x4 frustumCorners = Matrix4x4.identity;

		float fovWHalf = camFov * 0.5f;

		float tan_fov = Mathf.Tan(fovWHalf * Mathf.Deg2Rad);

		Vector3 toRight = Vector3.right * tan_fov * camAspect;
		Vector3 toTop = Vector3.up * tan_fov;

		Vector3 topLeft = (-Vector3.forward - toRight + toTop);
		Vector3 topRight = (-Vector3.forward + toRight + toTop);
		Vector3 bottomRight = (-Vector3.forward + toRight - toTop);
		Vector3 bottomLeft = (-Vector3.forward - toRight - toTop);

		frustumCorners.SetRow(0, topLeft);
		frustumCorners.SetRow(1, topRight);
		frustumCorners.SetRow(2, bottomRight);
		frustumCorners.SetRow(3, bottomLeft);

		return frustumCorners;
	}

	/// \brief Custom version of Graphics.Blit that encodes frustum corner indices into the input vertices.
	/// 
	/// In a shader you can expect the following frustum cornder index information to get passed to the z coordinate:
	/// Top Left vertex:     z=0, u=0, v=0
	/// Top Right vertex:    z=1, u=1, v=0
	/// Bottom Right vertex: z=2, u=1, v=1
	/// Bottom Left vertex:  z=3, u=1, v=0
	/// 
	/// \warning You may need to account for flipped UVs on DirectX machines due to differing UV semantics
	///          between OpenGL and DirectX.  Use the shader define UNITY_UV_STARTS_AT_TOP to account for this.
	static void CustomGraphicsBlit(RenderTexture source, RenderTexture dest, Material fxMaterial, int passNr)
	{
		RenderTexture.active = dest;

		fxMaterial.SetTexture("_MainTex", source);

		GL.PushMatrix();
		GL.LoadOrtho(); // Note: z value of vertices don't make a difference because we are using ortho projection

		fxMaterial.SetPass(passNr);

		GL.Begin(GL.QUADS);

		// Here, GL.MultitexCoord2(0, x, y) assigns the value (x, y) to the TEXCOORD0 slot in the shader.
		// GL.Vertex3(x,y,z) queues up a vertex at position (x, y, z) to be drawn.  Note that we are storing
		// our own custom frustum information in the z coordinate.
		GL.Vertex3(0.0f, 0.0f, 3.0f); // BL

		GL.Vertex3(1.0f, 0.0f, 2.0f); // BR

		GL.Vertex3(1.0f, 1.0f, 1.0f); // TR

		GL.Vertex3(0.0f, 1.0f, 0.0f); // TL

		GL.End();
		GL.PopMatrix();
	}



	// Not using this.

	public override bool CheckResources() { return true; }

}

