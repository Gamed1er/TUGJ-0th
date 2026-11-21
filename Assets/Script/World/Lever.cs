using UnityEngine;

/// <summary>
/// 拉桿：站在範圍內按 E → doors 裡的每扇門各自翻轉開關 (Door.Toggle)。
/// 不存檔：死亡 / 讀檔後回到初始狀態。需要一個 isTrigger 的 Collider2D 當互動範圍。
/// 連到拉桿的門不要再接壓力板。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Lever : MonoBehaviour, IInteractable
{
    [SerializeField] Door[] doors;

    [Header("外觀 (美術可替換)")]
    [Tooltip("拉桿的把手，拉下時左右翻轉")]
    [SerializeField] SpriteRenderer visual;
    [SerializeField] Color offColor = new Color(0.8f, 0.6f, 0.2f);
    [SerializeField] Color onColor = new Color(0.3f, 1f, 0.4f);

    bool on;

    public Vector3 PromptPosition => transform.position + Vector3.up * 1.5f;

    public string GetPrompt(PlayerInteractor who) => "E 拉桿";

    public void Interact(PlayerInteractor who)
    {
        on = !on;
        foreach (var d in doors) if (d != null) d.Toggle();
        RefreshVisual();
    }

    void Start() => RefreshVisual();

    void RefreshVisual()
    {
        if (visual == null) return;
        visual.flipX = on;
        visual.color = on ? onColor : offColor;
    }

    void OnDrawGizmos()
    {
        if (doors == null) return;
        Gizmos.color = Color.yellow;
        foreach (var d in doors) if (d != null) Gizmos.DrawLine(transform.position, d.transform.position);
    }
}
