using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 開發用快捷鍵，只在編輯器與 Development Build 生效 (正式版自動失效)。
/// F1 補滿所有道具 / F2 清除所有存檔 / F3 瞬移到下一個房間 / F4 無敵 / F5 自殺 / F6 回標題 / F12 顯示說明
/// </summary>
public class DebugTools : MonoBehaviour
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    bool showHelp = true;
    int teleportIndex = -1;

    void Update()
    {
        var kb = Keyboard.current;
        var gm = GameManager.Instance;
        if (kb == null || gm == null) return;

        if (kb.f1Key.wasPressedThisFrame)
        {
            foreach (var def in ItemRegistry.All)
            {
                var have = gm.Inventory.Find(def);
                gm.Inventory.AddUses(def, Mathf.Max(0, def.startingUses - (have != null ? have.uses : 0)));
            }
            ToastUI.Show("[Debug] 道具已補滿");
        }
        if (kb.f2Key.wasPressedThisFrame)
        {
            SaveSystem.DeleteAll();
            ToastUI.Show("[Debug] 已清除所有存檔");
        }
        if (kb.f3Key.wasPressedThisFrame && gm.Rooms.Count > 0)
        {
            // 下一個房間：瞬移到那個房間裡任一個 MapEdge 的入口 (不會存檔)
            teleportIndex = (teleportIndex + 1) % gm.Rooms.Count;
            var room = gm.Rooms[teleportIndex];
            foreach (var edge in FindObjectsByType<MapEdge>(FindObjectsSortMode.None))
            {
                if (!room.Contains(edge.EntryPoint)) continue;
                gm.player.Teleport(edge.EntryPoint);
                ToastUI.Show($"[Debug] 瞬移到 room{teleportIndex + 1}");
                break;
            }
        }
        if (kb.f4Key.wasPressedThisFrame)
        {
            gm.Invincible = !gm.Invincible;
            ToastUI.Show($"[Debug] 無敵 {(gm.Invincible ? "開" : "關")}");
        }
        if (kb.f5Key.wasPressedThisFrame) gm.KillPlayer();
        if (kb.f6Key.wasPressedThisFrame) gm.ReturnToTitle();
        if (kb.f12Key.wasPressedThisFrame) showHelp = !showHelp;
    }

    void OnGUI()
    {
        if (!showHelp) return;
        GUI.Label(new Rect(10, 10, 600, 24),
            $"[Debug] 欄位{SaveSystem.CurrentSlot + 1}  F1 補滿道具  F2 清存檔  F3 瞬移  F4 無敵({(GameManager.Instance != null && GameManager.Instance.Invincible ? "開" : "關")})  F5 自殺  F6 回標題  F12 隱藏");
    }
#endif
}
