using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 物品欄上方的道具狀態區：
/// - 一行提示文字：切換格子時顯示道具名稱、道具失敗原因 (ItemStatusUI.Hint("..."))。新訊息蓋掉舊的。
/// - 火箭藍條：飛行中顯示剩餘時間 (蓄力中不顯示)。
/// </summary>
public class ItemStatusUI : MonoBehaviour
{
    static ItemStatusUI instance;

    [SerializeField] Inventory inventory;
    [SerializeField] PlayerRocket rocket;

    [Header("提示文字")]
    [SerializeField] CanvasGroup hintGroup;
    [SerializeField] Text hintLabel;
    [SerializeField] float showTime = 1.2f;
    [SerializeField] float fadeTime = 0.3f;

    [Header("火箭藍條")]
    [SerializeField] GameObject barRoot;
    [Tooltip("填充條：用 anchorMax.x 控制長度 (anchorMin.x 要是 0)")]
    [SerializeField] RectTransform barFill;

    Coroutine hintRoutine;
    ItemInstance lastSelected;

    void Awake()
    {
        instance = this;
        hintGroup.alpha = 0f;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    public static void Hint(string text)
    {
        if (instance != null) instance.ShowHint(text);
    }

    void Update()
    {
        // 切換到不同道具 → 顯示名稱 (空格不顯示)
        var selected = inventory.SelectedItem;
        if (Time.timeSinceLevelLoad < 0.2f) lastSelected = selected; // 開場讀檔時不顯示名稱
        if (selected != lastSelected)
        {
            lastSelected = selected;
            if (selected != null) ShowHint(selected.definition.displayName);
        }

        // 火箭藍條 (只在飛行中顯示；蓄力中不顯示)
        bool show = rocket != null && rocket.State == PlayerRocket.RocketState.Flying;
        barRoot.SetActive(show);
        if (show) barFill.anchorMax = new Vector2(rocket.Fill, barFill.anchorMax.y);
    }

    void ShowHint(string text)
    {
        hintLabel.text = text;
        if (hintRoutine != null) StopCoroutine(hintRoutine);
        hintRoutine = StartCoroutine(RunHint());
    }

    IEnumerator RunHint()
    {
        hintGroup.alpha = 1f;
        yield return new WaitForSecondsRealtime(showTime);
        for (float t = 0f; t < fadeTime; t += Time.unscaledDeltaTime)
        {
            hintGroup.alpha = 1f - t / fadeTime;
            yield return null;
        }
        hintGroup.alpha = 0f;
    }
}
