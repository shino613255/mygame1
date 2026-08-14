using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public SkillData ChooseSkill (EnemyManager enemy)                                  
    {
        if (enemy == null || enemy.data == null) return null;

        List<SkillData> pool = new();                                                  

        if (enemy.data.attackSkill != null)                                             
            pool.Add(enemy.data.attackSkill);                                           

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

        pool.RemoveAll(s => s.mpCost > enemy.mp);                                       

        if (pool.Count == 0) return null;

        // 使用可能なHealスキルだけ抽出
        List<SkillData> healSkills =
           pool.FindAll(s => s.skillType == SkillType.Heal);                            

        float healChance = 0f;                                                         

        if (hpRate <= 0.2f)
        {
            healChance = 1f;
        }
        else if (hpRate <= 0.5f)
        {
            healChance = 0.4f;
        }
        else
        {
            healChance = 0f;
        }

        if (healSkills.Count > 0 && Random.value < healChance && enemy.mp >= healSkills[0].mpCost)                         
        {
            return healSkills[Random.Range(0, healSkills.Count)];
        }

        pool.RemoveAll(s => s.skillType == SkillType.Heal);                             

        if (pool.Count == 0) return null;                                              

        return pool[Random.Range(0, pool.Count)];                                      
    }
}