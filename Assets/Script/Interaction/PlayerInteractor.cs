using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 掛在玩家身上。追蹤範圍內的 IInteractable，選最近的一個顯示提示，按 E 觸發互動。
/// </summary>
public class PlayerInteractor : MonoBehaviour
{
    readonly List<IInteractable> inRange = new List<IInteractable>();
    IInteractable current;

    public Inventory Inventory { get; private set; }
    public PlayerController Player { get; private set; }

    void Awake()
    {
        Inventory = GetComponent<Inventory>();
        Player = GetComponent<PlayerController>();
    }

    void Update()
    {
        inRange.RemoveAll(i => i == null || (i as Object) == null); // 被 Destroy 的物件

        current = null;
        string prompt = null;
        float best = float.MaxValue;
        foreach (var i in inRange)
        {
            string p = i.GetPrompt(this);
            if (p == null) continue;
            float d = ((Vector2)(i.PromptPosition - transform.position)).sqrMagnitude;
            if (d < best) { best = d; current = i; prompt = p; }
        }

        if (current != null) InteractPromptUI.Show(prompt, current.PromptPosition);
        else InteractPromptUI.Hide();

        if (current != null && GameInput.InteractPressed) current.Interact(this);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var i = other.GetComponentInParent<IInteractable>();
        if (i != null && !inRange.Contains(i)) inRange.Add(i);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        var i = other.GetComponentInParent<IInteractable>();
        if (i != null) inRange.Remove(i);
    }

    void OnDisable() => InteractPromptUI.Hide();
}
