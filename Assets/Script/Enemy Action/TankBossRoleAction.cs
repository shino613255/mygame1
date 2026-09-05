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

        // HP50Åìà»â∫ Å{ ÉMÉ~ÉbÉNñ¢égóp
        if (
            hpRate <= 0.5f &&
            !bossGimmickUsed
        )
        {
            // á@ âÒïúÇç≈óDêÊ
            SkillData healSkill =
                pool.Find(s =>
                    s.skillType == SkillType.Heal
                );

            if (healSkill != null)
            {
                bossGimmickUsed = true;
                return healSkill;
            }

            // áA âÒïúÇ≈Ç´Ç»ÇØÇÍÇŒñhå‰UP
            SkillData defenseSkill =
                pool.Find(s =>
                    s.skillType == SkillType.Buff &&
                    s.buff != null &&
                    s.buff.type == BuffType.DefenseUp
                );

            if (defenseSkill != null)
            {
                bossGimmickUsed = true;
                return defenseSkill;
            }

            // áB ÇªÇÍÇ‡Ç»ÇØÇÍÇŒñÇñ@ñhå‰UP
            SkillData magicDefenseSkill =
                pool.Find(s =>
                    s.skillType == SkillType.Buff &&
                    s.buff != null &&
                    s.buff.type == BuffType.MagicDefenseUp
                );

            if (magicDefenseSkill != null)
            {
                bossGimmickUsed = true;
                return magicDefenseSkill;
            }
        }

        // ÉMÉ~ÉbÉNà»äOÇÕïÅí ÇÃTank AI
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