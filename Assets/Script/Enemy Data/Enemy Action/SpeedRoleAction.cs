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
        {
            return null;
        }

        // 40%の確率でスキルを使用
        if (Random.value < 0.4f)
        {
            return pool[
                Random.Range(
                    0,
                    pool.Count
                )
            ];
        }

        // スキルを使用しない場合は通常攻撃
        return null;
    }
}