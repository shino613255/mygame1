using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class EnemyAI : MonoBehaviour
{
    private Dictionary<EnemyRole, IEnemyRoleAction> roleActions;
    private TankBossRoleAction tankBossAction;

    public bool IsBossHeavyAttackUnlocked => tankBossAction?.IsHeavyAttackUnlocked ?? false;
    public SkillData PendingBossHeavyAttack => tankBossAction?.PendingHeavyAttack;

    private void Awake()
    {
        tankBossAction = new TankBossRoleAction();

        roleActions =
            new Dictionary<EnemyRole, IEnemyRoleAction>
            {
            {
                EnemyRole.Attacker,
                new AttackerRoleAction()
            },
            {
                EnemyRole.Tank,
                new TankRoleAction()
            },
            {
                EnemyRole.Speed,
                new SpeedRoleAction()
            }
            };
    }

    public SkillData ChooseSkill (EnemyManager enemy)                                  
    {
        return ChooseSkill(enemy, 0);
    }

    public SkillData ChooseSkill(EnemyManager enemy, int cooldownTicksBeforeAction)
    {
        return ChooseSkill(enemy, cooldownTicksBeforeAction, null);
    }

    public SkillData ChooseSkill(EnemyManager enemy, int cooldownTicksBeforeAction, PlayerManager player,
        bool allowBossHeavyAttackStance = false)
    {
        if (enemy == null || enemy.data == null) return null;

        List<SkillData> pool = new();                                                  

        if (enemy.data.attackSkill != null)
        {
            pool.Add(enemy.data.attackSkill);
        }

        if (enemy.data.skillList != null)
        {
            pool.AddRange(enemy.data.skillList);
        }

        SkillCooldowns cooldowns =
            enemy.GetComponent<SkillCooldowns>();

        // 使用可能なスキルだけをAIの選択候補に残す
        pool = pool
            .Where(s => s != null)
            .Where(s => s.mpCost <= enemy.mp)
            .Where(s => cooldowns == null || cooldowns.IsReadyAfterTicks(s, cooldownTicksBeforeAction))
            .ToList();


        // Tank Bossは通常のTank AIではなく、専用の行動ロジックを使う
        if (
            enemy.data.enemyType == EnemyType.Boss &&
            enemy.data.role == EnemyRole.Tank
        )
        {
            return tankBossAction.ChooseSkill(
                enemy,
                pool,
                player,
                allowBossHeavyAttackStance
            );
        }

        if (pool.Count == 0) return null;

        if (enemy.data.role == EnemyRole.Caster || enemy.data.role == EnemyRole.Brute || enemy.data.role == EnemyRole.Warden)
            return ExpansionEnemyAction.Choose(enemy, pool, player);

        if (
            roleActions != null &&
            roleActions.TryGetValue(
                enemy.data.role,
                out IEnemyRoleAction roleAction
            )
        )
        {
            return roleAction.ChooseSkill(
                enemy,
                pool
            );
        }

        return null;
    }

    public SkillData TakePendingBossHeavyAttack(EnemyManager enemy)
    {
        return tankBossAction?.TakePendingHeavyAttack(enemy);
    }

    public void ResetBattleState()
    {
        tankBossAction?.ResetState();
    }
}
