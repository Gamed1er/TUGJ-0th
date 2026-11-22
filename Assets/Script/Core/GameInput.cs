using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 全遊戲共用的輸入入口。所有按鍵綁定都集中在這裡，要改鍵位只改這個檔案。
/// 用法：GameInput.Move、GameInput.JumpPressed ... (每幀輪詢)
/// </summary>
public static class GameInput
{
    static InputAction move, vertical, jump, interact, use, map, scroll, restart;
    static bool initialized;

    /// <summary>
    /// 每次進入 Play 時重置。專案關閉了 Domain Reload (Enter Play Mode Options)，static 變數會留到下一次 Play，
    /// 但 Input System 在離開 Play 時會停用所有 action → 第二次 Play 輸入全部失效。所以這裡丟掉舊的、下次用到時重建。
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        if (initialized)
            foreach (var a in new[] { move, vertical, jump, interact, use, map, scroll, restart }) a?.Dispose();
        initialized = false;
        Locked = false;
    }

    static void Init()
    {
        if (initialized) return;
        initialized = true;

        move = new InputAction("Move", InputActionType.Value);
        move.AddCompositeBinding("1DAxis")
            .With("Negative", "<Keyboard>/a").With("Positive", "<Keyboard>/d");
        move.AddCompositeBinding("1DAxis")
            .With("Negative", "<Keyboard>/leftArrow").With("Positive", "<Keyboard>/rightArrow");

        // 垂直軸 (無人機上下飛用)。W / ↑ 同時也是跳躍鍵，操控無人機時玩家不吃輸入所以不衝突
        vertical = new InputAction("Vertical", InputActionType.Value);
        vertical.AddCompositeBinding("1DAxis")
            .With("Negative", "<Keyboard>/s").With("Positive", "<Keyboard>/w");
        vertical.AddCompositeBinding("1DAxis")
            .With("Negative", "<Keyboard>/downArrow").With("Positive", "<Keyboard>/upArrow");

        jump = new InputAction("Jump", InputActionType.Button);
        jump.AddBinding("<Keyboard>/space");
        jump.AddBinding("<Keyboard>/w");
        jump.AddBinding("<Keyboard>/upArrow");

        interact = new InputAction("Interact", InputActionType.Button, "<Keyboard>/e");
        use = new InputAction("UseItem", InputActionType.Button, "<Mouse>/rightButton");
        map = new InputAction("Map", InputActionType.Button, "<Keyboard>/m");
        scroll = new InputAction("Scroll", InputActionType.Value, "<Mouse>/scroll/y");
        restart = new InputAction("Restart", InputActionType.Button, "<Keyboard>/r");

        move.Enable(); vertical.Enable(); jump.Enable(); interact.Enable(); use.Enable(); map.Enable(); scroll.Enable(); restart.Enable();
    }

    /// <summary>暫停玩家輸入 (死亡、轉場時)。UI 仍可用滑鼠操作。</summary>
    public static bool Locked { get; set; }

    public static float Move { get { Init(); return Locked ? 0f : move.ReadValue<float>(); } }
    public static float Vertical { get { Init(); return Locked ? 0f : vertical.ReadValue<float>(); } }
    public static bool JumpPressed { get { Init(); return !Locked && jump.WasPressedThisFrame(); } }
    public static bool JumpHeld { get { Init(); return !Locked && jump.IsPressed(); } }
    public static bool InteractPressed { get { Init(); return !Locked && interact.WasPressedThisFrame(); } }
    public static bool UsePressed { get { Init(); return !Locked && use.WasPressedThisFrame(); } }
    public static bool UseHeld { get { Init(); return !Locked && use.IsPressed(); } }
    /// <summary>R：回到目前的重生點。</summary>
    public static bool RestartPressed { get { Init(); return !Locked && restart.WasPressedThisFrame(); } }
    /// <summary>M：開關終端機 (跟點工作列的終端機圖示一樣)。</summary>
    public static bool MapPressed { get { Init(); return map.WasPressedThisFrame(); } }

    // ---------- 選單 (終端機) 操作：不受 Locked 影響 ----------

    static bool KeyPressed(Key a, Key b)
    {
        var kb = Keyboard.current;
        return kb != null && (kb[a].wasPressedThisFrame || kb[b].wasPressedThisFrame);
    }

    /// <summary>選單上下左右 (WASD / 方向鍵) 這幀被按下的方向，沒有 = (0, 0)。上 = y +1。</summary>
    public static Vector2Int MenuMove =>
        KeyPressed(Key.W, Key.UpArrow) ? Vector2Int.up
        : KeyPressed(Key.S, Key.DownArrow) ? Vector2Int.down
        : KeyPressed(Key.A, Key.LeftArrow) ? Vector2Int.left
        : KeyPressed(Key.D, Key.RightArrow) ? Vector2Int.right
        : Vector2Int.zero;

    /// <summary>Enter：選單確定。</summary>
    public static bool SubmitPressed => KeyPressed(Key.Enter, Key.NumpadEnter);

    /// <summary>Esc：選單返回 / 關閉。</summary>
    public static bool CancelPressed => KeyPressed(Key.Escape, Key.Escape);

    /// <summary>滾輪：往上 = -1 (前一格)，往下 = +1 (下一格)，沒滾 = 0。</summary>
    public static int ScrollStep
    {
        get
        {
            Init();
            if (Locked) return 0;
            float y = scroll.ReadValue<float>();
            return y > 0.01f ? -1 : y < -0.01f ? 1 : 0;
        }
    }

    /// <summary>數字鍵 1~9 這幀被按下時回傳 0~8，否則 -1。</summary>
    public static int SlotKeyPressed
    {
        get
        {
            var kb = Keyboard.current;
            if (Locked || kb == null) return -1;
            for (int i = 0; i < 9; i++)
                if (kb[Key.Digit1 + i].wasPressedThisFrame) return i;
            return -1;
        }
    }

    /// <summary>滑鼠游標在世界中的座標 (2D)。</summary>
    public static Vector2 MouseWorld
    {
        get
        {
            var cam = Camera.main;
            if (cam == null || Mouse.current == null) return Vector2.zero;
            Vector3 p = Mouse.current.position.ReadValue();
            p.z = -cam.transform.position.z;
            return cam.ScreenToWorldPoint(p);
        }
    }
}
