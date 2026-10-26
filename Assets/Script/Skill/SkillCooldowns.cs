using System.Collections.Generic;
using UnityEngine;

public class SkillCooldowns : MonoBehaviour
{
    private readonly Dictionary<SkillData, int> cd = new();

    public bool IsReady(SkillData skill)
    {
        return IsReadyAfterTicks(skill, 0);
    }

    public bool IsReadyAfterTicks(SkillData skill, int ticks)
    {
        if (skill == null) return false;
        return !cd.TryGetValue(skill, out var t) || t <= Mathf.Max(0, ticks);
    }

    public void StartCooldown(SkillData skill)
    {
        if (skill == null) return;
        cd[skill] = Mathf.Max(0, skill.cooldown);
    }

    public void Tick()
    {
        var keys = new List<SkillData>(cd.Keys);
        foreach (var k in keys) cd[k] = Mathf.Max(0, cd[k] - 1);
    }

    public int Remaining(SkillData skill) => skill != null && cd.TryGetValue(skill, out int turns) ? turns : 0;
    public void Clear() => cd.Clear();
}
