using UnityEngine;

/// <summary>
/// 存檔讀寫。資料以 JSON 存在 PlayerPrefs (WebGL 上會存進瀏覽器的 IndexedDB)。
/// CurrentSlot 由標題畫面設定，遊戲場景讀它來知道要用哪個欄位。
/// </summary>
public static class SaveSystem
{
    public const int SlotCount = 3;
    const string KeyPrefix = "TUGJ_Save_";

    /// <summary>目前遊玩中的欄位。直接在編輯器開 Game 場景時預設用欄位 0。</summary>
    public static int CurrentSlot = 0;

    public static bool HasSave(int slot) => PlayerPrefs.HasKey(KeyPrefix + slot);

    public static SaveData Load(int slot)
    {
        if (!HasSave(slot)) return null;
        try { return JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(KeyPrefix + slot)); }
        catch { Debug.LogWarning($"存檔 {slot} 損毀，視為空檔"); return null; }
    }

    public static void Save(int slot, SaveData data)
    {
        data.lastSaveTime = System.DateTime.Now.ToString("yyyy/MM/dd HH:mm");
        PlayerPrefs.SetString(KeyPrefix + slot, JsonUtility.ToJson(data));
        PlayerPrefs.Save(); // WebGL 需要明確呼叫才會寫入
    }

    public static void Delete(int slot)
    {
        PlayerPrefs.DeleteKey(KeyPrefix + slot);
        PlayerPrefs.Save();
    }

    public static void DeleteAll()
    {
        for (int i = 0; i < SlotCount; i++) PlayerPrefs.DeleteKey(KeyPrefix + i);
        PlayerPrefs.Save();
    }
}
