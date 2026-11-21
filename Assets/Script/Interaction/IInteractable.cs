using UnityEngine;

/// <summary>
/// 可以按 E 互動的東西 (存檔點、場景道具、之後的機關/門…)。
/// 物件需要有 isTrigger 的 Collider2D，玩家走進範圍就會被 PlayerInteractor 偵測到。
/// </summary>
public interface IInteractable
{
    /// <summary>頭上顯示的提示文字；回傳 null = 目前不顯示也不能互動。</summary>
    string GetPrompt(PlayerInteractor who);

    /// <summary>提示框的世界座標。</summary>
    Vector3 PromptPosition { get; }

    void Interact(PlayerInteractor who);
}
