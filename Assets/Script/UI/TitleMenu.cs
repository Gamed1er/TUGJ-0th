using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// 標題畫面：背景循環播 main 動畫 (UIFrameAnimation，「開始」的字畫在背景圖上)。
/// 點擊任意位置 (或 Enter / Space) → 彈出終端機風格的存檔視窗 (TerminalMenu)：
///   new save (已有存檔要按兩次，清掉 save1、save2 從一周目開始) / save1 / save2 (存在才能選)。
/// 視窗右上角 × 或 Esc 關閉，回到動畫。
/// 角色 (另一個 UIFrameAnimation，疊在背景上) 平常循環 main_character 1~16；
/// 選好存檔後關掉視窗、換播 departFrames (main_character2 1~9) 一次 + departSfx，播完才淡出進遊戲。
/// </summary>
public class TitleMenu : MonoBehaviour
{
    [SerializeField] ScreenFader fader;

    [Header("角色")]
    [Tooltip("疊在背景上的角色動畫 (平常的循環圖設在它自己的 frames)")]
    [SerializeField] UIFrameAnimation character;
    [Tooltip("選好存檔後播一次的離場動畫")]
    [SerializeField] Sprite[] departFrames;
    [Tooltip("離場動畫開始時播的音效")]
    [SerializeField] AudioClip departSfx;

    [Header("存檔視窗")]
    [SerializeField] GameObject popup;
    [SerializeField] RectTransform column;
    [Tooltip("沒指定就用內建字型")]
    [SerializeField] Font font;
    [SerializeField] int fontSize = 34;
    [SerializeField] float rowHeight = 52f;
    [Tooltip("選單按下 (Enter / 點擊) 的音效")]
    [SerializeField] AudioClip clickSfx;
    [SerializeField] Color normalColor = new Color(0.3f, 1f, 0.4f);
    [Tooltip("不能選的行 (沒有存檔)")]
    [SerializeField] Color dimColor = new Color(0.15f, 0.45f, 0.2f);
    [Tooltip("new save 確認中")]
    [SerializeField] Color warnColor = new Color(1f, 0.25f, 0.2f);

    TerminalMenu menu;
    bool confirmingNew;
    bool loading;
    int closedFrame = -1; // 按 × 關閉的那一幀不能又被當成「點擊開始」

    bool PopupOpen => popup.activeSelf;

    void Start()
    {
        GameInput.Locked = false;
        Time.timeScale = 1f;
        menu = new TerminalMenu(font, fontSize, rowHeight, clickSfx, column);
        popup.SetActive(false);
    }

    void Update()
    {
        if (loading) return;
        if (!PopupOpen)
        {
            if (StartPressed() && Time.frameCount != closedFrame) Open();
            return;
        }
        if (GameInput.CancelPressed) { Close(); return; }
        menu.HandleInput();
    }

    static bool StartPressed()
    {
        var kb = Keyboard.current;
        return (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            || (kb != null && (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame));
    }

    void Open()
    {
        popup.SetActive(true);
        confirmingNew = false;
        menu.ForgetSelection();
        Rebuild();
    }

    /// <summary>右上角的 × 按鈕呼叫。</summary>
    public void Close()
    {
        if (loading) return;
        popup.SetActive(false);
        closedFrame = Time.frameCount;
    }

    void Rebuild()
    {
        menu.Clear();
        menu.Add(0, "== select save ==", normalColor, bold: true);
        var first = menu.Add(0, confirmingNew ? "new save  (press again: clears save1 / save2)" : "new save",
                             confirmingNew ? warnColor : normalColor, NewGame);
        AddSaveRow(SaveSystem.Save1, "save1  loop 1");
        AddSaveRow(SaveSystem.Save2, "save2  loop 2");
        menu.EndBuild(first);
    }

    void AddSaveRow(int slot, string name)
    {
        var data = SaveSystem.Load(slot);
        if (data == null) menu.Add(0, name + "   ----", dimColor);
        else menu.Add(0, $"{name}   {data.lastSaveTime}", normalColor, () => Play(slot));
    }

    /// <summary>新的存檔：已有存檔要按兩次；清掉 save1、save2，從一周目開頭開始。</summary>
    void NewGame()
    {
        if (SaveSystem.HasSave(SaveSystem.Save1) && !confirmingNew)
        {
            confirmingNew = true;
            Rebuild();
            return;
        }
        SaveSystem.DeleteAll();
        SaveSystem.Save(SaveSystem.Save1, new SaveData());
        Play(SaveSystem.Save1);
    }

    /// <summary>讀 save1 / save2 (SaveSystem.Save1 / Save2)。</summary>
    void Play(int slot)
    {
        if (loading || !SaveSystem.HasSave(slot)) return;
        loading = true;
        SaveSystem.CurrentSlot = slot;
        StartCoroutine(LoadGame());
    }

    IEnumerator LoadGame()
    {
        popup.SetActive(false);
        if (character != null && departFrames != null && departFrames.Length > 0)
        {
            character.Play(departFrames, loop: false);
            Sfx.Play(departSfx);
            yield return new WaitUntil(() => character.Finished);
        }
        if (fader != null) yield return fader.FadeOut();
        SceneManager.LoadScene(SceneNames.Game);
    }
}
