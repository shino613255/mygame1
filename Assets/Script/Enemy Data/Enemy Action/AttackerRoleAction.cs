using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackerRoleAction : IEnemyRoleAction
{
    public SkillData ChooseSkill(
        EnemyManager enemy,
        List<SkillData> pool
    )
    {
        float hpRate =
            (float)enemy.hp / enemy.maxHp;

        SkillData healSkill =
            pool.Find(
                s => s.skillType == SkillType.Heal
            );

        // HP50%以下なら一定確率で回復
        if (
            hpRate <= 0.5f && 
            healSkill != null &&
            Random.value < 0.3f 
        )
        {
            return healSkill;
        }

        List<SkillData> attackSkills =
            pool.FindAll(
                s =>
                    s.skillType == SkillType.Physical ||
                    s.skillType == SkillType.Magic
            );

        if (attackSkills.Count > 0)
        {
            return attackSkills[
                Random.Range(
                    0,
                    attackSkills.Count
                )
            ];
        }

        // 使用できる攻撃スキルがない場合は通常攻撃
        return null;
    }
}