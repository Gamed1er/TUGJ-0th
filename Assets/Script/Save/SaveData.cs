using System;
using System.Collections.Generic;

/// <summary>
/// 一個存檔欄位的完整內容。用 JsonUtility 序列化，所以只能用 public 欄位 / List / 可序列化 class。
/// 要存新東西就在這裡加欄位，並在 GameManager 的 BuildSaveData / ApplySaveData 處理。
/// </summary>
[Serializable]
public class SaveData
{
    /// <summary>最後存檔的存檔點 ID；空字串 = 尚未存過 (從 PlayerStart 出生)。</summary>
    public string savePointId = "";

    /// <summary>物品欄內容，長度 = 格數；空格的 itemId 為空字串。整個清單是空的 = 新遊戲 (發開局道具)。</summary>
    public List<ItemSave> items = new List<ItemSave>();
    public int selectedSlot;

    /// <summary>去過的存檔點 (地圖上亮起)。</summary>
    public List<string> discoveredSavePoints = new List<string>();

    /// <summary>已被消耗/停用的存檔點 (預留給企劃的「消耗存檔」trick)。</summary>
    public List<string> disabledSavePoints = new List<string>();

    /// <summary>已被撿走的場景補給 ID (地圖狀態唯一的例外，避免讀檔刷次數)。</summary>
    public List<string> pickedUpIds = new List<string>();

    /// <summary>最後存檔時間，給標題畫面顯示。</summary>
    public string lastSaveTime = "";
}

[Serializable]
public class ItemSave
{
    public string itemId = "";
    public int uses;
}
