using UnityEngine;

public class CameraExtrinsics
{
	private float _X, _Y, _Z;
	private float _XR, _YR, _ZR;
	public float X
	{
		get => _X;
		set
		{
			_X = value;
			cameraPose[0, 3] = _X;
		}
	}
	public float Y
	{
		get => _Y;
		set
		{
			_Y = value;
			cameraPose[1, 3] = _Y;
		}
	}
	public float Z
	{
		get => _Z;
		set
		{
			_Z = value;
			cameraPose[2, 3] = _Z;
		}
	}

	public float XR
	{
		get => _XR;
		set
		{
			_XR = value;
			SetRotation(_XR, _YR, _ZR);
		}
	}
	public float YR
	{
		get => _YR;
		set
		{
			_YR = value;
			SetRotation(_XR, _YR, _ZR);
		}
	}
	public float ZR
	{
		get => _ZR;
		set
		{
			_ZR = value;
			SetRotation(_XR, _YR, _ZR);
		}
	}
	public Matrix4x4 cameraPose;
    public void SetRotation(float x, float y, float z)
	{
		// Compute rotation matrix
		float a = Mathf.Deg2Rad * x;
		float b = Mathf.Deg2Rad * y;
		float c = Mathf.Deg2Rad * z;

		float sinA = Mathf.Sin(a);
		float sinB = Mathf.Sin(b);
		float sinC = Mathf.Sin(c);

		float CosA = Mathf.Cos(a);
		float CosB = Mathf.Cos(b);
		float CosC = Mathf.Cos(c);

		cameraPose[0, 0] = CosC * CosB; cameraPose[0, 1] = -sinC * CosA + CosC * sinB * sinA; cameraPose[0, 2] = sinC * sinA + CosC * sinB * CosA;
		cameraPose[1, 0] = sinC * CosB; cameraPose[1, 1] = CosC * CosA + sinC * sinB * sinA; cameraPose[1, 2] = -CosC * sinA + sinC * sinB * CosA;
		cameraPose[2, 0] = -sinB; cameraPose[2, 1] = CosB * sinA; cameraPose[2, 2] = CosB * CosA;
		cameraPose[3, 0] = 0; cameraPose[3, 1] = 0; cameraPose[3, 2] = 0;
	}

	public void SetTransform(float x, float y, float z)
	{
		cameraPose[0, 3] = x;
		cameraPose[1, 3] = y;
		cameraPose[2, 3] = z;
		cameraPose[3, 3] = 1;
	}

	public CameraExtrinsics()
	{
		cameraPose = new Matrix4x4();
	}
}
