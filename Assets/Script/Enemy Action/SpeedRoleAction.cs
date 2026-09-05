using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpeedRoleAction : IEnemyRoleAction
{
    public SkillData ChooseSkill(
        EnemyManager enemy,
        List<SkillData> pool
    )
    {
        if (pool == null || pool.Count == 0)
            return null;

        return pool[
            Random.Range(0, pool.Count)
        ];
    }
}