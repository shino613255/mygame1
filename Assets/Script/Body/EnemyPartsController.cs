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
        selectedPart = part;                                                                                               
    }
    public struct AttackResult                                                                                              
    {
        public int mainDamage;
        public int partDamage;
    }
    public AttackResult ApplyAttack(AttackContext ctx)                                                                      
    {
        AttackResult result = new AttackResult();                                                                          

        if (enemy == null) return result;                                                           
        if (ctx.baseDamage <= 0) return result;                                                                             

        result.mainDamage = Mathf.RoundToInt(ctx.baseDamage * ctx.mainDamageRate);                                              
        enemy.TakeDamageRaw(result.mainDamage);                                                                             

        if (selectedPart != null)
        {
            result.partDamage = Mathf.RoundToInt(ctx.baseDamage * ctx.partDamageRate);                                      
            selectedPart.TakePartDamage(result.partDamage);                                                                 

        }

        Debug.Log(
            $"[ApplyAttack] 本体ダメージ={result.mainDamage}, " +
            $"部位ダメージ={result.partDamage}, " +
            $"selectedPart={(selectedPart != null ? selectedPart.GetPartNameJP() : "なし")}");

        return result;
    }    
}
