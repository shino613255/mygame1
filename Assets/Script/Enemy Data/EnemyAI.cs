using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public SkillData ChooseSkill (EnemyManager enemy)                                   // 敵のAIが使用するスキルを選ぶメソッド
    {
        if (enemy == null || enemy.data == null) return null;

        List<SkillData> pool = new();                                                   // スキルの候補に空のリスト

        if (enemy.data.attackSkill != null)                                             // 通常攻撃スキルが設定されている場合
            pool.Add(enemy.data.attackSkill);                                           // 通常攻撃スキルを候補に追加

        if (enemy.data.skillList != null && enemy.data.skillList.Count > 0)             // スキルリストが設定されている場合
        {
            foreach (var s in enemy.data.skillList)                                     // スキルリストの中身を1つずつ確認
            {
                if (s == null) continue;                                                // nullのスキルはパスする
                pool.Add(s);                                                            // スキルを候補に追加
            }
        }

        if (pool.Count == 0) return null;                                               // 候補が0の場合はnullを返す

        float hpRate = (float)enemy.hp / enemy.maxHp;                                   // 敵のHP割合を計算      

        pool.RemoveAll(s => s.mpCost > enemy.mp);                                       // MPが足りないスキルを候補から消す

        if (pool.Count == 0) return null;

        List<SkillData> healSkills =
           pool.FindAll(s => s.skillType == SkillType.Heal);                            // HP割合が条件を満たすスキルだけを残す

        float healChance = 0f;                                                          // Healを使う確率を初期化

        if (hpRate <= 0.1f)
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

        if (healSkills.Count > 0 && Random.value < healChance && enemy.mp >= healSkills[0].mpCost)                          // Healを使う判定
        {
            return healSkills[Random.Range(0, healSkills.Count)];
        }

        pool.RemoveAll(s => s.skillType == SkillType.Heal);                             // Healを選ばなかった場合、候補からHealを消す

        if (pool.Count == 0) return null;                                               // 候補が0の場合はnullを返す

        return pool[Random.Range(0, pool.Count)];                                       // 候補の中からランダムで1つ返す
    }
}