using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 一個存檔欄位的完整內容。用 JsonUtility 序列化，所以只能用 public 欄位 / List / 可序列化 class。
/// 要存新東西就在這裡加欄位，並在 GameManager 處理。改了格式就把 CurrentVersion +1 (舊存檔會被當成空欄位)。
///
/// 模型：每個節點 (SavePoint) 各有一份快照 (到達當下的物品欄)。
/// currentNodeId = 目前的重生點；死亡 / 回溯都會回到這個節點的快照。
/// </summary>
[Serializable]
public class SaveData
{
    public const int CurrentVersion = 3;

    /// <summary>存檔格式版本，由 SaveSystem.Save 寫入。不符 = 舊存檔，當作空欄位。</summary>
    public int version;

    /// <summary>目前的重生節點 ID；空字串 = 還沒到過任何節點 (從 PlayerStart 出生、拿開局道具)。</summary>
    public string currentNodeId = "";

    /// <summary>各節點的快照 (每個節點最多一份)。</summary>
    public List<NodeSnapshot> snapshots = new List<NodeSnapshot>();

    /// <summary>去過的節點 (地圖上亮起)。</summary>
    public List<string> discoveredSavePoints = new List<string>();

    /// <summary>已被消耗/停用的節點 (預留給企劃的「消耗存檔」trick)：不會存檔、不能回溯。</summary>
    public List<string> disabledSavePoints = new List<string>();

    /// <summary>第幾周目 (1 或 2)。一周目通關 (ClearGoal) 後變成 2，回不去 1。</summary>
    public int loop = 1;

    /// <summary>一周目通關當下，玩家生成的電池位置 (二周目「請輸入文本」用)。屬於哪個區段由位置判斷 (MapArea.Find)。</summary>
    public List<Vector2> batteryRecords = new List<Vector2>();

    /// <summary>最後存檔時間，給標題畫面顯示。</summary>
    public string lastSaveTime = "";

    public NodeSnapshot FindSnapshot(string nodeId) => snapshots.Find(s => s.nodeId == nodeId);

    /// <summary>取得 (沒有就建立) 某節點的快照。</summary>
    public NodeSnapshot GetOrCreateSnapshot(string nodeId)
    {
        var s = FindSnapshot(nodeId);
        if (s == null) { s = new NodeSnapshot { nodeId = nodeId }; snapshots.Add(s); }
        return s;
    }
}

/// <summary>某個節點的快照：到達該節點當下的物品欄。</summary>
[Serializable]
public class NodeSnapshot
{
    public string nodeId = "";

    /// <summary>物品欄內容，長度 = 格數；空格的 itemId 為空字串。</summary>
    public List<ItemSave> items = new List<ItemSave>();
    public int selectedSlot;

    /// <summary>二周目：已經用過「請輸入文本」的區段 (MapArea.Id)。</summary>
    public List<string> recallUsedAreas = new List<string>();
}

[Serializable]
public class ItemSave
{
    public string itemId = "";
    public int uses;
}
