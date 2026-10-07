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

        // 現在のプレイヤーは魔法攻撃主体のためDefenseUpは優先しない。
        // 物理攻撃主体のプレイヤー追加後に再導入予定。

        if (
            !enemy.IsMagicDefenseBuffed &&
            magicDefenseSkill != null
        )
        {
            return magicDefenseSkill;
        }

        // Tank型は低HP時のみ回復を選択肢に入れる
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

        // 何もする必要がなければ通常攻撃
        return null;
    }
}