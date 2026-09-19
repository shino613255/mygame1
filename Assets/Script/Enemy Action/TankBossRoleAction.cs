using System.Collections;
using System.Collections.Generic;

public class TankBossRoleAction : IEnemyRoleAction
{
    private readonly TankRoleAction tankRoleAction
        = new TankRoleAction();

    private bool bossGimmickUsed = false;

    public SkillData ChooseSkill(
        EnemyManager enemy,
        List<SkillData> pool
    )
    {
        float hpRate =
            (float)enemy.hp / enemy.maxHp;

        // BossêÍópÅFHP50%à»â∫Ç≈1âÒÇæÇØ
        if (
            hpRate <= 0.5f &&
            !bossGimmickUsed
        )
        {
            SkillData poisonSkill =
                pool.Find(
                    s =>
                        s.statusEffect != null &&
                        s.statusEffect.type ==
                            StatusEffectType.Poison
                );

            if (poisonSkill != null)
            {
                bossGimmickUsed = true;

                return poisonSkill;
            }
            SkillData healSkill =
                pool.Find(
                    s => s.skillType == SkillType.Heal
                );

            if (healSkill != null)
            {
                bossGimmickUsed = true;
                return healSkill;
            }

            SkillData defenseSkill =
                pool.Find(
                    s =>
                        s.skillType == SkillType.Buff &&
                        s.buff != null &&
                        s.buff.type == BuffType.DefenseUp
                );

            if (defenseSkill != null && !enemy.IsDefenseBuffed)
            {
                bossGimmickUsed = true;
                return defenseSkill;
            }

            SkillData magicDefenseSkill =
                pool.Find(
                    s =>
                        s.skillType == SkillType.Buff &&
                        s.buff != null &&
                        s.buff.type ==
                            BuffType.MagicDefenseUp
                );

            if (
                magicDefenseSkill != null &&
                !enemy.IsMagicDefenseBuffed
            )
            {
                bossGimmickUsed = true;
                return magicDefenseSkill;
            }
        }

        // í èÌéûÇÕïÅí ÇÃTankÇ∆ìØÇ∂
        return tankRoleAction.ChooseSkill(
            enemy,
            pool
        );
    }

    public void ResetState()
    {
        bossGimmickUsed = false;
    }
}