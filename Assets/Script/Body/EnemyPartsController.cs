using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class EnemyPartsController : MonoBehaviour
{
    [SerializeField] private EnemyManager enemy;
    [SerializeField] private BodyPart selectedPart;

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

        // 本体にダメージ
        result.mainDamage = Mathf.RoundToInt(ctx.baseDamage * ctx.mainDamageRate);
        enemy.TakeDamageRaw(result.mainDamage);

        // 部位にダメージ
        if (selectedPart != null)
        {
            int rawPartDamage = Mathf.RoundToInt(ctx.baseDamage * ctx.partDamageRate);
            result.partDamage = selectedPart.TakePartDamage(rawPartDamage);
        }

        Debug.Log($"[ApplyAttack] 本体ダメージ={result.mainDamage}, 部位ダメージ={result.partDamage}, selectedPart={(selectedPart != null ? selectedPart.GetPartNameJP() : "なし")}");
        return result;
    }
    public void ApplyMainAndPartDamage(int damage)      // 本体と部位両方にダメージを与える
    {
        Debug.Log($"[ApplyMainAndPartDamage] damage={damage} enemy={(enemy != null)} selectedPart={(selectedPart != null ? selectedPart.partType.ToString() : "null")}");

        if (enemy == null) return;
        if (damage <= 0) return;

        // 本体にダメージ
        int before = enemy.hp;
        enemy.TakeDamageRaw(damage);
        Debug.Log($"[MainHP] {before} -> {enemy.hp}");

        // 部位にダメージ
        if (selectedPart != null)
        {
            selectedPart.TakePartDamage(damage);
        }
    }

    
}
