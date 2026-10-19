using System.Collections;
using System.Collections.Generic;

public class TankBossRoleAction : IEnemyRoleAction
{
    private readonly TankRoleAction tankRoleAction
        = new TankRoleAction();

    private bool bossGimmickUsed = false;
    private bool poisonTacticUnlocked = false;
    private bool heavyAttackUnlocked = false;
    private bool heavyAttackDeferred = false;
    private SkillData pendingHeavyAttack;
    private EnemyManager pendingHeavyAttackEnemy;

    public bool IsHeavyAttackUnlocked => heavyAttackUnlocked;
    public SkillData PendingHeavyAttack => pendingHeavyAttack;

    public SkillData ChooseSkill(
        EnemyManager enemy,
        List<SkillData> pool
    )
    {
        return ChooseSkill(enemy, pool, null);
    }

    public SkillData ChooseSkill(
        EnemyManager enemy,
        List<SkillData> pool,
        PlayerManager player,
        bool allowHeavyAttackStance = false
    )
    {
        var sealedSkills = GetSealedSkills(enemy);
        pool = pool.FindAll(skill => !sealedSkills.Contains(skill));

        float hpRate =
            (float)enemy.hp / enemy.maxHp;

        if (hpRate <= 0.75f)
            poisonTacticUnlocked = true;

        if (hpRate <= 0.35f)
            heavyAttackUnlocked = true;

        SkillData heavyAttack = pool.Find(skill =>
            skill == enemy.data.telegraphedSkill &&
            skill.skillType == SkillType.Physical);
        pool = pool.FindAll(skill => skill != enemy.data.telegraphedSkill);

        if (allowHeavyAttackStance && pendingHeavyAttack != null)
            return null;

        SkillData poisonSkill =
            player != null && player.IsAlive && !player.IsPoisoned
                ? pool.Find(s => s.statusEffect != null &&
                    s.statusEffect.type == StatusEffectType.Poison)
                : null;

        // Boss専用ギミックはHP50%以下で1度だけ発動する
        if (
            hpRate <= 0.5f &&
            !bossGimmickUsed
        )
        {
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

        SkillData tankSkill = tankRoleAction.ChooseSkill(
            enemy,
            pool
        );
        SkillData fallbackSkill = tankSkill != null
            ? tankSkill
            : poisonTacticUnlocked ? poisonSkill : null;

        if (allowHeavyAttackStance && heavyAttackUnlocked && heavyAttack != null &&
            enemy.IsAlive && player != null && player.IsAlive)
        {
            if (fallbackSkill != null && !heavyAttackDeferred)
            {
                heavyAttackDeferred = true;
                return fallbackSkill;
            }

            pendingHeavyAttack = heavyAttack;
            pendingHeavyAttackEnemy = enemy;
            heavyAttackDeferred = false;
            return null;
        }

        return fallbackSkill;
    }

    public static List<SkillData> GetSealedSkills(EnemyManager enemy)
    {
        var sealedSkills = new List<SkillData>();
        if (enemy == null || enemy.data == null ||
            enemy.data.enemyType != EnemyType.Boss || enemy.data.role != EnemyRole.Tank)
            return sealedSkills;

        int brokenHands = 0;
        bool headBroken = false;
        foreach (BodyPart part in enemy.GetComponentsInChildren<BodyPart>(true))
        {
            if (!part.IsBroken)
                continue;

            if (part.partType == PartType.Hand)
                brokenHands++;
            else if (part.partType == PartType.Face)
                headBroken = true;
        }

        var skills = new List<SkillData>();
        if (enemy.data.attackSkill != null)
            skills.Add(enemy.data.attackSkill);
        if (enemy.data.skillList != null)
            skills.AddRange(enemy.data.skillList);

        foreach (SkillData skill in skills)
        {
            if (skill == null || sealedSkills.Contains(skill))
                continue;

            bool poisonSealed = brokenHands >= 2 && skill.statusEffect != null &&
                skill.statusEffect.type == StatusEffectType.Poison;
            bool magicGuardSealed = brokenHands > 0 && skill.skillType == SkillType.Buff &&
                skill.buff != null && skill.buff.type == BuffType.MagicDefenseUp;
            bool healSealed = headBroken && skill.skillType == SkillType.Heal;
            bool heavyAttackSealed = brokenHands >= 2 && skill == enemy.data.telegraphedSkill &&
                skill.skillType == SkillType.Physical;

            if (poisonSealed || magicGuardSealed || healSealed || heavyAttackSealed)
                sealedSkills.Add(skill);
        }

        return sealedSkills;
    }

    public SkillData TakePendingHeavyAttack(EnemyManager enemy)
    {
        SkillData skill = enemy != null && pendingHeavyAttackEnemy == enemy ? pendingHeavyAttack : null;
        pendingHeavyAttack = null;
        pendingHeavyAttackEnemy = null;
        return skill;
    }

    public void ResetState()
    {
        bossGimmickUsed = false;
        poisonTacticUnlocked = false;
        heavyAttackUnlocked = false;
        heavyAttackDeferred = false;
        pendingHeavyAttack = null;
        pendingHeavyAttackEnemy = null;
    }
}
