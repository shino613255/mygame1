using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyPartsController : MonoBehaviour
{
    [SerializeField] private EnemyManager enemy;
    [SerializeField] private BodyPart selectedPart;

    private void Awake()
    {
        if (enemy == null)
            enemy = GetComponent<EnemyManager>();

        Debug.Log($"[EnemyPartsController.Awake] enemy={(enemy != null ? enemy.name : "null")}");
    }

    public void SetSelectedPart(BodyPart part)
    {
        selectedPart = part;
    }

    public void ApplyMainAndPartDamage(int damage)
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
