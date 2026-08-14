using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillCooldowns : MonoBehaviour
{
    private readonly Dictionary<SkillData, int> cd = new();

    public bool IsReady(SkillData skill)
    {
        if (skill == null) return false;
        return !cd.TryGetValue(skill, out var t) || t <= 0;
    }

    public void StartCooldown(SkillData skill)      
    {
        if (skill == null) return;
        cd[skill] = Mathf.Max(0, skill.cooldown);
    }

    // 1ターン経過時に全スキルのクールダウンを1減らす
    public void Tick()
    {
        var keys = new List<SkillData>(cd.Keys);
        foreach (var k in keys) cd[k] = Mathf.Max(0, cd[k] - 1);
    }
}