using UnityEngine;

/// <summary>
/// 持續循環的音效 (火箭噴射、黑洞吸入)。掛在物件上，由那個物件的腳本呼叫 Play / Stop；物件被刪掉聲音也跟著停。
/// 音量 = volume × GameAudio.SfxVolume (會跟著設定即時改變)；遊戲暫停 (Time.timeScale = 0，例如開終端機) 時暫停。
/// silentDistance > 0 → 依跟玩家的距離衰減：fullVolumeDistance 內最大聲、silentDistance 外聽不到。
/// </summary>
public class LoopSfx : MonoBehaviour
{
    [Tooltip("要循環的音檔 (頭尾音量要接得起來，接縫才不會有喀聲)")]
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 0.6f;
    [Tooltip("距離玩家這個範圍 (格) 內是最大聲")]
    public float fullVolumeDistance = 0f;
    [Tooltip("距離玩家超過這個範圍 (格) 就聽不到。0 = 不依距離衰減")]
    public float silentDistance = 0f;

    AudioSource source;
    bool playing;

    void Awake()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.clip = clip;
    }

    public void Play()
    {
        if (playing || clip == null) return;
        playing = true;
        source.clip = clip;
        source.volume = CurrentVolume();
        source.Play();
    }

    public void Stop()
    {
        playing = false;
        if (source != null) source.Stop();
    }

    void Update()
    {
        if (!playing) return;
        bool paused = Time.timeScale == 0f;
        if (paused && source.isPlaying) source.Pause();
        else if (!paused && !source.isPlaying) source.UnPause();
        source.volume = CurrentVolume();
    }

    float CurrentVolume()
    {
        float v = volume * GameAudio.SfxVolume;
        var gm = GameManager.Instance;
        if (silentDistance > 0f && gm != null && gm.player != null)
        {
            float d = Vector2.Distance(transform.position, gm.player.Body.position);
            v *= 1f - Mathf.InverseLerp(fullVolumeDistance, silentDistance, d);
        }
        return v;
    }
}
