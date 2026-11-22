using UnityEngine;

/// <summary>
/// 音效入口：Sfx.Play(clip) 播一次 (2D，不分遠近)，音量 = volume × GameAudio.SfxVolume。
/// 第一次呼叫時自動建立一個跨場景的 AudioSource，不用在場景放任何東西。
/// </summary>
public static class Sfx
{
    static AudioSource source;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => source = null;

    public static void Play(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        if (source == null)
        {
            var go = new GameObject("Sfx");
            Object.DontDestroyOnLoad(go);
            source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
        }
        source.PlayOneShot(clip, volume * GameAudio.SfxVolume);
    }
}
