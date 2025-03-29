using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class JoinCode : MonoBehaviour
{
    void Update()
    {
        TextMeshPro text = GetComponent<TextMeshPro>();
        if(text)
            text.text = Relay.Instance.m_JoinCode.text;
    }
}
