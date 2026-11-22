using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 標題畫面的一個按鈕。索引 0 = 新的存檔 (已有存檔時要按兩次確認)、1 = save1、2 = save2 (沒有存檔時不能按)。
/// </summary>
public class TitleSlotUI : MonoBehaviour
{
    [SerializeField] Button playButton;
    [SerializeField] Text playLabel;

    TitleMenu menu;
    int index;
    bool confirming;

    public void Init(TitleMenu menu, int index)
    {
        this.menu = menu;
        this.index = index;
        playButton.onClick.AddListener(OnClick);
        Refresh();
    }

    int Slot => index == 1 ? SaveSystem.Save1 : SaveSystem.Save2;

    void Refresh()
    {
        if (index == 0)
        {
            playLabel.text = confirming ? "新的存檔\n<size=20>會清除 save1、save2，再按一次確定</size>" : "新的存檔";
            return;
        }
        var data = SaveSystem.Load(Slot);
        playButton.interactable = data != null;
        string name = index == 1 ? "save1  (一周目)" : "save2  (二周目)";
        playLabel.text = data != null ? $"{name}\n<size=20>{data.lastSaveTime}</size>" : $"{name}\n<size=20>---</size>";
    }

    void OnClick()
    {
        if (index != 0)
        {
            menu.Play(Slot);
            return;
        }
        if (SaveSystem.HasSave(SaveSystem.Save1) && !confirming)
        {
            confirming = true;
            Refresh();
            return;
        }
        menu.NewGame();
    }
}
