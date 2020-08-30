using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.IO;
using NUI;
using System;

public class TextSample : MonoBehaviour {
    private NText text;
    private NInputField input;

	// Use this for initialization
	void Start () {
        text = transform.Find("NText").GetComponent<NText>();
        input = transform.Find("NInputField").GetComponent<NInputField>();
        transform.Find("ButtonInsertText").GetComponent<Button>().onClick.AddListener(OnBtnInsertText);
        transform.Find("ButtonInsertLink").GetComponent<Button>().onClick.AddListener(OnBtnInsertLink);
        transform.Find("ButtonInsertImage").GetComponent<Button>().onClick.AddListener(OnBtnInsertImage);
        transform.Find("ButtonSend").GetComponent<Button>().onClick.AddListener(OnBtnSend);
        text.OnLink = OnLink;
    }

    void OnBtnInsertText()
    {
        input.InsertLink("insert text", input.caretPosition, null);
    }

    void OnBtnInsertLink()
    {
        input.InsertLink("insert link", input.caretPosition, "param2", underlineColor:Color.red);
    }

    void OnBtnInsertImage()
    {
        input.InsertImage(0, input.caretPosition, animLength: 5);
    }

    void OnBtnSend()
    {
        var htmlText = input.HtmlText;
        text.text += htmlText;
    }

    void OnLink(string param)
    {
        Debug.Log("超链接:" + param);
    }

    static int Int32Parse(string text)
    {
        int result = 0;
        if (Int32.TryParse(text, out result))
            return result;
        return 0;
    }

    void OnBtnAsset()
    {
#if UNITY_EDITOR
        AssetDatabase.CreateAsset(new NTextSpritePackage(), "Assets/TextSpritePackage.asset");
#endif
    }
}
