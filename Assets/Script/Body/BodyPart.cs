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

    public string GetPartNameJP()
    {
        switch (partType)
        {
            case PartType.Face: return "顔";
            case PartType.Belly: return "腹";
            case PartType.RightHand: return "右手";
            case PartType.LeftHand: return "左手";
            case PartType.RightLeg: return "右脚";
            case PartType.LeftLeg: return "左脚";
            default: return partType.ToString();
        }
    }
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

    public int TakePartDamage(int damage)
    {
        if (damage <= 0) return 0;
        if (IsBroken) return 0;

        int before = partHp;
        partHp -= damage;

        if (!canBreak)
        {
            if (partHp <= 0) partHp = 1;
            Debug.Log($"[BodyPart] {GetPartNameJP()} の部位HP: {before} → {partHp}");
            return before - partHp;
        }

        if (partHp <= 0)
        {
            partHp = 0;
            OnBroken();
        }

        Debug.Log($"[BodyPart] {GetPartNameJP()} の部位HP: {before} → {partHp}");
        return before - partHp;
    }

    private void OnBroken()
    {
        var enemy = GetComponentInParent<EnemyManager>();
        if (enemy == null) return;

        enemy.OnPartBroken(partType);
    }
    private void OnValidate()
    {
        if (maxPartHp < 1) maxPartHp = 1;
        if (partType == PartType.Belly) canBreak = false;
    }
}
