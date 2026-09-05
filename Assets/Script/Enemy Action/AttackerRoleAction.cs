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
        List<SkillData> attackSkills =
            pool.FindAll(s =>
                s.skillType == SkillType.Physical ||
                s.skillType == SkillType.Magic
            );

        if (attackSkills.Count == 0)
            return null;

        return attackSkills[
            Random.Range(0, attackSkills.Count)
        ];
    }
}