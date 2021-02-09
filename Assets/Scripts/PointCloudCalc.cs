using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using OpenCvSharp;
using TMPro;

public class PointCloudCalc : MonoBehaviour
{
	public float pointSize = 0.05f;
	public Color tintColor;
	public float scale = 1.0f;

	public bool filesLoaded = false;

	[SerializeField] Shader _pointShader = null;

	[Space]
	public TMP_InputField focalLengthX;
	public TMP_InputField focalLengthY;
	public TMP_InputField principalPointX;
	public TMP_InputField principalPointY;
	public TMP_InputField translationX;
	public TMP_InputField translationY;
	public TMP_InputField translationZ;
	public TMP_InputField rotationX;
	public TMP_InputField rotationY;
	public TMP_InputField rotationZ;
	[Space]

	// private Texture mRGBImage;
	private Texture2D mRGBImage;
	private Texture2D mDispImage;
	private ParticleSystem.Particle[] mPointCloud;
	private ComputeBuffer pointBuffer, sourceBuffer;
	private int pixelCount;
	private int imageHeight;
	private int imageWidth;
	public const int elementSize = sizeof(float) * 4;

	private CameraIntrinsics cameraIntrinsics;
	private CameraExtrinsics cameraExtrinsics;

	private Mat mDispMap;
	private Mat mRGBMap;

	private int mNumOfChannelsDepth = 1;
	private MatType mMatTypeDepth;

	private bool calculateFocalLength = false;
	private bool calculatePrinciplePoint = false;

	struct Point
	{
		public Vector3 position;
		public uint color;
	}

	[SerializeField] Point[] _pointData;

	Material _pointMaterial;
	Material _diskMaterial;


	void Start()
	{
		// mRGBImage = new Texture2D(2, 2);
		// mDispImage = new Texture2D(2, 2);
		cameraExtrinsics = new CameraExtrinsics();
		cameraIntrinsics = new CameraIntrinsics();
		focalLengthX.text = cameraIntrinsics.FX.ToString();
		focalLengthY.text = cameraIntrinsics.FY.ToString();
		principalPointX.text = cameraIntrinsics.X0.ToString();
		principalPointY.text = cameraIntrinsics.Y0.ToString();
		translationX.text = cameraExtrinsics.X.ToString();
		translationY.text = cameraExtrinsics.Y.ToString();
		translationZ.text = cameraExtrinsics.Z.ToString();
		rotationX.text = cameraExtrinsics.XR.ToString();
		rotationY.text = cameraExtrinsics.YR.ToString();
		rotationZ.text = cameraExtrinsics.ZR.ToString();

		translationX.onEndEdit.AddListener(delegate { SetPositionX(translationX.text); });
		translationY.onEndEdit.AddListener(delegate { SetPositionY(translationY.text); });
		translationZ.onEndEdit.AddListener(delegate { SetPositionZ(translationZ.text); });
		rotationX.onEndEdit.AddListener(delegate { SetRotationX(rotationX.text); });
		rotationY.onEndEdit.AddListener(delegate { SetRotationY(rotationY.text); });
		rotationZ.onEndEdit.AddListener(delegate { SetRotationZ(rotationZ.text); });

		focalLengthX.onEndEdit.AddListener(delegate { SetFocalX(focalLengthX.text); });
		focalLengthY.onEndEdit.AddListener(delegate { SetFocalY(focalLengthY.text); });

		principalPointX.onEndEdit.AddListener(delegate { SetPPointX(principalPointX.text); });
		principalPointY.onEndEdit.AddListener(delegate { SetPPointY(principalPointY.text); });

		if (_pointMaterial == null)
		{
			_pointMaterial = new Material(_pointShader);
			_pointMaterial.hideFlags = HideFlags.DontSave;
			_pointMaterial.EnableKeyword("_COMPUTE_BUFFER");
		}
	}

	void OnDestroy()
	{
		if (_pointMaterial != null)
		{
			if (Application.isPlaying)
			{
				Destroy(_pointMaterial);
				Destroy(_diskMaterial);
			}
			else
			{
				DestroyImmediate(_pointMaterial);
				DestroyImmediate(_diskMaterial);
			}
		}
		if(pointBuffer != null)
			pointBuffer.Release();
		if(sourceBuffer != null)
			sourceBuffer.Release();
	}


