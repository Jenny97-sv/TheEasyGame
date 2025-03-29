using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class UIBackgoundSize : MonoBehaviour
{
    private RectTransform image = null;
    void Start()
    {
        image = GetComponent<RectTransform>();
        image.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Screen.width);
        image.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Screen.height);
    }

    public void Hide()
    {
        image.gameObject.SetActive(false);
    }
}
