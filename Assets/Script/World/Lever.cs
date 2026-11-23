using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 拉桿：玩家站在範圍內按 E (或無人機飛到旁邊按 E) → targets 裡的每個機關各自翻轉 (Activatable.Toggle)。
/// 不存檔：死亡 / 讀檔後回到初始狀態。需要一個 isTrigger 的 Collider2D 當互動範圍。
/// 連到拉桿的機關不要再接壓力板。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Lever : MonoBehaviour, IInteractable
{
    [FormerlySerializedAs("doors")]
    [SerializeField] Activatable[] targets;
    [SerializeField] AudioClip pullSfx;

    [Header("外觀 (美術可替換)")]
    [Tooltip("顯示拉桿圖片的 SpriteRenderer")]
    [SerializeField] SpriteRenderer visual;
    [SerializeField] Sprite offSprite;
    [SerializeField] Sprite onSprite;
    [SerializeField] Color offColor = new Color(0.8f, 0.6f, 0.2f);
    [SerializeField] Color onColor = new Color(0.3f, 1f, 0.4f);

    bool on;

    /// <summary>目前是不是拉開的狀態 (TrueEndGate 檢查條件用)。</summary>
    public bool On => on;

    public Vector3 PromptPosition => transform.position + Vector3.up * 1.5f;

    public string GetPrompt(PlayerInteractor who) => "E 拉桿";

    public void Interact(PlayerInteractor who) => Pull();

    /// <summary>切換拉桿 (玩家、無人機共用)。</summary>
    public void Pull()
    {
        on = !on;
        Sfx.Play(pullSfx);
        foreach (var t in targets) if (t != null) t.Toggle();
        RefreshVisual();
    }

    void Start() => RefreshVisual();

    void RefreshVisual()
    {
        if (visual == null) return;

        Sprite sprite = on ? onSprite : offSprite;
        if (sprite != null)
        {
            visual.sprite = sprite;
        }

        visual.color = on ? onColor : offColor;
    }

    void OnDrawGizmos()
    {
        if (targets == null) return;
        Gizmos.color = Color.yellow;
        foreach (var t in targets) if (t != null) Gizmos.DrawLine(transform.position, t.transform.position);
    }
}