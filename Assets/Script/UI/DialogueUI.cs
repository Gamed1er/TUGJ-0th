using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 劇情對話框 (dialogue.png)。打字機一個字一個字出現，每個字播一次 voice (隨機音高，像 Undertale)。
/// 操作：Space / Enter / E / 左鍵 → 打字中 = 直接顯示整句；整句顯示完 = 下一句；最後一句按完就結束。
/// root 平常關著。由 GameManager.IntroRoutine (開場) / EndingUI.PlayStory (結局) 呼叫 Play (鎖玩家輸入是 GameManager 的事)。
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class DialogueUI : MonoBehaviour
{
    [SerializeField] GameObject root;
    [SerializeField] Text body;

    [Header("打字機")]
    [Tooltip("每秒出現幾個字")]
    [SerializeField] float charsPerSecond = 18f;
    [Tooltip("遇到標點 (，。…！？) 多停幾秒")]
    [SerializeField] float punctuationPause = 0.12f;

    [Header("對話音效")]
    [SerializeField] AudioClip voice;
    [SerializeField, Range(0f, 1f)] float voiceVolume = 0.6f;
    [Tooltip("每個字隨機的音高範圍")]
    [SerializeField] Vector2 pitchRange = new Vector2(0.9f, 1.15f);
    [Tooltip("每幾個字播一次音效 (1 = 每個字都播)")]
    [SerializeField, Min(1)] int voiceEvery = 1;

    const string Punctuation = "，。、…！？,.!?)）";

    AudioSource source;

    public bool Playing { get; private set; }

    void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        root.SetActive(false);
    }

    /// <summary>依序播放每一句，玩家看完最後一句才結束。</summary>
    public IEnumerator Play(IList<string> lines)
    {
        Playing = true;
        root.SetActive(true);
        foreach (var line in lines)
        {
            body.text = "";
            yield return null; // 上一句的按鍵不要算到這一句

            // 打字
            int shown = 0;
            float wait = 0f;
            while (shown < line.Length)
            {
                if (NextPressed()) { shown = line.Length; break; }
                wait -= Time.unscaledDeltaTime;
                if (wait <= 0f)
                {
                    char c = line[shown++];
                    wait += 1f / charsPerSecond + (Punctuation.IndexOf(c) >= 0 ? punctuationPause : 0f);
                    if (!char.IsWhiteSpace(c) && Punctuation.IndexOf(c) < 0 && shown % voiceEvery == 0) PlayVoice();
                }
                body.text = line.Substring(0, shown);
                yield return null;
            }
            body.text = line;
            yield return null;

            // 等玩家按鍵換下一句
            while (!NextPressed()) yield return null;
        }
        root.SetActive(false);
        Playing = false;
    }

    void PlayVoice()
    {
        if (voice == null) return;
        // 用 Play 而不是 PlayOneShot：下一個字直接蓋掉上一個，不會疊在一起，音高也只影響這一聲
        source.clip = voice;
        source.pitch = Random.Range(pitchRange.x, pitchRange.y);
        source.volume = voiceVolume * GameAudio.SfxVolume;
        source.Play();
    }

    public static bool NextPressed()
    {
        var kb = Keyboard.current;
        return (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame
                               || kb.numpadEnterKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame))
            || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
    }
}
