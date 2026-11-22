using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 終端機 (取代舊的 M 地圖)。M 鍵或點工作列的終端機圖示開關；開著時遊戲暫停、玩家不能操作。
/// 操作：滑鼠移上去選取、點擊執行；或 WASD / 方向鍵選、Enter 執行、Esc 返回 / 關閉。
///
/// 主頁 (左右兩半)：
/// - 左上角 [settings] → 設定頁
/// - 左半：目前周目的存檔 (一周目 save1、二周目 save2)，由上往下列出 room1, room2...，點有紀錄的房間 = 讀檔 (要按兩次)
/// - 右半 (只有二周目)：save1，每個房間顯示 transform 的狀態：
///   目前所在且沒用過 = transform Item (綠色粗體，點了生成電池並關閉終端機)、不在這裡且沒用過 = exist (淡灰)、
///   用過 = File Not Exist (紅)、一周目沒有電池紀錄 = empty (灰)
/// 設定頁：music / sfx 音量 (A/D 或點左半 / 右半調整)、exit (按兩次回標題)、back。
///
/// 每一行都是程式產生的 Text (TerminalRow)，排版交給 leftColumn / rightColumn 上的 VerticalLayoutGroup。
/// 美術調整：panel 的背景圖 (SavePage)、字型 / 字級 / 顏色都在 Inspector。
/// </summary>
public class TerminalUI : MonoBehaviour
{
    [SerializeField] GameObject panel;
    [SerializeField] RectTransform leftColumn;
    [SerializeField] RectTransform rightColumn;
    [Tooltip("最下面一行的操作提示 (讀檔確認訊息也顯示在這)")]
    [SerializeField] Text hintLabel;
    [Tooltip("沒指定就用內建字型")]
    [SerializeField] Font font;
    [SerializeField] int fontSize = 30;
    [SerializeField] float rowHeight = 40f;
    [Tooltip("開終端機時暫停遊戲")]
    [SerializeField] bool pauseWhileOpen = true;

    [Header("顏色")]
    [SerializeField] Color normalColor = new Color(0.3f, 1f, 0.4f);
    [Tooltip("目前所在 / 重生的房間")]
    [SerializeField] Color currentColor = new Color(0.75f, 1f, 0.8f);
    [Tooltip("不能選的行 (沒去過、紀錄被覆蓋)")]
    [SerializeField] Color dimColor = new Color(0.15f, 0.45f, 0.2f);
    [Tooltip("exist / empty")]
    [SerializeField] Color grayColor = new Color(0.6f, 0.6f, 0.6f);
    [Tooltip("File Not Exist")]
    [SerializeField] Color redColor = new Color(1f, 0.25f, 0.2f);

    const string DefaultHint = "WASD / mouse: select   Enter / click: run   Esc: close";

    /// <summary>一行。selectable = 可以選取 / 執行。</summary>
    class Entry
    {
        public TerminalRow row;
        public string label;
        public Color color;
        public bool bold;
        public bool selectable;
        public int column;       // 0 = 左、1 = 右
        public int line;         // 在該欄的第幾行
        public Action submit;    // Enter / 點擊
        public Action<int> adjust; // 左右調整 (音量)
    }

    readonly List<Entry> entries = new List<Entry>();
    Entry selected;
    bool settingsPage;
    MapArea pendingLoad;  // 讀檔確認中的房間
    bool pendingExit;     // exit 確認中

    public bool IsOpen => panel.activeSelf;

    void Start()
    {
        panel.SetActive(false);
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        GameManager.Instance.StateChanged += OnStateChanged;
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.StateChanged -= OnStateChanged;
        if (IsOpen && pauseWhileOpen) Time.timeScale = 1f;
    }

    void OnStateChanged()
    {
        if (IsOpen) Rebuild();
    }

    void Update()
    {
        if (GameInput.MapPressed) { Toggle(); return; }
        if (!IsOpen) return;

        if (GameInput.CancelPressed)
        {
            if (settingsPage) ShowPage(false);
            else SetOpen(false);
            return;
        }
        Vector2Int move = GameInput.MenuMove;
        if (move.y != 0) MoveVertical(-move.y);
        else if (move.x != 0) MoveHorizontal(move.x);
        if (GameInput.SubmitPressed && selected != null) selected.submit?.Invoke();
    }

    // ---------- 開關 ----------

    /// <summary>工作列的終端機圖示呼叫。</summary>
    public void Toggle() => SetOpen(!IsOpen);

    /// <summary>右上角的 × 按鈕呼叫。</summary>
    public void Close() => SetOpen(false);

    public void SetOpen(bool open)
    {
        var gm = GameManager.Instance;
        if (open && (gm == null || gm.Busy)) return;
        if (open == IsOpen) return;
        panel.SetActive(open);
        if (pauseWhileOpen) Time.timeScale = open ? 0f : 1f;
        GameInput.Locked = open; // 開著時不能操作角色
        if (open) ShowPage(false);
        else InteractPromptUI.Hide();
    }

    void ShowPage(bool settings)
    {
        settingsPage = settings;
        pendingLoad = null;
        pendingExit = false;
        selected = null;
        SetHint(DefaultHint);
        Rebuild();
    }

    // ---------- 內容 ----------

    void Rebuild()
    {
        // 重建後盡量選回同一行
        int keepColumn = selected != null ? selected.column : 0;
        int keepLine = selected != null ? selected.line : -1;

        foreach (var e in entries) Destroy(e.row.gameObject);
        entries.Clear();
        selected = null;

        if (settingsPage) BuildSettings();
        else BuildMain();

        rightColumn.gameObject.SetActive(!settingsPage && GameManager.Instance.Loop >= 2);

        selected = entries.Find(e => e.selectable && e.column == keepColumn && e.line == keepLine)
                ?? entries.Find(e => e.selectable && e.column == 0 && e.line > 0)
                ?? entries.Find(e => e.selectable);
        RefreshRows();
    }

