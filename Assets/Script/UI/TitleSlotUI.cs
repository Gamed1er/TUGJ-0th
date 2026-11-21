using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 標題畫面的一個存檔欄位。
/// 空：顯示「新遊戲」；有資料：顯示「繼續 + 存檔時間」與「刪除」(按兩次才刪)。
/// </summary>
public class TitleSlotUI : MonoBehaviour
{
    [SerializeField] Button playButton;
    [SerializeField] Text playLabel;
    [SerializeField] Button deleteButton;
    [SerializeField] Text deleteLabel;

    TitleMenu menu;
    int slot;
    bool confirmingDelete;

    public void Init(TitleMenu menu, int slot)
    {
        this.menu = menu;
        this.slot = slot;
        playButton.onClick.AddListener(() => menu.Play(slot));
        deleteButton.onClick.AddListener(OnDelete);
        Refresh();
    }

    void Refresh()
    {
        var data = SaveSystem.Load(slot);
        bool has = data != null;
        playLabel.text = has
            ? $"欄位 {slot + 1}  繼續{(data.loop >= 2 ? "  (二周目)" : "")}\n<size=20>{data.lastSaveTime}</size>"
            : $"欄位 {slot + 1}  新遊戲";
        deleteButton.gameObject.SetActive(has);
        confirmingDelete = false;
        deleteLabel.text = "刪除";
    }

    void OnDelete()
    {
        if (!confirmingDelete)
        {
            confirmingDelete = true;
            deleteLabel.text = "確定？";
            return;
        }
        SaveSystem.Delete(slot);
        Refresh();
    }
}