	void OnRenderObject()
	{
		if (false == filesLoaded)
			return;
		
		pointBuffer = sourceBuffer;
		_pointMaterial.SetPass(0);
		// _pointMaterial.SetColor("_Tint", tintColor);
		_pointMaterial.SetMatrix("_Transform", transform.localToWorldMatrix);
		_pointMaterial.SetBuffer("_PointBuffer", pointBuffer);
		_pointMaterial.SetFloat("_PointSize", pointSize);
#if UNITY_2019_1_OR_NEWER
		Graphics.DrawProceduralNow(MeshTopology.Points, pointBuffer.count, 1);
#else
		Graphics.DrawProcedural(MeshTopology.Points, pointBuffer.count, 1);
#endif
	}

	public void SetPositions()
	{
		int count = 0;
		_pointData = new Point[pixelCount];
		for (int i = 0; i < imageWidth; i++)
		{
			for (int j = 0; j < imageHeight; j++)
			{
				float dispValue = mDispImage.GetPixel(i, j).r;// / 257.0f;// *  257.0f;
				float z = 0.57f + cameraIntrinsics.FX / dispValue;
				float x = (i - cameraIntrinsics.X0) * z / cameraIntrinsics.FX;
				float y = (j - cameraIntrinsics.Y0) * z / cameraIntrinsics.FY;

				Vector3 coords =  cameraExtrinsics.cameraPose.MultiplyPoint(new Vector3(x, y, z));

				_pointData[count] = new Point
				{
					position = coords,
					color = EncodeColor(mRGBImage.GetPixel(i, j))
				};
				count++;
			}
		}
		sourceBuffer = new ComputeBuffer(pixelCount, elementSize);
		sourceBuffer.SetData(_pointData);
	}

	public void SetPosCV()
	{
		int count = 0;
		float dispValue = 0;
		_pointData = new Point[pixelCount];
		for (int j = 0; j < imageHeight; j++)
		{
			for (int i = 0; i < imageWidth; i++)
			{
				if (mMatTypeDepth == MatType.CV_8UC4)
					dispValue = mDispMap.Get<Vec4b>(j, i).Item0;
				else if (mMatTypeDepth == MatType.CV_16UC1)
					dispValue = mDispMap.Get<short>(j, i);
				else if (mMatTypeDepth == MatType.CV_32FC1)
					dispValue = 1 / mDispMap.Get<float>(j, i) * 10000f;

				float z = 0.57f + cameraIntrinsics.FX / dispValue;
				float x = (i - cameraIntrinsics.X0) * z / cameraIntrinsics.FX;
				float y = (j - cameraIntrinsics.Y0) * z / cameraIntrinsics.FY;

				Vector3 coords = cameraExtrinsics.cameraPose.MultiplyPoint(new Vector3(x, -y, z) * 100f);

				_pointData[count] = new Point
				{
					position = coords,
					color = EncodeColorCV(mRGBMap.Get<Vec3b>(j, i))
				};
				count++;
			}
		}
		sourceBuffer = new ComputeBuffer(pixelCount, elementSize);
		sourceBuffer.SetData(_pointData);
	}

	static uint EncodeColorCV(Vec3b col)
	{
		const float kMaxBrightness = 16;
		var y = (float)Mathf.Max(Mathf.Max(col.Item2, col.Item1), col.Item0);
		y = Mathf.Clamp(Mathf.Ceil(y / kMaxBrightness), 1, 255);
		col *= 255 / (y * kMaxBrightness);
		return ((uint)col.Item2) |
			   ((uint)col.Item1 << 8) |
			   ((uint)col.Item0 << 16) |
			   ((uint)y << 24);
	}

	static uint EncodeColor(Color c)
	{
		const float kMaxBrightness = 16;

		var y = Mathf.Max(Mathf.Max(c.r, c.g), c.b);
		y = Mathf.Clamp(Mathf.Ceil(y * 255 / kMaxBrightness), 1, 255);

		var rgb = new Vector3(c.r, c.g, c.b);
		rgb *= 255 * 255 / (y * kMaxBrightness);

		return ((uint)rgb.x) |
			   ((uint)rgb.y << 8) |
			   ((uint)rgb.z << 16) |
			   ((uint)y << 24);
	}

