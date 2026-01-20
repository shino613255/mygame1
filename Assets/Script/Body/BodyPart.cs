using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PartType
{
    Face, Belly, RightHand, LeftHand, RightLeg, LeftLeg
}

public class BodyPart : MonoBehaviour
{
    [Header("Highlight")]
    [SerializeField] private SpriteRenderer highlight;

    [Header("Part Settings")]
    public PartType partType;
    public bool canBreak = true;

    [Header("Part HP")]
    [Min(1)] public int maxPartHp = 30;
    [SerializeField] private int partHp;

    public bool IsBroken => canBreak && partHp <= 0;

    private void Awake()
    {
        partHp = maxPartHp;

        if (highlight == null)
            highlight = GetComponent<SpriteRenderer>();

        if (highlight != null)
            highlight.enabled = false;
    }

    public void SetSelectedVisual(bool selected)
    {
        if (highlight == null) return;
        highlight.enabled = selected;
    }

    public void TakePartDamage(int damage)
    {
        if (damage <= 0) return;
        if (IsBroken) return;

        partHp -= damage;

        if (!canBreak)
        {
            if (partHp <= 0) partHp = 1;
            return;
        }

        if (partHp <= 0)
        {
            partHp = 0;
            // OnBroken() 後で追加
        }
    }

    private void OnBroken()
    {
        var enemy = GetComponentInParent<EnemyManager>();
        if (enemy != null) return;

        enemy.OnPartBroken(partType);
    }
    private void OnValidate()
    {
        if (maxPartHp < 1) maxPartHp = 1;
        if (partType == PartType.Belly) canBreak = false;
    }
}
