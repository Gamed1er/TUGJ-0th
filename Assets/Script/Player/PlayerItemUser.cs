using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 物品欄的輸入：數字鍵 1~9 選格、滾輪切換、右鍵使用。
/// 右鍵的生命週期：按下 → OnUseStart；按住 → OnUseHold；放開 / 選中的道具換了 / 輸入被鎖 → OnUseEnd。
/// 特例：黑洞飛行中時，右鍵一律是「固定黑洞」，不管選哪一格。
/// 滑鼠在 UI 上 (例如正在拖曳物品欄) 時右鍵不會使用道具。
/// 操控無人機時：物品欄鎖定，右鍵 = 退出操控。
/// </summary>
[RequireComponent(typeof(Inventory), typeof(PlayerController))]
public class PlayerItemUser : MonoBehaviour
{
    Inventory inventory;
    PlayerController player;
    ItemInstance held; // 正在按住使用中的道具

    void Awake()
    {
        inventory = GetComponent<Inventory>();
        player = GetComponent<PlayerController>();
    }

    void Update()
    {
        if (Drone.Controlled != null)
        {
            if (GameInput.UsePressed && !IsPointerOverUI()) Drone.Controlled.EndControl();
            return;
        }

        int key = GameInput.SlotKeyPressed;
        if (key >= 0) inventory.Select(key);
        inventory.Cycle(GameInput.ScrollStep);

        // 選中的道具換了 (切格、拖曳) 或放開右鍵 → 結束上一次的使用
        if (held != null && (held != inventory.SelectedItem || !GameInput.UseHeld)) EndHold();

        if (GameInput.UsePressed && !IsPointerOverUI())
        {
            if (BlackHole.Current != null && BlackHole.Current.IsFlying)
            {
                BlackHole.Current.Fix();
            }
            else if (inventory.SelectedItem != null)
            {
                held = inventory.SelectedItem;
                held.definition.OnUseStart(Context(held));
            }
        }
        else if (held != null)
        {
            held.definition.OnUseHold(Context(held));
        }
    }

    void EndHold()
    {
        var item = held;
        held = null;
        item.definition.OnUseEnd(Context(item));
    }

    ItemUseContext Context(ItemInstance item) => new ItemUseContext
    {
        player = player,
        inventory = inventory,
        item = item,
        aimWorld = GameInput.MouseWorld,
    };

    static bool IsPointerOverUI() =>
        EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
}
