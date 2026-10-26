using System.Collections.Generic;
using UnityEngine;

public sealed class PlayerBattleState
{
    private readonly HashSet<PartType> learnedTechniques = new HashSet<PartType>();
    private int playerTurn;
    private int aimGrantedTurn;

    public int TechniqueCount => learnedTechniques.Count;
    public bool HasInsight => learnedTechniques.Contains(PartType.Face);
    public bool HasMartialArt => learnedTechniques.Contains(PartType.Hand);
    public bool HasDefensiveStance => learnedTechniques.Contains(PartType.Leg);
    public float GuardReduction { get; private set; }
    public bool IsAiming { get; private set; }
    public int EmpoweredAttacks { get; private set; }

    public void Reset()
    {
        learnedTechniques.Clear();
        playerTurn = 0;
        GuardReduction = 0f;
        IsAiming = false;
        EmpoweredAttacks = 0;
    }

    public bool LearnTechnique(PlayerRole role, BodyPart part)
    {
        return role == PlayerRole.Warrior && part != null && part.canBreak && part.IsBroken &&
            part.partType != PartType.Belly && learnedTechniques.Add(part.partType);
    }

    public int GetMpCost(int baseCost)
    {
        if (baseCost <= 0) return 0;
        return HasInsight ? Mathf.Max(1, Mathf.CeilToInt(baseCost * 0.8f)) : baseCost;
    }

    public void BeginPlayerTurn() => playerTurn++;

    public void EndPlayerTurn()
    {
        if (IsAiming && playerTurn > aimGrantedTurn) IsAiming = false;
    }

    public void EndEnemyAction() => GuardReduction = 0f;
    public void GrantGuard(float reduction) => GuardReduction = Mathf.Max(GuardReduction, reduction);

    public void GrantAim()
    {
        IsAiming = true;
        aimGrantedTurn = playerTurn;
    }

    public void GrantWarCry() => EmpoweredAttacks = 2;

    public float ConsumeAttackEffects(SkillData skill, out float accuracyBonus, out float partMultiplier)
    {
        accuracyBonus = IsAiming ? 0.15f : 0f;
        partMultiplier = IsAiming ? 1.25f : 1f;
        IsAiming = false;

        bool physical = skill == null || skill.skillType == SkillType.Physical;
        float multiplier = physical && HasMartialArt ? 1.15f : 1f;
        if (physical && EmpoweredAttacks > 0)
        {
            multiplier *= 1.25f;
            EmpoweredAttacks--;
        }
        if (skill != null && skill.id == SkillId.Warrior_TacticalSlash)
            multiplier *= 1f + 0.1f * TechniqueCount;
        if (skill != null && skill.id == SkillId.Warrior_GuardSlash)
            GrantGuard(0.25f);
        return multiplier;
    }

    public int ReduceDirectDamage(int damage)
    {
        float multiplier = (HasDefensiveStance ? 0.85f : 1f) * (1f - GuardReduction);
        return Mathf.Max(1, Mathf.RoundToInt(damage * multiplier));
    }
}
