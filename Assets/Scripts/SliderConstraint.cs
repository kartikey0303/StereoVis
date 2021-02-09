using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class SliderConstraint : MonoBehaviour
{
    public RectTransform targetTransform;
    public float offset = 0.0f;
    private RectTransform currentRect;
    private Canvas canvas;
    private Vector2 mMinMaxDim;
    // Start is called before the first frame update
    void Start()
    {
        currentRect = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        mMinMaxDim.x = Screen.width / canvas.scaleFactor - offset;
        mMinMaxDim.y = Screen.width / canvas.scaleFactor - targetTransform.sizeDelta.x - offset;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        currentRect.sizeDelta = new Vector2(Mathf.Clamp(Screen.width / canvas.scaleFactor - targetTransform.sizeDelta.x - targetTransform.anchoredPosition.x - offset, mMinMaxDim.y, mMinMaxDim.x), 20);
    }
}
