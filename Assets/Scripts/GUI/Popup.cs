using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading;

public class Popup : MonoBehaviour
{
    [SerializeField]
    private static Popup _prefab;

    [SerializeField]
    private TMP_Text _text;
    [SerializeField]
    private Button _backdrop;
    [SerializeField]
    private Button _closeButton;
    private bool _canClose;

    public static void Create(string text, bool canClose = true)
    {
        _prefab.gameObject.SetActive(true);
        _prefab._text.text = text;
        _prefab._canClose = canClose;
    }


    void Start()
    {
        _prefab = this;
        DontDestroyOnLoad(gameObject);
        _backdrop.onClick.AddListener(OnClose);
        _closeButton.onClick.AddListener(OnClose);
        gameObject.SetActive(false);
    }

    private void OnClose()
    {
        if (_canClose)
            gameObject.SetActive(false);
    }
}
