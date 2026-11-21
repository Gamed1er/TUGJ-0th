using UnityEngine;
using UnityEngine.UI;

/// <summary>地圖上的一個存檔點節點。去過 = 亮色並顯示名稱；沒去過 = 灰色「???」。</summary>
public class MapNodeUI : MonoBehaviour
{
    [SerializeField] Image dot;
    [SerializeField] Text label;
    [SerializeField] Color undiscoveredColor = new Color(0.4f, 0.4f, 0.4f);
    [SerializeField] Color discoveredColor = new Color(0.3f, 0.8f, 1f);
    [SerializeField] Color currentColor = new Color(1f, 0.85f, 0.2f);
    [SerializeField] Color disabledColor = new Color(0.25f, 0.25f, 0.25f);

    SavePoint savePoint;

    public RectTransform RectTransform => (RectTransform)transform;

    public void Bind(SavePoint sp) => savePoint = sp;

    public void Refresh(string currentSavePointId)
    {
        bool discovered = savePoint.Discovered;
        label.text = discovered ? savePoint.DisplayName : "???";
        dot.color = !savePoint.IsActive ? disabledColor
                  : savePoint.Id == currentSavePointId ? currentColor
                  : discovered ? discoveredColor
                  : undiscoveredColor;
    }
}
