using UnityEngine;

/// <summary>
/// 背景音樂：掛在場景中任意物件上 (要是場景根物件)，指定 clip 就會循環播放，音量 = baseVolume × GameAudio.MusicVolume。
/// 每個場景各放一個 (Title = menu、Game_EGG = inGame)。
/// 跨場景保留 (DontDestroyOnLoad)：新場景的 MusicPlayer 是同一首歌 → 自己刪掉、舊的繼續播 (死亡重載 / 讀檔不會從頭播)；
/// 不同首 → 舊的刪掉、換新的播。
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class MusicPlayer : MonoBehaviour
{
    [SerializeField] AudioClip clip;
    [Range(0f, 1f)] [SerializeField] float baseVolume = 1f;

    static MusicPlayer current;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => current = null;

    AudioSource source;

    void Awake()
    {
        if (current != null && current != this)
        {
            if (current.clip == clip) { Destroy(gameObject); return; }
            Destroy(current.gameObject);
        }
        current = this;
        DontDestroyOnLoad(gameObject);

        source = GetComponent<AudioSource>();
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
    }

    void OnEnable()
    {
        if (source == null) return; // Awake 裡被判定重複、即將刪掉
        GameAudio.Changed += Apply;
        Apply();
        if (clip != null && !source.isPlaying) { source.clip = clip; source.Play(); }
    }

    void OnDisable() => GameAudio.Changed -= Apply;

    void OnDestroy()
    {
        if (current == this) current = null;
    }

    void Apply() => source.volume = baseVolume * GameAudio.MusicVolume;
}
