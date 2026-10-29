using System.Collections.Generic;
using UnityEngine;

public class SkillCooldowns : MonoBehaviour
{
    private readonly Dictionary<SkillData, int> remainingTurnsBySkill = new();

    public bool IsReady(SkillData skill)
    {
        return IsReadyAfterTicks(skill, 0);
    }

    public bool IsReadyAfterTicks(SkillData skill, int ticks)
    {
        if (skill == null)
        {
            return false;
        }

        return !remainingTurnsBySkill.TryGetValue(skill, out var remainingTurns) || remainingTurns <= Mathf.Max(0, ticks);
    }

    public void StartCooldown(SkillData skill)
    {
        if (skill == null)
        {
            return;
        }

        remainingTurnsBySkill[skill] = Mathf.Max(0, skill.cooldown);
    }

    public void Tick()
    {
        var keys = new List<SkillData>(remainingTurnsBySkill.Keys);
        foreach (var skill in keys)
        {
            remainingTurnsBySkill[skill] = Mathf.Max(0, remainingTurnsBySkill[skill] - 1);
        }
    }

    public int Remaining(SkillData skill) => skill != null && remainingTurnsBySkill.TryGetValue(skill, out int turns) ? turns : 0;

    public void Clear() => remainingTurnsBySkill.Clear();
}
