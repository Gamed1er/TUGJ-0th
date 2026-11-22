using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI Image 的逐格動畫 (標題的 main、通關的 end)。用不受暫停影響的時間播放。
/// loop = 循環；不循環時播完停在最後一格 (Finished = true)。
/// </summary>
[RequireComponent(typeof(Image))]
public class UIFrameAnimation : MonoBehaviour
{
    [SerializeField] Sprite[] frames;
    [SerializeField] float fps = 8f;
    [SerializeField] bool loop = true;

    Image image;
    float time;

    public bool Finished => !loop && frames.Length > 0 && time >= frames.Length / fps;

    void Awake() => image = GetComponent<Image>();

    void OnEnable() => Restart();

    public void Restart()
    {
        time = 0f;
        Show();
    }

    void Update()
    {
        time += Time.unscaledDeltaTime;
        Show();
    }

    void Show()
    {
        if (frames == null || frames.Length == 0 || image == null) return;
        int i = Mathf.FloorToInt(time * fps);
        image.sprite = frames[loop ? i % frames.Length : Mathf.Min(i, frames.Length - 1)];
    }
}