    void BuildMain()
    {
        var gm = GameManager.Instance;
        bool second = gm.Loop >= 2;

        // 左半：目前周目的存檔
        Add(0, "[settings]", normalColor, () => ShowPage(true));
        Add(0, second ? "== save2 ==" : "== save1 ==", normalColor, bold: true);
        for (int i = 0; i < gm.Rooms.Count; i++)
        {
            var room = gm.Rooms[i];
            string name = $"room{i + 1}";
            if (room.Id == gm.CurrentRoomId)
                Add(0, name + "  <- now", currentColor, () => Load(room), bold: true);
            else if (gm.CanRewindTo(room))
                Add(0, name, normalColor, () => Load(room));
            else
                Add(0, name + (gm.IsDiscovered(room) ? "  (overwritten)" : "  ----"), dimColor);
        }

        if (!second) return;

        // 右半：save1 (transform)
        Add(1, "", normalColor);
        Add(1, "== save1 ==", normalColor, bold: true);
        var here = gm.PlayerRoom;
        for (int i = 0; i < gm.Rooms.Count; i++)
        {
            var room = gm.Rooms[i];
            string name = $"room{i + 1}  ";
            if (!gm.HasRecords(room)) Add(1, name + "empty", grayColor);
            else if (gm.IsRecallUsed(room)) Add(1, name + "File Not Exist", redColor);
            else if (room == here) Add(1, name + "transform Item", normalColor, () => RecallRoom(room), bold: true);
            else Add(1, name + "exist", grayColor);
        }
    }

    void BuildSettings()
    {
        Add(0, "== settings ==", normalColor, bold: true);
        Add(0, VolumeLabel("music", GameAudio.MusicLevel), normalColor, null, d => { GameAudio.MusicLevel += d; Rebuild(); });
        Add(0, VolumeLabel("sfx  ", GameAudio.SfxLevel), normalColor, null, d => { GameAudio.SfxLevel += d; Rebuild(); });
        Add(0, pendingExit ? "exit  (press again to return to title)" : "exit", pendingExit ? redColor : normalColor, Exit);
        Add(0, "back", normalColor, () => ShowPage(false));
    }

    static string VolumeLabel(string name, int level) =>
        $"{name}  < [{new string('#', level)}{new string('-', GameAudio.Steps - level)}] >";

    void Add(int column, string label, Color color, Action submit = null, Action<int> adjust = null, bool bold = false)
    {
        var parent = column == 0 ? leftColumn : rightColumn;
        var go = new GameObject("Row", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleLeft;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        go.AddComponent<LayoutElement>().preferredHeight = rowHeight;
        var row = go.AddComponent<TerminalRow>();

        var e = new Entry
        {
            row = row,
            label = label,
            color = color,
            bold = bold,
            selectable = submit != null || adjust != null,
            column = column,
            line = entries.FindAll(x => x.column == column).Count,
            submit = submit,
            adjust = adjust,
        };
        if (adjust != null && submit == null) e.submit = () => adjust(1);
        text.raycastTarget = e.selectable;
        if (e.selectable)
        {
            row.Hovered = () => Select(e);
            row.Clicked = side =>
            {
                Select(e);
                if (e.adjust != null) e.adjust(side);
                else e.submit?.Invoke();
            };
        }
        entries.Add(e);
    }

    void RefreshRows()
    {
        foreach (var e in entries)
        {
            var t = e.row.Text;
            t.text = (e == selected ? "> " : "  ") + e.label;
            t.color = e.color;
            t.fontStyle = e.bold ? FontStyle.Bold : FontStyle.Normal;
        }
    }

    // ---------- 選取 ----------

    void Select(Entry e)
    {
        if (e == selected || !e.selectable) return;
        selected = e;
        RefreshRows();
    }

    void MoveVertical(int step)
    {
        if (selected == null) { selected = entries.Find(e => e.selectable); RefreshRows(); return; }
        var list = entries.FindAll(e => e.selectable && e.column == selected.column);
        int i = list.IndexOf(selected);
        if (i + step >= 0 && i + step < list.Count) Select(list[i + step]);
    }

    void MoveHorizontal(int dir)
    {
        if (selected == null) return;
        if (selected.adjust != null) { selected.adjust(dir); return; } // 設定頁：調音量
        int column = selected.column + dir;
        Entry best = null;
        int bestD = int.MaxValue;
        foreach (var e in entries)
        {
            if (!e.selectable || e.column != column) continue;
            int d = Mathf.Abs(e.line - selected.line);
            if (d < bestD) { bestD = d; best = e; }
        }
        if (best != null) Select(best);
    }

    // ---------- 動作 ----------

    void Load(MapArea room)
    {
        int n = GameManager.Instance.RoomIndex(room.Id) + 1;
        if (pendingLoad != room)
        {
            pendingLoad = room;
            SetHint($"Load room{n}? Press again to confirm (items return to that time)");
            return;
        }
        SetHint($"Loading room{n}...");
        SetOpen(false);
        GameManager.Instance.RewindTo(room);
    }

    void RecallRoom(MapArea room)
    {
        SetOpen(false);
        GameManager.Instance.Recall(room);
    }

    void Exit()
    {
        if (!pendingExit)
        {
            pendingExit = true;
            Rebuild();
            return;
        }
        SetOpen(false);
        GameManager.Instance.ReturnToTitle();
    }

    void SetHint(string text)
    {
        if (hintLabel != null) hintLabel.text = text;
    }
}
