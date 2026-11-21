using UnityEngine;

/// <summary>
/// 通關點：玩家站在範圍內按 E → GameManager.ClearGame (一周目 → 進入二周目；二周目 → 顯示「通關」)。
/// 需要一個 isTrigger 的 Collider2D 當互動範圍 (跟拉桿一樣)。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ClearGoal : MonoBehaviour, IInteractable
{
    public Vector3 PromptPosition => transform.position + Vector3.up * 1.5f;

    public string GetPrompt(PlayerInteractor who) => "E 通關";

    public void Interact(PlayerInteractor who) => GameManager.Instance.ClearGame();
}
