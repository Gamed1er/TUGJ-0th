using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 二周目通關 (在通關點按 E，GameManager.EndingRoutine 呼叫)：
/// 1. PlayStory：全螢幕 CG + 字幕 (底部黑色漸層 + 白字，不用對話框) 播結局劇情。劇本一行一句，單獨一行 "cg_3" = 換成 CG3 那張圖；
///    CG 底下沒有句子 (例如最後的 cg_11) = 只顯示圖、按一下鍵繼續；"【end】" = 劇本結束。
/// 2. Play：全螢幕播 end 動畫一次，停在最後一格後顯示閃爍的「Press any key」，按任意鍵 / 點擊 → 結束 (GameManager 回標題)。
/// root 平常關著；anim 是 root 底下的 UIFrameAnimation (loop 不勾)；cgImage 是 root 底下的全螢幕 Image。
/// </summary>
public class EndingUI : MonoBehaviour
{
    [SerializeField] GameObject root;
    [SerializeField] UIFrameAnimation anim;
    [Tooltip("播完後閃爍的提示")]
    [SerializeField] GameObject pressAnyKey;
    [SerializeField] float blinkRate = 1.5f;

    [Header("結局劇情 CG")]
    [SerializeField] Image cgImage;
    [Tooltip("劇本的 cg_N 對應名稱是 CGN 的圖 (不分大小寫)")]
    [SerializeField] Sprite[] cgSprites;
    [Tooltip("結局用的字幕 (CG 底下的 Caption，也是 DialogueUI：打字機 + 音效一樣，外觀是黑色漸層 + 白字)")]
    [SerializeField] DialogueUI caption;

    static readonly Regex CgLine = new Regex(@"^cg_?(\d+)$", RegexOptions.IgnoreCase);

    void Awake() => root.SetActive(false);

    /// <summary>播結局劇情 (CG + 字幕)。</summary>
    public IEnumerator PlayStory(string script)
    {
        root.SetActive(true);
        anim.gameObject.SetActive(false);
        pressAnyKey.SetActive(false);
        cgImage.gameObject.SetActive(true);

        foreach (var (cg, lines) in Parse(script))
        {
            cgImage.sprite = FindCg(cg);
            cgImage.enabled = cgImage.sprite != null;
            if (lines.Count > 0 && caption != null)
                yield return caption.Play(lines);
            else
            {
                yield return null; // 上一句的按鍵不要算進來
                while (!DialogueUI.NextPressed()) yield return null;
            }
        }
    }

    /// <summary>顯示並播放 end 動畫，等玩家按鍵後結束。</summary>
    public IEnumerator Play()
    {
        root.SetActive(true);
        cgImage.gameObject.SetActive(false);
        anim.gameObject.SetActive(true);
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

    /// <summary>把劇本切成 (CG 編號, 句子) 段落。CG 標記前的句子沿用 -1 (沒有圖)。</summary>
    static List<(int cg, List<string> lines)> Parse(string script)
    {
        var result = new List<(int, List<string>)>();
        if (string.IsNullOrWhiteSpace(script)) return result;
        int cg = -1;
        List<string> lines = null;
        foreach (var raw in script.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0) continue;
            if (line == "【end】") break;
            var m = CgLine.Match(line);
            if (m.Success)
            {
                if (lines != null) result.Add((cg, lines));
                cg = int.Parse(m.Groups[1].Value);
                lines = new List<string>();
                continue;
            }
            lines ??= new List<string>();
            lines.Add(line);
        }
        if (lines != null) result.Add((cg, lines));
        return result;
    }

    Sprite FindCg(int number)
    {
        if (cgSprites == null) return null;
        string name = "cg" + number;
        foreach (var s in cgSprites)
            if (s != null && s.name.Replace("_", "").ToLowerInvariant() == name) return s;
        if (number >= 0) Debug.LogWarning($"[EndingUI] 找不到 CG{number} 的圖，請拖進 cgSprites");
        return null;
    }

    static bool AnyKey() =>
        (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) ||
        (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
}
