using UnityEngine;

/// <summary>
/// 道具的基底資料。每種道具 = 繼承這個 class，覆寫 OnUseStart (必要時加 OnUseHold / OnUseEnd)，
/// 然後在 Project 視窗右鍵 Create/Items/... 建立資產。
/// 資產必須放在 Assets/Resources/Items/ 底下，讀檔時才能用 id 找回來 (見 ItemRegistry)。
///
/// 右鍵的生命週期 (由 PlayerItemUser 呼叫)：
///   按下 → OnUseStart；按住每幀 → OnUseHold；放開 / 切換到別格 / 被鎖輸入 → OnUseEnd
/// 要不要扣次數由道具自己決定：成功時呼叫 ctx.Consume()。
/// </summary>
public abstract class ItemDefinition : ScriptableObject
{
    [Tooltip("存檔用的唯一 ID，留空會自動用檔名")]
    public string id;
    public string displayName = "道具";
    [TextArea] public string description;
    public Sprite icon;
    [Tooltip("開新遊戲時的可用次數")]
    [Min(0)] public int startingUses = 3;

    public abstract void OnUseStart(ItemUseContext ctx);
    public virtual void OnUseHold(ItemUseContext ctx) { }
    public virtual void OnUseEnd(ItemUseContext ctx) { }

    /// <summary>物品欄要不要顯示次數。</summary>
    public virtual bool ShowUses => true;

    /// <summary>目前能不能用 (false = 物品欄圖示變黑白)。預設 = 還有次數。</summary>
    public virtual bool IsAvailable(ItemInstance item) => item.uses > 0;

    /// <summary>
    /// 瞄準提示線 (AimLine 每幀呼叫)。要顯示就把路徑點 (世界座標) 填進 points 並回傳 true。
    /// highlight = true 時畫成綠色 (例如傳送槍會打到電池 = 可以傳送)。預設沒有提示線。
    /// </summary>
    public virtual bool GetAimPath(ItemUseContext ctx, System.Collections.Generic.List<Vector3> points, out bool highlight)
    {
        highlight = false;
        return false;
    }

    protected virtual void OnValidate()
    {
        if (string.IsNullOrEmpty(id)) id = name;
    }
}

/// <summary>使用道具時傳入的情境資訊。需要更多資訊時在這裡加欄位。</summary>
public struct ItemUseContext
{
    public PlayerController player;
    public Inventory inventory;
    /// <summary>正在使用的那一格道具 (含剩餘次數)。</summary>
    public ItemInstance item;
    /// <summary>滑鼠在世界中的位置 (瞄準用)。</summary>
    public Vector2 aimWorld;

    /// <summary>從玩家往滑鼠的方向 (單位向量)；滑鼠剛好在玩家身上時 = 面向的方向。</summary>
    public Vector2 AimDirection
    {
        get
        {
            Vector2 dir = aimWorld - player.Body.position;
            return dir.sqrMagnitude < 0.0001f ? Vector2.right * player.Facing : dir.normalized;
        }
    }

    /// <summary>還有次數就回傳 true；沒有就顯示「已用完」並回傳 false。</summary>
    public bool CheckUses()
    {
        if (item.uses > 0) return true;
        ItemStatusUI.Hint($"{item.definition.displayName} 已用完");
        return false;
    }

    /// <summary>扣 1 次。</summary>
    public void Consume() => inventory.Consume(item);
}
