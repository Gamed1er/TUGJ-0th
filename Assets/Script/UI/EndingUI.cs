using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 通關畫面 (二周目在通關點按 E，GameManager.ClearGame 呼叫 Play)：全螢幕播 end 動畫一次，停在最後一格後
/// 顯示閃爍的「Press any key」，按任意鍵 / 點擊 → 結束 (GameManager 回標題)。
/// root 平常關著；anim 是 root 底下的 UIFrameAnimation (loop 不勾)。
/// </summary>
public class EndingUI : MonoBehaviour
{
    [SerializeField] GameObject root;
    [SerializeField] UIFrameAnimation anim;
    [Tooltip("播完後閃爍的提示")]
    [SerializeField] GameObject pressAnyKey;
    [SerializeField] float blinkRate = 1.5f;

    void Awake() => root.SetActive(false);

    /// <summary>顯示並播放，等玩家按鍵後結束。</summary>
    public IEnumerator Play()
    {
        root.SetActive(true);
        pressAnyKey.SetActive(false);
        anim.Restart();
        while (!anim.Finished) yield return null;

        for (float t = 0f; ; t += Time.unscaledDeltaTime)
        {
            pressAnyKey.SetActive(Mathf.Repeat(t * blinkRate, 1f) < 0.6f);
            yield return null;
            if (AnyKey()) break;
        }
    }

    static bool AnyKey() =>
        (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) ||
        (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
}
