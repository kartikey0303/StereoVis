public class CameraIntrinsics
{
    private float _FX, _FY;
    private float _X0, _Y0;

    public float X0
	{
		get => _X0;
        set => _X0 = value;
	}

    public float Y0
	{
		get => _Y0;
		set => _Y0 = value;
	}

	public float FX
	{
		get => _FX;
		set => _FX = value;
	}

	public float FY
    {
        get => _FY;
		set => _FY = value;
    }

    public int imageHeight, imageWidth;

    public void SetPrincipalPoint(float _X0, float _Y0)
	{
        this._X0 = _X0;
        this._Y0 = _Y0;
	}

	public void SetFocalLength(float _FX, float _FY)
	{
        this._FX = _FX;
        this._FY = _FY;
	}

	public CameraIntrinsics()
	{
		_FX = 1000;
		_FY = 1000;
		_X0 = 500;
		_Y0 = 500;
	}
}
