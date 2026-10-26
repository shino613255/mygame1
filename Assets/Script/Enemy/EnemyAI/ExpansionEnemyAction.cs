using System.Collections.Generic;
using UnityEngine;

public static class ExpansionEnemyAction
{
    public static bool UsesCharge(EnemyManager enemy) => enemy != null && enemy.data != null &&
        (enemy.data.role == EnemyRole.Caster || enemy.data.role == EnemyRole.Brute);

    public static bool IsSealed(EnemyManager enemy, SkillData skill)
    {
        if (enemy == null || skill == null) return false;
        int hands = 0;
        bool face = false;
        foreach (BodyPart part in enemy.GetComponentsInChildren<BodyPart>(true))
        {
            if (!part.canBreak || !part.IsBroken) continue;
            if (part.partType == PartType.Hand) hands++;
            if (part.partType == PartType.Face) face = true;
        }
        switch (skill.id)
        {
            case SkillId.Auditor_ManaLevy:
            case SkillId.Raider_BloodFury:
            case SkillId.Pilgrim_Renewal:
                return face;
            case SkillId.Auditor_ChargedLance:
            case SkillId.Pilgrim_TalismanWard:
                return hands > 0;
            case SkillId.Raider_ChargedAxe:
                return hands >= 2;
            default:
                return false;
        }
    }

    public static SkillData Choose(EnemyManager enemy, List<SkillData> pool, PlayerManager player)
    {
        var usable = pool.FindAll(s => !IsSealed(enemy, s));
        SkillData Find(SkillId id) => usable.Find(s => s.id == id);
        switch (enemy.data.role)
        {
            case EnemyRole.Caster:
                return (player != null && player.mp > 0 ? Find(SkillId.Auditor_ManaLevy) : null)
                    ?? Find(SkillId.Auditor_ChargedLance) ?? Find(SkillId.Auditor_ArcaneBolt);
            case EnemyRole.Brute:
                return Find(SkillId.Raider_ChargedAxe)
                    ?? (enemy.BattleEffects.EmpoweredAttacks == 0 &&
                        enemy.hp > Mathf.Max(1, Mathf.CeilToInt(enemy.maxHp * 0.08f))
                        ? Find(SkillId.Raider_BloodFury) : null)
                    ?? Find(SkillId.Raider_CleavingBlow);
            case EnemyRole.Warden:
                return (enemy.hp <= enemy.maxHp * 0.6f ? Find(SkillId.Pilgrim_Renewal) : null)
                    ?? (!enemy.BattleEffects.HasWard ? Find(SkillId.Pilgrim_TalismanWard) : null)
                    ?? Find(SkillId.Pilgrim_TalismanBolt);
            default:
                return null;
        }
    }
}
