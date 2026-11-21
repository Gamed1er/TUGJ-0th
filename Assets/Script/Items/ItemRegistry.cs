using System.Collections.Generic;
using UnityEngine;

/// <summary>用 id 找道具資產 (讀檔時用)。會自動載入 Resources/Items/ 底下所有 ItemDefinition。</summary>
public static class ItemRegistry
{
    static Dictionary<string, ItemDefinition> map;

    // 關閉 Domain Reload 時 static 會留到下一次 Play → 每次進 Play 重新載入 (才吃得到新加的道具資產)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => map = null;

    public static IEnumerable<ItemDefinition> All { get { EnsureLoaded(); return map.Values; } }

    public static ItemDefinition Get(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        EnsureLoaded();
        if (map.TryGetValue(id, out var def)) return def;
        Debug.LogWarning($"找不到道具 id: {id} (資產是否在 Resources/Items/ 底下？)");
        return null;
    }

    static void EnsureLoaded()
    {
        if (map != null) return;
        map = new Dictionary<string, ItemDefinition>();
        foreach (var def in Resources.LoadAll<ItemDefinition>("Items"))
        {
            if (map.ContainsKey(def.id)) Debug.LogWarning($"道具 id 重複: {def.id}");
            else map[def.id] = def;
        }
    }
}
