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
        float hpRate =
            (float)enemy.hp / enemy.maxHp;

        SkillData healSkill =
            pool.Find(s =>
                s.skillType == SkillType.Heal
            );

        SkillData magicDefenseSkill =
            pool.Find(s =>
                s.skillType == SkillType.Buff &&
                s.buff != null &&
                s.buff.type == BuffType.MagicDefenseUp
            );

        SkillData defenseSkill =
            pool.Find(s =>
                s.skillType == SkillType.Buff &&
                s.buff != null &&
                s.buff.type == BuffType.DefenseUp
            );

        if (hpRate <= 0.3f && healSkill != null)
        {
            return healSkill;
        }

        if (
            magicDefenseSkill != null &&
            !enemy.IsMagicDefenseBuffed
        )
        {
            return magicDefenseSkill;
        }

        if (
            defenseSkill != null &&
            !enemy.IsDefenseBuffed
        )
        {
            return defenseSkill;
        }

        return null;
    }
}
