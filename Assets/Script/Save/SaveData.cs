using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 一個存檔 (save1 = 一周目、save2 = 二周目) 的完整內容。只能用 public 欄位 / List / 可序列化 class。
/// 要存新東西就在這裡加欄位，並在 GameManager 處理。
///
/// 模型：每個房間 (MapArea) 各有一份快照 (進入房間當下的物品欄 + 重生位置)。
/// currentRoomId = 目前的重生房間；死亡 / 讀檔都會回到這個房間的快照。
/// </summary>
[Serializable]
public class SaveData
{
    /// <summary>目前的重生房間 (MapArea.Id)；空字串 = 還沒存過 (從 PlayerStart 出生、拿開局道具)。</summary>
    public string currentRoomId = "";

    /// <summary>各房間的快照 (每個房間最多一份)。</summary>
    public List<RoomSnapshot> snapshots = new List<RoomSnapshot>();

    /// <summary>去過的房間。</summary>
    public List<string> discoveredRooms = new List<string>();

    /// <summary>第幾周目 (1 或 2)。</summary>
    public int loop = 1;

    /// <summary>二周目 transform 用的電池位置 (= 一周目進最後房間時快照累積的 RoomSnapshot.batteries)。屬於哪個房間由位置判斷 (MapArea.Contains)。</summary>
    public List<Vector2> batteryRecords = new List<Vector2>();

    /// <summary>最後存檔時間，給標題畫面顯示。</summary>
    public string lastSaveTime = "";

    public RoomSnapshot FindSnapshot(string roomId) => snapshots.Find(s => s.roomId == roomId);

    /// <summary>取得 (沒有就建立) 某房間的快照。</summary>
    public RoomSnapshot GetOrCreateSnapshot(string roomId)
    {
        var s = FindSnapshot(roomId);
        if (s == null) { s = new RoomSnapshot { roomId = roomId }; snapshots.Add(s); }
        return s;
    }

    /// <summary>深拷貝 (存進 / 讀出 SaveSystem 時用，避免遊戲中改到存檔本體)。</summary>
    public SaveData Clone() => JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(this));
}

/// <summary>某個房間的快照：進入該房間當下的物品欄與重生位置。</summary>
[Serializable]
public class RoomSnapshot
{
    public string roomId = "";

    /// <summary>重生位置 (進入房間時的落點)。</summary>
    public Vector2 spawn;

    /// <summary>物品欄內容，長度 = 格數；空格的 itemId 為空字串。</summary>
    public List<ItemSave> items = new List<ItemSave>();
    public int selectedSlot;

    /// <summary>二周目：已經用過 transform 的房間 (MapArea.Id)。</summary>
    public List<string> recallUsedAreas = new List<string>();

    /// <summary>
    /// 一周目：進這個房間之前，前面房間累積的電池紀錄 (玩家生成的電池位置)。
    /// 離開房間 (經過 MapEdge) 時 = 上個房間快照的紀錄 + 剛離開的房間裡現有的玩家電池。死亡重載不會掉。
    /// </summary>
    public List<Vector2> batteries = new List<Vector2>();

    /// <summary>二周目最後的房間 (room 6)：進來那一刻五個 [Target] 機關是否觸發中 (順序 = TrueEndGate 的順序)。</summary>
    public List<bool> targetResults = new List<bool>();
}

[Serializable]
public class ItemSave
{
    public string itemId = "";
    public int uses;
}
