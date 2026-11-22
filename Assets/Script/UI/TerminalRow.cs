using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>終端機的一行文字。滑鼠移上去 = 選取、點擊 = 執行 (由 TerminalUI 建立與處理)。</summary>
[RequireComponent(typeof(Text))]
public class TerminalRow : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    public Text Text { get; private set; }

    /// <summary>滑鼠移上去。</summary>
    public Action Hovered;
    /// <summary>點擊，參數 = 點在這行的左半 (-1) 或右半 (+1)，調音量用。</summary>
    public Action<int> Clicked;

    void Awake() => Text = GetComponent<Text>();

    public void OnPointerEnter(PointerEventData e) => Hovered?.Invoke();

    public void OnPointerClick(PointerEventData e)
    {
        if (e.button != PointerEventData.InputButton.Left) return;
        var rt = (RectTransform)transform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, e.position, e.pressEventCamera, out var local);
        Clicked?.Invoke(local.x < rt.rect.center.x ? -1 : 1);
    }
}
