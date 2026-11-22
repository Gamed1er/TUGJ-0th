using UnityEngine;

/// <summary>
/// 背景音樂：掛在場景中任意物件上，指定 clip 就會循環播放，音量 = baseVolume × GameAudio.MusicVolume。
/// 每個場景各放一個 (Title、Game 可以放不同的歌)。
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class MusicPlayer : MonoBehaviour
{
    [SerializeField] AudioClip clip;
    [Range(0f, 1f)] [SerializeField] float baseVolume = 1f;

    AudioSource source;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
    }

    void OnEnable()
    {
        GameAudio.Changed += Apply;
        Apply();
        if (clip != null && !source.isPlaying) { source.clip = clip; source.Play(); }
    }

    void OnDisable() => GameAudio.Changed -= Apply;

    void Apply() => source.volume = baseVolume * GameAudio.MusicVolume;
}
