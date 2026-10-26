using UnityEngine;

public static class SkillUseRules
{
    public static bool IsDirectAttack(SkillData skill) => skill != null &&
        (skill.skillType == SkillType.Physical || skill.skillType == SkillType.Magic);

    public static bool IsAllowedForRole(SkillData skill, PlayerRole role)
    {
        if (skill == null) return false;
        if (skill.id == SkillId.None) return true;
        if (skill.id >= SkillId.Warrior_BreakSlash && skill.id <= SkillId.Warrior_TacticalSlash)
            return role == PlayerRole.Warrior;
        if (skill.id >= SkillId.Mage_FlamePiercer && skill.id <= SkillId.Mage_ArcaneBurst)
            return role == PlayerRole.Mage;
        if (skill.id >= SkillId.Berserker_Rend && skill.id <= SkillId.Berserker_WarCry)
            return role == PlayerRole.Berserker;
        return skill.id >= SkillId.Common_Guard && skill.id <= SkillId.Common_PartProbe;
    }

    public static int HpCost(UnitBase user, SkillData skill)
    {
        float rate = skill.id == SkillId.Berserker_BloodPrice ? 0.1f :
            skill.id == SkillId.Raider_BloodFury ? 0.08f : 0f;
        return rate > 0f ? Mathf.Max(1, Mathf.CeilToInt(user.maxHp * rate)) : 0;
    }

    public static int MpCost(UnitBase user, SkillData skill) => user is PlayerManager player
        ? player.GetSkillMpCost(skill) : Mathf.Max(0, skill.mpCost);

    public static bool CanUse(UnitBase user, UnitBase target, SkillData skill, out string reason)
    {
        reason = "スキルを使用できない。";
        if (user == null || target == null || skill == null || !user.IsAlive || !target.IsAlive) return false;
        if (skill.targetType == TargetType.Self ? target != user : target == user) return false;
        if (user is PlayerManager player &&
            (!player.HasSkill(skill) || !IsAllowedForRole(skill, player.Role)))
        {
            reason = "この職業では使用できないスキル。";
            return false;
        }
        if (user is EnemyManager && skill.id > SkillId.None && skill.id < SkillId.Auditor_ArcaneBolt) return false;
        if (user is EnemyManager enemy && ExpansionEnemyAction.IsSealed(enemy, skill)) return false;
        var cds = user.GetComponent<SkillCooldowns>();
        if (cds != null && !cds.IsReady(skill))
        {
            reason = $"{skill.skillName}はクールダウン中！";
            return false;
        }
        if (user.mp < MpCost(user, skill))
        {
            reason = "MPが足りない！";
            return false;
        }
        if (user.hp <= HpCost(user, skill))
        {
            reason = "HPコストを支払うと倒れてしまう！";
            return false;
        }
        if (skill.id == SkillId.Common_Antidote && (user is not PlayerManager poisoned || !poisoned.IsPoisoned))
        {
            reason = "毒状態ではない。";
            return false;
        }
        if (skill.id == SkillId.Common_FirstAid && user.hp >= user.maxHp)
        {
            reason = "HPは満タン。";
            return false;
        }
        if (skill.id == SkillId.Raider_BloodFury && user is EnemyManager raider && raider.BattleEffects.EmpoweredAttacks > 0)
            return false;
        if (skill.id == SkillId.Pilgrim_TalismanWard && user is EnemyManager pilgrim && pilgrim.BattleEffects.HasWard)
            return false;
        reason = string.Empty;
        return true;
    }

    public static bool TryPay(UnitBase user, UnitBase target, SkillData skill, out string reason)
    {
        if (!CanUse(user, target, skill, out reason)) return false;
        user.mp -= MpCost(user, skill);
        user.hp -= HpCost(user, skill);
        return true;
    }
}
