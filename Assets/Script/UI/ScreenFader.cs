using System.Collections;
using UnityEngine;

/// <summary>
/// 全螢幕淡入淡出。場景開始時從黑畫面淡入；FadeOut() 給死亡/切場景用。
/// 掛在一個全螢幕黑色 Image 上 (需要 CanvasGroup)。
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class ScreenFader : MonoBehaviour
{
    public float duration = 0.35f;

    CanvasGroup group;

    void Awake()
    {
        group = GetComponent<CanvasGroup>();
        group.alpha = 1f;
    }

    IEnumerator Start() => Fade(1f, 0f);

    public IEnumerator FadeOut() => Fade(group.alpha, 1f);
    public IEnumerator FadeIn() => Fade(group.alpha, 0f);

    IEnumerator Fade(float from, float to)
    {
        group.blocksRaycasts = true;
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            group.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        group.alpha = to;
        group.blocksRaycasts = to > 0.5f;
    }
}
