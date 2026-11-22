using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 終端機風格的文字選單 (遊戲內的 TerminalUI、標題畫面的 TitleMenu 共用)。
/// 每一行是程式產生的 Text (TerminalRow)，放進 columns 上的 VerticalLayoutGroup。選中的行前面加 "> "。
/// 操作：滑鼠移上去選取、點擊執行；WASD / 方向鍵選、Enter 執行 (HandleInput)；A/D 在有 adjust 的行上調整數值、否則換欄。
/// 用法：Clear() → Add(...) 數行 → EndBuild()。重建後會盡量選回同一欄同一行。
/// </summary>
public class TerminalMenu
{
    public class Entry
    {
        public TerminalRow row;
        public string label;
        public Color color;
        public bool bold;
        public bool selectable;
        public int column;           // 第幾欄
        public int line;             // 在該欄的第幾行
        public Action submit;        // Enter / 點擊
        public Action<int> adjust;   // 左右調整 (-1 / +1)
    }

    readonly RectTransform[] columns;
    readonly Font font;
    readonly int fontSize;
    readonly float rowHeight;
    readonly List<Entry> entries = new List<Entry>();
    int keepColumn, keepLine = -1;

    public Entry Selected { get; private set; }

    public TerminalMenu(Font font, int fontSize, float rowHeight, params RectTransform[] columns)
    {
        this.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        this.fontSize = fontSize;
        this.rowHeight = rowHeight;
        this.columns = columns;
    }

    /// <summary>清掉所有行 (記住目前選的位置，EndBuild 時選回去)。</summary>
    public void Clear()
    {
        keepColumn = Selected != null ? Selected.column : 0;
        keepLine = Selected != null ? Selected.line : -1;
        foreach (var e in entries) UnityEngine.Object.Destroy(e.row.gameObject);
        entries.Clear();
        Selected = null;
    }

    /// <summary>下次 Clear / EndBuild 不選回目前的位置 (換頁時用)。</summary>
    public void ForgetSelection() => Selected = null;

    /// <summary>加一行。submit / adjust 都沒有 = 不能選的文字。</summary>
    public Entry Add(int column, string label, Color color, Action submit = null, Action<int> adjust = null, bool bold = false)
    {
        var go = new GameObject("Row", typeof(RectTransform));
        go.transform.SetParent(columns[column], false);
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
            submit = submit ?? (adjust != null ? () => adjust(1) : null),
            adjust = adjust,
        };
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
        return e;
    }

    /// <summary>加完所有行後呼叫：選回原本的位置，沒有就選 fallback (或第一個能選的)。</summary>
    public void EndBuild(Entry fallback = null)
    {
        Selected = entries.Find(e => e.selectable && e.column == keepColumn && e.line == keepLine)
                ?? (fallback != null && fallback.selectable ? fallback : null)
                ?? entries.Find(e => e.selectable);
        Refresh();
    }

    /// <summary>每幀呼叫：鍵盤選取 / 調整 / 執行。</summary>
    public void HandleInput()
    {
        Vector2Int move = GameInput.MenuMove;
        if (move.y != 0) MoveVertical(-move.y);
        else if (move.x != 0) MoveHorizontal(move.x);
        if (GameInput.SubmitPressed && Selected != null) Selected.submit?.Invoke();
    }

    void Refresh()
    {
        foreach (var e in entries)
        {
            var t = e.row.Text;
            t.text = (e == Selected ? "> " : "  ") + e.label;
            t.color = e.color;
            t.fontStyle = e.bold ? FontStyle.Bold : FontStyle.Normal;
        }
    }

    void Select(Entry e)
    {
        if (e == Selected || !e.selectable) return;
        Selected = e;
        Refresh();
    }

    void MoveVertical(int step)
    {
        if (Selected == null) return;
        var list = entries.FindAll(e => e.selectable && e.column == Selected.column);
        int i = list.IndexOf(Selected);
        if (i + step >= 0 && i + step < list.Count) Select(list[i + step]);
    }

    void MoveHorizontal(int dir)
    {
        if (Selected == null) return;
        if (Selected.adjust != null) { Selected.adjust(dir); return; }
        int column = Selected.column + dir;
        Entry best = null;
        int bestD = int.MaxValue;
        foreach (var e in entries)
        {
            if (!e.selectable || e.column != column) continue;
            int d = Mathf.Abs(e.line - Selected.line);
            if (d < bestD) { bestD = d; best = e; }
        }
        if (best != null) Select(best);
    }
}
