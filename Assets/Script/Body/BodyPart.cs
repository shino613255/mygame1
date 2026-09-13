using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PartType
{
    Face,
    Belly,
    Hand,
    Leg
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
    private int partHp;                                                     

    public bool IsBroken => canBreak && partHp <= 0;

    private void Awake()
    {
        partHp = maxPartHp;

        if (highlight == null)
            highlight = GetComponent<SpriteRenderer>();

        if (highlight != null)
            highlight.enabled = false;                                      
    }

    public string GetPartNameJP()                                           
    {
        switch (partType)
        {
            case PartType.Face: return "顔";
            case PartType.Belly: return "腹";
            case PartType.Hand: return "手";
            case PartType.Leg: return "脚";            
            default: return partType.ToString();
        }
    }

    private bool isSelected = false;
    private bool isPartViewVisible = false;


    public void SetSelectedVisual(bool selected)
    {
        isSelected = selected;
        RefreshVisual();
    }


    public void SetPartViewVisible(bool visible)
    {
        isPartViewVisible = visible;
        RefreshVisual();
    }


    private void RefreshVisual()
    {
        if (highlight == null)
            return;

        highlight.enabled =
            isSelected || isPartViewVisible;

        if (!highlight.enabled)
            return;

        // 選択中は白くする
        if (isSelected)
        {
            highlight.color = new Color(1f, 1f, 1f, 0.7f);
            return;
        }

        // 部位確認モード
        switch (partType)
        {
            case PartType.Face:
                highlight.color =
                    new Color(1f, 0.2f, 0.2f, 0.45f);
                break;

            case PartType.Belly:
                highlight.color =
                    new Color(1f, 0.85f, 0.2f, 0.45f);
                break;

            case PartType.Hand:
                highlight.color =
                    new Color(0.2f, 0.5f, 1f, 0.45f);
                break;

            case PartType.Leg:
                highlight.color =
                    new Color(0.2f, 1f, 0.4f, 0.45f);
                break;
        }
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

        // 腹部の場合は破壊不可能に設定する
        if (partType == PartType.Belly) canBreak = false;                   
    }
}
