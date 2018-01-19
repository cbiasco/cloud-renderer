//The MIT License(MIT)

//Copyright(c) 2018 Caleb Biasco

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



using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour {

	const KeyCode left = KeyCode.A;
	const KeyCode right = KeyCode.D;
	const KeyCode forward = KeyCode.W;
	const KeyCode backward = KeyCode.S;
	const KeyCode up = KeyCode.E;
	const KeyCode down = KeyCode.Q;

	[SerializeField]
	float speed = 1f;

	[Space]

	float rotationX = 0f;
	float rotationY = 0f;

	[SerializeField]
	float sensitivityX = 3f;
	[SerializeField]
	float sensitivityY = 3f;

	[SerializeField, Range(-360f, 360f)]
	float maxRotationY = 60f;
	[SerializeField, Range(-360f, 360f)]
	float minRotationY = -60f;

	Quaternion initialRotation;

	void Start ()
	{
		initialRotation = Quaternion.identity;

		if (minRotationY > maxRotationY)
		{
			Debug.Log("[CameraMovement] ERROR: minRotationY must be less than maxRotationY.");
		}
	}
	
	// Update is called once per frame
	void Update () {
		// ROTATION
		if (Input.GetMouseButton(0))
		{
			rotationX += Input.GetAxis("Mouse X") * sensitivityX;
			rotationY += Input.GetAxis("Mouse Y") * sensitivityY;

			rotationY = Mathf.Clamp(rotationY, minRotationY, maxRotationY);

			Quaternion xQuaternion = Quaternion.AngleAxis(rotationX, Vector3.up);
			Quaternion yQuaternion = Quaternion.AngleAxis(rotationY, -Vector3.right);

			transform.localRotation = initialRotation * xQuaternion * yQuaternion;
		}

		// TRANSLATION
		Vector3 translation = Vector3.zero;
		translation.x -= (Input.GetKey(left)) ? 1f : 0f;
		translation.x += (Input.GetKey(right)) ? 1f : 0f;
		translation.y -= (Input.GetKey(down)) ? 1f : 0f;
		translation.y += (Input.GetKey(up)) ? 1f : 0f;
		translation.z -= (Input.GetKey(backward)) ? 1f : 0f;
		translation.z += (Input.GetKey(forward)) ? 1f : 0f;

		translation.Normalize();
		translation *= speed;

		transform.position += transform.rotation * translation;
	}
}
