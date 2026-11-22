using System;
using UnityEngine;

/// <summary>
/// 音量設定 (終端機的 settings 調整)。跟存檔一樣只存在記憶體，重新整理網頁就回到預設值。
/// 背景音樂用 MusicPlayer、音效用 Sfx.Play，兩者都會自動套用這裡的音量。
/// </summary>
public static class GameAudio
{
    /// <summary>音量分幾格 (終端機上的方塊數)。</summary>
    public const int Steps = 10;
    const int DefaultLevel = 7;

    static int musicLevel = DefaultLevel, sfxLevel = DefaultLevel;

    /// <summary>音量改變時觸發。</summary>
    public static event Action Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        musicLevel = sfxLevel = DefaultLevel;
        Changed = null;
    }

    /// <summary>0 ~ Steps。</summary>
    public static int MusicLevel
    {
        get => musicLevel;
        set { musicLevel = Mathf.Clamp(value, 0, Steps); Changed?.Invoke(); }
    }

    /// <summary>0 ~ Steps。</summary>
    public static int SfxLevel
    {
        get => sfxLevel;
        set { sfxLevel = Mathf.Clamp(value, 0, Steps); Changed?.Invoke(); }
    }

    public static float MusicVolume => (float)musicLevel / Steps;
    public static float SfxVolume => (float)sfxLevel / Steps;
}
