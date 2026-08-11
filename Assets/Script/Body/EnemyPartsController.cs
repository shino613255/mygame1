using System.Collections;
using System.Collections.Generic;
//using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class EnemyPartsController : MonoBehaviour
{
    private EnemyManager enemy;
    private BodyPart selectedPart;

    private void Awake()
    {
        if (enemy == null)
            enemy = GetComponent<EnemyManager>();

        Debug.Log($"[EnemyPartsController.Awake] enemy={(enemy != null ? enemy.data.enemyName : "null")}");
    }

    public void SetSelectedPart(BodyPart part)
    {
        selectedPart = part;                                                                                                // BodyPartにある部位をselectedPartに設定する
    }
    public struct AttackResult                                                                                              // 攻撃結果をひとまとめにする構造体
    {
        public int mainDamage;
        public int partDamage;
    }
    public AttackResult ApplyAttack(AttackContext ctx)                                                                      // 今回の攻撃結果を記録する箱
    {
        AttackResult result = new AttackResult();                                                                           // 攻撃結果を初期化

        if (enemy == null) return result;                                                           
        if (ctx.baseDamage <= 0) return result;                                                                             // ダメージが0以下の場合は何もしない

        result.mainDamage = Mathf.RoundToInt(ctx.baseDamage * ctx.mainDamageRate);                                          // 本体ダメージを計算(小数は四捨五入する)
        enemy.TakeDamageRaw(result.mainDamage);                                                                             // 本体ダメージを実行し、与える

        if (selectedPart != null)
        {
            result.partDamage = Mathf.RoundToInt(ctx.baseDamage * ctx.partDamageRate);                                      // 部位ダメージを計算(小数は四捨五入する)
            selectedPart.TakePartDamage(result.partDamage);                                                                 // 部位ダメージを実行し、与える           

        }

        Debug.Log(
            $"[ApplyAttack] 本体ダメージ={result.mainDamage}, " +
            $"部位ダメージ={result.partDamage}, " +
            $"selectedPart={(selectedPart != null ? selectedPart.GetPartNameJP() : "なし")}");

        return result;
    }    
}
