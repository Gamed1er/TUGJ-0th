using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>畫面上方短暫顯示的訊息 (例如「已存檔」)。ToastUI.Show("...")。</summary>
public class ToastUI : MonoBehaviour
{
    static ToastUI instance;

    [SerializeField] CanvasGroup group;
    [SerializeField] Text label;
    [SerializeField] float showTime = 1.2f;
    [SerializeField] float fadeTime = 0.3f;

    Coroutine routine;

    void Awake()
    {
        instance = this;
        group.alpha = 0f;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    public static void Show(string text)
    {
        if (instance == null) return;
        instance.label.text = text;
        if (instance.routine != null) instance.StopCoroutine(instance.routine);
        instance.routine = instance.StartCoroutine(instance.Run());
    }

    IEnumerator Run()
    {
        group.alpha = 1f;
        yield return new WaitForSecondsRealtime(showTime);
        for (float t = 0f; t < fadeTime; t += Time.unscaledDeltaTime)
        {
            group.alpha = 1f - t / fadeTime;
            yield return null;
        }
        group.alpha = 0f;
    }
}
