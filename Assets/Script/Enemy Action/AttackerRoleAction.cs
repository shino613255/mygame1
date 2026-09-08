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

        // HP50%à»â∫Ç»ÇÁ50%Ç≈âÒïú
        if (
            hpRate <= 0.5f &&
            healSkill != null &&
            Random.value < 0.5f
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

        // í èÌçUåÇ
        return null;
    }
}