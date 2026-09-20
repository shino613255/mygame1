using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TankRoleAction : IEnemyRoleAction
{
    public SkillData ChooseSkill(
        EnemyManager enemy,
        List<SkillData> pool
    )
    {
        SkillData defenseSkill =
            pool.Find(s =>
                s.skillType == SkillType.Buff &&
                s.buff != null &&
                s.buff.type == BuffType.DefenseUp
            );

        SkillData magicDefenseSkill =
            pool.Find(s =>
                s.skillType == SkillType.Buff &&
                s.buff != null &&
                s.buff.type == BuffType.MagicDefenseUp
            );

        SkillData healSkill =
            pool.Find(s =>
                s.skillType == SkillType.Heal
            );

        // ‡@ DefenseUp‚ª–³‚¯‚ê‚ÎÅ—Dæ
        if (
            !enemy.IsDefenseBuffed &&
            defenseSkill != null
        )
        {
            return defenseSkill;
        }

        // ‡A MagicDefenseUp‚ª–³‚¯‚ê‚ÎŸ‚Ég—p
        if (
            !enemy.IsMagicDefenseBuffed &&
            magicDefenseSkill != null
        )
        {
            return magicDefenseSkill;
        }

        // ‡B —¼•ûc‚Á‚Ä‚¢‚éê‡
        // HP‚ª30%ˆÈ‰º‚È‚çHeal
        float hpRate =
            (float)enemy.hp / enemy.maxHp;

        if (
            hpRate <= 0.3f &&
            healSkill != null &&
            Random.value <= 0.5f
        )
        {
            return healSkill;
        }

        // ‡C ‰½‚à‚·‚é•K—v‚ª‚È‚¯‚ê‚Î’ÊíUŒ‚
        return null;
    }
}