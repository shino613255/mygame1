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
        public float damageMultiplier;
    }
    public AttackResult ApplyAttack(AttackContext ctx)                                                                      
    {
        AttackResult result = new AttackResult { damageMultiplier = 1f };

        if (enemy == null) return result;                                                           
        if (ctx.baseDamage <= 0) return result;                                                                             

        if (enemy.data != null && selectedPart != null)
        {
            SkillType attackType = ctx.sourceSkill != null ? ctx.sourceSkill.skillType : SkillType.Physical;
            result.damageMultiplier = enemy.data.GetPartDamageMultiplier(selectedPart.partType, attackType);
        }

        float adjustedDamage = ctx.baseDamage * result.damageMultiplier;
        result.mainDamage = Mathf.RoundToInt(adjustedDamage * ctx.mainDamageRate);
        result.mainDamage = enemy.TakeDirectDamage(result.mainDamage);

        if (selectedPart != null)
        {
            float focusedMultiplier = ctx.focusedPartDamageMultiplier > 0f
                ? ctx.focusedPartDamageMultiplier
                : 1f;
            int calculatedPartDamage = Mathf.RoundToInt(
                adjustedDamage * ctx.partDamageRate * focusedMultiplier *
                (ctx.preparedPartDamageMultiplier > 0f ? ctx.preparedPartDamageMultiplier : 1f)
            );

            result.partDamage = selectedPart.TakePartDamage(calculatedPartDamage);
        }
        return result;
    }    
}
