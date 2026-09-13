using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class EnemyAI : MonoBehaviour
{
    private Dictionary<EnemyRole, IEnemyRoleAction> roleActions;
    private TankBossRoleAction tankBossAction;

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

        pool = pool
            .Where(s => s != null)
            .Where(s => s.mpCost <= enemy.mp)
            .Where(s => cooldowns == null || cooldowns.IsReady(s))
            .ToList();


        if (pool.Count == 0) return null;

        float hpRate = (float)enemy.hp / enemy.maxHp;

        if (
            enemy.data.enemyType == EnemyType.Boss &&
            enemy.data.role == EnemyRole.Tank
        )
        {
            return tankBossAction.ChooseSkill(
                enemy,
                pool
            );
        }

        // í èÌÇÃñäÑ
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

    public void ResetBattleState()
    {
        tankBossAction?.ResetState();
    }
}