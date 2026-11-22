using UnityEngine;

/// <summary>
/// 音效入口：Sfx.Play(clip) 播一次 (2D，不分遠近)，音量 = volume × GameAudio.SfxVolume。
/// Sfx.PlayPitched(clip, volume, pitchRange) = 每次隨機音高 (腳步聲這種一直重複的音效用，聽起來比較不單調)。
/// 第一次呼叫時自動建立一個跨場景的物件 (兩個 AudioSource：一般 / 變音高)，不用在場景放任何東西。
/// </summary>
public static class Sfx
{
    static AudioSource source;
    static AudioSource pitchedSource; // 音高是整個 AudioSource 共用的，所以變音高的另外用一個

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => source = pitchedSource = null;

    public static void Play(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        Init();
        source.PlayOneShot(clip, volume * GameAudio.SfxVolume);
    }

    /// <summary>隨機音高播一次 (pitchRange = 最低 ~ 最高，1 = 原音高)。</summary>
    public static void PlayPitched(AudioClip clip, float volume, Vector2 pitchRange)
    {
        if (clip == null) return;
        Init();
        pitchedSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
        pitchedSource.PlayOneShot(clip, volume * GameAudio.SfxVolume);
    }

    static void Init()
    {
        if (source != null) return;
        var go = new GameObject("Sfx");
        Object.DontDestroyOnLoad(go);
        source = CreateSource(go);
        pitchedSource = CreateSource(go);
    }

    static AudioSource CreateSource(GameObject go)
    {
        var s = go.AddComponent<AudioSource>();
        s.playOnAwake = false;
        s.spatialBlend = 0f;
        return s;
    }
}
