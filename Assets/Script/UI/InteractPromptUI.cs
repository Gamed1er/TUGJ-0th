using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 互動提示框 (例如「E 存檔」)，跟著世界座標顯示在畫面上。場景中放一個即可。
/// 用 InteractPromptUI.Show(...) / Hide() 呼叫。
/// </summary>
public class InteractPromptUI : MonoBehaviour
{
    static InteractPromptUI instance;

    [SerializeField] GameObject root;
    [SerializeField] Text label;

    Vector3 worldPos;

    void Awake()
    {
        instance = this;
        root.SetActive(false);
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    public static void Show(string text, Vector3 worldPosition)
    {
        if (instance == null) return;
        instance.worldPos = worldPosition;
        instance.label.text = text;
        instance.root.SetActive(true);
        instance.UpdatePosition();
    }

    public static void Hide()
    {
        if (instance != null) instance.root.SetActive(false);
    }

    void LateUpdate()
    {
        if (root.activeSelf) UpdatePosition();
    }

    void UpdatePosition()
    {
        var cam = Camera.main;
        if (cam != null) root.transform.position = cam.WorldToScreenPoint(worldPos);
    }
}
