using UnityEngine;

/// <summary>
/// 存檔讀寫。只存在記憶體 (static)：關掉 / 重新整理網頁 (F5) 就清空；編輯器每次按 Play 也會清空。
/// 兩個存檔：Save1 = 一周目、Save2 = 二周目，都是自動存檔。標題畫面的「新的存檔」會清掉兩個重來。
/// CurrentSlot 由標題畫面設定，遊戲場景讀它來知道要用哪個存檔。
/// </summary>
public static class SaveSystem
{
    public const int Save1 = 0;
    public const int Save2 = 1;
    public const int SlotCount = 2;

    static SaveData[] slots = new SaveData[SlotCount];

    /// <summary>目前遊玩中的存檔。直接在編輯器開 Game 場景時預設用 save1。</summary>
    public static int CurrentSlot = Save1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        slots = new SaveData[SlotCount];
        CurrentSlot = Save1;
    }

    public static bool HasSave(int slot) => slots[slot] != null;

    /// <summary>讀出一份拷貝 (沒有存檔回傳 null)。</summary>
    public static SaveData Load(int slot) => slots[slot]?.Clone();

    public static void Save(int slot, SaveData data)
    {
        data.lastSaveTime = System.DateTime.Now.ToString("yyyy/MM/dd HH:mm");
        slots[slot] = data.Clone();
    }

    public static void Delete(int slot) => slots[slot] = null;

    public static void DeleteAll()
    {
        for (int i = 0; i < SlotCount; i++) slots[i] = null;
    }
}
