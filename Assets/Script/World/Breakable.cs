using UnityEngine;

/// <summary>
/// 可破壞物件 (1x1)。被飛行中的黑洞撞到會被破壞，黑洞也會消失。
/// 放在 Ground 圖層。不存檔 (死亡重載會恢復)。要做大面牆就排很多個。
/// </summary>
public class Breakable : MonoBehaviour
{
    public void Break()
    {
        // 之後可以在這裡加破壞特效 / 音效
        Destroy(gameObject);
    }
}
