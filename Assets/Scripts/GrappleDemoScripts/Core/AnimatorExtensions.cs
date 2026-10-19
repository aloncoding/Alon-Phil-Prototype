using System.Collections.Generic;
using UnityEngine;

/// Safe animator calls: silently skip parameters that don't exist in the controller yet.
public static class AnimatorExtensions
{
    static readonly Dictionary<int, HashSet<int>> cache = new();

    public static bool Has(this Animator a, int hash)
    {
        if (a == null || a.runtimeAnimatorController == null) return false;
        int id = a.runtimeAnimatorController.GetInstanceID();
        if (!cache.TryGetValue(id, out var set))
        {
            set = new HashSet<int>();
            foreach (var p in a.parameters) set.Add(p.nameHash);
            cache[id] = set;
        }
        return set.Contains(hash);
    }

    public static void SafeTrigger(this Animator a, int h) { if (a.Has(h)) a.SetTrigger(h); }
    public static void SafeBool(this Animator a, int h, bool v) { if (a.Has(h)) a.SetBool(h, v); }
    public static void SafeFloat(this Animator a, int h, float v) { if (a.Has(h)) a.SetFloat(h, v); }
}
