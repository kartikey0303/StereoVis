using UnityEngine;
using System.IO;
using SFB;
using TMPro;
using UnityEngine.UI;

public class FileLoader : MonoBehaviour
{
    public TMP_InputField filePathInputField;
    public Slider playBackSlider;
    public Image playPauseButton;

    [SerializeField]
    private Sprite playSprite;
    [SerializeField]
    private Sprite pauseSprite;

    private PointCloudCalc mPointCloudCalc;
    private int currentFrame = 0;
    private int totalFrames = 0;

    private string[] mRGBFilePaths;
    private string[] mDepthFilePaths;

    private bool mIsPlaying;
    public bool isPlaying
    {
        get
        {
            return mIsPlaying;
        }
        set
        {
            mIsPlaying = value;
            SetButtonIcon();
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        mPointCloudCalc = FindObjectOfType<PointCloudCalc>();
    }

    void SetButtonIcon()
	{
        if (isPlaying)
            playPauseButton.sprite = pauseSprite;
        else
            playPauseButton.sprite = playSprite;
	}

    public void TogglePlay()
	{
        isPlaying = !isPlaying;
	}

	private void Update()
	{
		if(Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
		{
            LoadNextFrame();
		}
        else if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            LoadPreviousFrame();
        }

        if(isPlaying)
		{
            LoadNextFrame();
		}
    }

    private void LoadNextFrame()
	{
        currentFrame++;
        if(currentFrame < totalFrames)
		{
            LoadFilesAtIndex(currentFrame);
		}
		else
		{
            if (isPlaying)
                currentFrame = 0;
            else
                currentFrame--;
		}
	}

    private void LoadPreviousFrame()
	{
        currentFrame--;
        if (currentFrame >= 0)
        {
            LoadFilesAtIndex(currentFrame);
        }
        else
        {
            currentFrame = 0;
        }
    }

    public void SetIndex(float value)
	{
        int index = (int)(value * totalFrames);
        LoadFilesAtIndex(index);
	}

    private void LoadFilesAtIndex(int index)
	{
        mPointCloudCalc.LoadRGBImage(mRGBFilePaths[index]);
        mPointCloudCalc.LoadDispImage(mDepthFilePaths[index]);
        mPointCloudCalc.SetPosCV();
        mPointCloudCalc.filesLoaded = true;
        playBackSlider.SetValueWithoutNotify((float)index / totalFrames);
    }

	public void SetLoadingFolder(string path)
	{
        // TODO: Add try catch block here
        string rbgImagePath = Path.Combine(path, "images");
        string depthImagePath = Path.Combine(path, "depths");

        mRGBFilePaths = Directory.GetFiles(rbgImagePath);
        mDepthFilePaths = Directory.GetFiles(depthImagePath);
        currentFrame = 0;
        totalFrames = mRGBFilePaths.Length;
        isPlaying = false;
        LoadFilesAtIndex(0);
    }
    public void OnButtonClick()
    {
        var folderPath = StandaloneFileBrowser.OpenFolderPanel("Select Stereo Folder", "", false);
        SetLoadingFolder(folderPath[0]);
        filePathInputField.text = folderPath[0];
    }
}
