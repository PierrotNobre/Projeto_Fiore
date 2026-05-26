using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class UIButtonEntry : MonoBehaviour
{
    [SerializeField]
    private TMP_Text label;

    [SerializeField]
    private Button button;

    public void Setup(
        string text,
        Action onClick)
    {
        label.text = text;

        button.onClick.RemoveAllListeners();

        button.onClick.AddListener(
            () => onClick?.Invoke()
        );
    }
}