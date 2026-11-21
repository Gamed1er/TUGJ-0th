using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 地圖上的一個節點。去過 = 亮色並顯示名稱；沒去過 = 灰色「???」；目前節點 = 黃色。
/// 可以點擊 (回溯用，由 MapUI 處理確認流程)；被選中時放大。
/// </summary>
public class MapNodeUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] Image dot;
    [SerializeField] Text label;
    [SerializeField] Color undiscoveredColor = new Color(0.4f, 0.4f, 0.4f);
    [SerializeField] Color discoveredColor = new Color(0.3f, 0.8f, 1f);
    [SerializeField] Color currentColor = new Color(1f, 0.85f, 0.2f);
    [SerializeField] Color disabledColor = new Color(0.25f, 0.25f, 0.25f);
    [SerializeField] float selectedScale = 1.5f;

    public SavePoint SavePoint { get; private set; }
    public RectTransform RectTransform => (RectTransform)transform;

    Action<MapNodeUI> onClick;

    public void Bind(SavePoint sp, Action<MapNodeUI> onClick)
    {
        SavePoint = sp;
        this.onClick = onClick;
    }

    /// <param name="hasRecord">這個節點還有紀錄 (可回溯)。去過但紀錄被覆蓋的節點顯示名稱、但用灰色。</param>
    public void Refresh(string currentSavePointId, bool selected, bool hasRecord)
    {
        bool discovered = SavePoint.Discovered;
        label.text = discovered ? SavePoint.DisplayName : "???";
        dot.color = !SavePoint.IsActive ? disabledColor
                  : SavePoint.Id == currentSavePointId ? currentColor
                  : discovered && hasRecord ? discoveredColor
                  : undiscoveredColor;
        transform.localScale = Vector3.one * (selected ? selectedScale : 1f);
    }

    public void OnPointerClick(PointerEventData e) => onClick?.Invoke(this);
}
