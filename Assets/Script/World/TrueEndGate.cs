using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 最後一個房間 (room 6) 的真結局門。場景裡名稱以 "[Target]" 開頭的拉桿 / 壓力板就是條件 (應該要有 5 個)，
/// 順序 = 由左到右 (x 座標)。
///
/// 二周目進入最後的房間那一刻，GameManager 呼叫 Evaluate 結算哪些條件「觸發中」(壓力板被壓著、拉桿拉開)，
/// 結果存進該房間的快照 (死亡重載也一樣)，再呼叫 ApplyRecord：全部達成 → 打開 door；沒有 → GameManager 播 BE。
///
/// 給美術元件查詢：TrueEndGate.Instance 的 TargetCount / IsRecorded(i) (進房間時的結算) / IsTriggeredNow(i) (即時狀態)，
/// 結算結果改變時觸發 Changed。
/// </summary>
public class TrueEndGate : MonoBehaviour
{
    public const string TargetPrefix = "[Target]";

    public static TrueEndGate Instance { get; private set; }

    [Tooltip("條件全部達成時打開的門 (不要再接拉桿 / 壓力板，也不要勾 startOpen)")]
    [SerializeField] Activatable door;

    readonly List<Component> targets = new List<Component>();
    readonly List<bool> recorded = new List<bool>();

    /// <summary>ApplyRecord 之後觸發 (美術元件監聽它更新外觀)。</summary>
    public event Action Changed;

    public int TargetCount => targets.Count;
    /// <summary>第 i 個條件的機關 (Lever 或 PressurePlate)。</summary>
    public Component GetTarget(int i) => targets[i];
    /// <summary>已經結算過 (二周目進過最後的房間)。</summary>
    public bool HasRecord => recorded.Count > 0;
    /// <summary>進入最後的房間時，第 i 個條件是否觸發中。</summary>
    public bool IsRecorded(int i) => i >= 0 && i < recorded.Count && recorded[i];
    /// <summary>結算結果：全部達成。</summary>
    public bool AllRecorded => targets.Count > 0 && recorded.Count == targets.Count && !recorded.Contains(false);

    void Awake()
    {
        Instance = this;
        foreach (var lever in FindObjectsByType<Lever>(FindObjectsSortMode.None))
            if (lever.name.StartsWith(TargetPrefix)) targets.Add(lever);
        foreach (var plate in FindObjectsByType<PressurePlate>(FindObjectsSortMode.None))
            if (plate.name.StartsWith(TargetPrefix)) targets.Add(plate);
        targets.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
        if (targets.Count != 5) Debug.LogWarning($"[TrueEndGate] 場景裡有 {targets.Count} 個 {TargetPrefix} 機關 (預期 5 個)", this);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>第 i 個條件現在是否觸發中 (壓力板被壓著 / 拉桿拉開)。</summary>
    public bool IsTriggeredNow(int i) => targets[i] switch
    {
        Lever lever => lever.On,
        PressurePlate plate => plate.Pressed,
        _ => false,
    };

    /// <summary>現在每個條件的狀態 (存進房間快照用)。</summary>
    public List<bool> Evaluate()
    {
        var result = new List<bool>();
        for (int i = 0; i < targets.Count; i++) result.Add(IsTriggeredNow(i));
        return result;
    }

    /// <summary>套用結算結果 (進房間 / 讀快照)：全部達成就開門。</summary>
    public void ApplyRecord(IList<bool> results)
    {
        recorded.Clear();
        if (results != null) recorded.AddRange(results);
        if (door != null) door.SetActivated(AllRecorded);
        Changed?.Invoke();
    }
}