	public void LoadRGBImage(string path)
	{
		// mRGBImage.LoadImage(File.ReadAllBytes(path));
		mRGBMap = Cv2.ImRead(path);
		imageHeight = mRGBMap.Size().Height;
		imageWidth = mRGBMap.Size().Width;
		pixelCount = imageWidth * imageHeight;

		if(calculateFocalLength)
		{
			CalculateFocalLength();
		}
		if(calculatePrinciplePoint)
		{
			CalculatePrincipalPoint();
		}

		cameraIntrinsics.imageHeight = imageHeight;
		cameraIntrinsics.imageWidth = imageWidth;

		cameraExtrinsics.SetRotation(0, 0, 0);
		cameraExtrinsics.SetTransform(0, 0, 0);
	}

	public void SetPPointX(string value)
	{
		if (!calculatePrinciplePoint)
			cameraIntrinsics.X0 = float.Parse(value);
		SetPosCV();
	}
	public void SetPPointY(string value)
	{
		if (!calculatePrinciplePoint)
			cameraIntrinsics.Y0 = float.Parse(value);
		SetPosCV();
	}

	public void SetFocalX(string value)
	{
		if (!calculateFocalLength)
			cameraIntrinsics.FX = float.Parse(value);
		SetPosCV();
	}

	public void SetFocalY(string value)
	{
		if (!calculateFocalLength)
			cameraIntrinsics.FY = float.Parse(value);
		SetPosCV();
	}

	public void ToggleFocalLengthCalc(bool value)
	{
		calculateFocalLength = value;
		if(false == calculateFocalLength)
		{
			focalLengthX.interactable = true;
			focalLengthY.interactable = true;
			cameraIntrinsics.FX = float.Parse(focalLengthX.text);
			cameraIntrinsics.FY = float.Parse(focalLengthY.text);
		}
		else
		{
			focalLengthX.interactable = false;
			focalLengthY.interactable = false;
			CalculateFocalLength();
		}
		SetPosCV();
	}

	private void CalculateFocalLength()
	{
		if(imageHeight != 0)
		{
			cameraIntrinsics.SetFocalLength(imageHeight / 2, imageHeight / 2);
			focalLengthX.text = cameraIntrinsics.FX.ToString();
			focalLengthY.text = cameraIntrinsics.FY.ToString();
		}
		SetPosCV();
	}

	public void TogglePrinciplePointCalc(bool value)
	{
		calculatePrinciplePoint = value;
		if (false == calculatePrinciplePoint)
		{
			principalPointX.interactable = true;
			principalPointY.interactable = true;
			cameraIntrinsics.X0 = float.Parse(principalPointX.text);
			cameraIntrinsics.Y0 = float.Parse(principalPointY.text);
		}
		else
		{
			principalPointX.interactable = false;
			principalPointY.interactable = false;
			CalculatePrincipalPoint();
		}
		SetPosCV();
	}
	private void CalculatePrincipalPoint()
	{
		if (imageHeight != 0)
		{
			cameraIntrinsics.SetPrincipalPoint(imageWidth / 2, imageHeight / 2);
			principalPointX.text = cameraIntrinsics.X0.ToString();
			principalPointY.text = cameraIntrinsics.Y0.ToString();
		}
		SetPosCV();
	}

	public void SetPositionX(string x)
	{
		cameraExtrinsics.X = float.Parse(x);
		SetPosCV();
	}

	public void SetPositionY(string y)
	{
		cameraExtrinsics.Y = float.Parse(y);
		SetPosCV();
	}

	public void SetPositionZ(string z)
	{
		cameraExtrinsics.Z = float.Parse(z);
		SetPosCV();
	}

	public void SetRotationX(string x)
	{
		cameraExtrinsics.XR = float.Parse(x);
		SetPosCV();
	}

	public void SetRotationY(string y)
	{
		cameraExtrinsics.YR = float.Parse(y);
		SetPosCV();
	}

	public void SetRotationZ(string z)
	{
		cameraExtrinsics.ZR = float.Parse(z);
		SetPosCV();
	}

	public void LoadDispImage(string path)
	{
		mDispMap = Cv2.ImRead(path, ImreadModes.Unchanged);
		mNumOfChannelsDepth = mDispMap.Channels();
		mMatTypeDepth = mDispMap.Type();
	}
}
