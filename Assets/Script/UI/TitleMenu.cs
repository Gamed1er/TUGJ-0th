using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>標題畫面：3 個存檔欄位。選欄位後進入 Game 場景。</summary>
public class TitleMenu : MonoBehaviour
{
    [SerializeField] TitleSlotUI[] slots;
    [SerializeField] ScreenFader fader;

    void Start()
    {
        GameInput.Locked = false;
        Time.timeScale = 1f;
        for (int i = 0; i < slots.Length; i++) slots[i].Init(this, i);
    }

    /// <summary>空欄位 → 建立初始存檔再進遊戲；有存檔 → 直接繼續。</summary>
    public void Play(int slot)
    {
        if (!SaveSystem.HasSave(slot)) SaveSystem.Save(slot, new SaveData());
        SaveSystem.CurrentSlot = slot;
        StartCoroutine(LoadGame());
    }

    IEnumerator LoadGame()
    {
        if (fader != null) yield return fader.FadeOut();
        SceneManager.LoadScene(SceneNames.Game);
    }
}
