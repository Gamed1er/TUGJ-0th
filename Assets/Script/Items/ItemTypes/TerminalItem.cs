using UnityEngine;

/// <summary>
/// 電腦 (工作列最左邊那格)：選中後右鍵打開終端機 (跟 M 鍵一樣)。
/// 固定在第 1 格 (Pinned)，不能拖曳交換；沒有次數、永遠可用。圖示畫在工作列底圖上，所以 icon 留空。
/// </summary>
[CreateAssetMenu(menuName = "Items/Terminal")]
public class TerminalItem : ItemDefinition
{
    public override bool ShowUses => false;
    public override bool IsAvailable(ItemInstance item) => true;
    public override bool Pinned => true;

    public override void OnUseStart(ItemUseContext ctx)
    {
        var terminal = Object.FindFirstObjectByType<TerminalUI>();
        if (terminal != null) terminal.SetOpen(true);
    }
}
