using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

        if (enemy.data.skillList != null && enemy.data.skillList.Count > 0)             
        {
            foreach (var s in enemy.data.skillList)                                     
            {
                if (s == null) continue;
                
                pool.Add(s);                                                            
            }
        }

        if (pool.Count == 0) return null;                                               
        float hpRate = (float)enemy.hp / enemy.maxHp;

        pool.RemoveAll(
            s => s.mpCost > enemy.mp
        );

        SkillCooldowns cooldowns =
            enemy.GetComponent<SkillCooldowns>();

        if (cooldowns != null)
        {
            pool.RemoveAll(s => !cooldowns.IsReady(s));
        }

        if (pool.Count == 0) return null;

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