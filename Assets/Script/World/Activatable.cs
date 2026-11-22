using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 可以被拉桿 / 壓力板控制的機關 (門、軌道平台) 的共用基底。
/// - 壓力板：任一塊板被壓著 → Active = true；全部放開 → false
/// - 拉桿：每拉一次 Active 翻轉
/// 子類別讀 Active 決定自己的狀態。同一個機關不要同時接拉桿和壓力板。
/// </summary>
public abstract class Activatable : MonoBehaviour
{
    readonly HashSet<PressurePlate> pressedBy = new HashSet<PressurePlate>();

    /// <summary>目前是否被啟動。</summary>
    protected bool Active { get; set; }

    /// <summary>壓力板呼叫。</summary>
    public void SetPressed(PressurePlate plate, bool pressed)
    {
        if (pressed) pressedBy.Add(plate);
        else pressedBy.Remove(plate);
        Active = pressedBy.Count > 0;
    }

    /// <summary>拉桿呼叫：翻轉。</summary>
    public void Toggle() => Active = !Active;

    /// <summary>程式直接開 / 關 (例如 TrueEndGate 打開 room 6 的門)。</summary>
    public void SetActivated(bool on) => Active = on;
}
