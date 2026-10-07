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
    }

    public void SetSelectedPart(BodyPart part)
    {
        selectedPart = part;                                                                                               
    }

    // 1回の攻撃で本体と部位に与えたダメージ結果
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
            int calculatedPartDamage = Mathf.RoundToInt(ctx.baseDamage * ctx.partDamageRate);

            result.partDamage = selectedPart.TakePartDamage(calculatedPartDamage);
        }
        return result;
    }    
}
