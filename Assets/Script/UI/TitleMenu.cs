using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 標題畫面：3 個按鈕 = 新的存檔 / save1 (一周目自動存檔) / save2 (二周目自動存檔)。
/// slots 的順序就是這 3 個 (TitleSlotUI.Kind 由索引決定)。
/// </summary>
public class TitleMenu : MonoBehaviour
{
    [SerializeField] TitleSlotUI[] slots;
    [SerializeField] ScreenFader fader;

    bool loading;

    void Start()
    {
        GameInput.Locked = false;
        Time.timeScale = 1f;
        for (int i = 0; i < slots.Length; i++) slots[i].Init(this, i);
    }

    /// <summary>新的存檔：清掉 save1、save2，從一周目開頭開始。</summary>
    public void NewGame()
    {
        SaveSystem.DeleteAll();
        SaveSystem.Save(SaveSystem.Save1, new SaveData());
        Play(SaveSystem.Save1);
    }

    /// <summary>讀 save1 / save2 (SaveSystem.Save1 / Save2)。</summary>
    public void Play(int slot)
    {
        if (loading || !SaveSystem.HasSave(slot)) return;
        loading = true;
        SaveSystem.CurrentSlot = slot;
        StartCoroutine(LoadGame());
    }

    IEnumerator LoadGame()
    {
        if (fader != null) yield return fader.FadeOut();
        SceneManager.LoadScene(SceneNames.Game);
    }
}
