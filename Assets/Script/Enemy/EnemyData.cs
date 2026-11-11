using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Enemy/Enemy Data", fileName = "EnemyData_")]
public class EnemyData : ScriptableObject
{
    [System.Serializable]
    public class PartDamageMultiplier
    {
        public PartType partType;
        [Min(0.01f)] public float physicalMultiplier = 1f;
        [Min(0.01f)] public float magicMultiplier = 1f;
    }

    [Header("基本情報")]
    public string enemyName;
    public GameObject prefab; 

    [Header("基本ステータス")]
    public int maxHp = 1;
    public int maxMp = 0;

    public int at = 1;
    public int mag = 0;

    public int def = 0;
    public int mdef = 0;

    [Range(0f, 1f)]
    public float evasionRate = 0f;             

    [Header("パフォーマンス / 演出")]
    public GameObject damageEffect;    

    [Tooltip("使えるスキル一覧（任意）")]
    
    public List<SkillData> skillList = new();       

    [Header("攻撃予告")]
    public SkillData AnnouncementSkillName;

    [Header("部位別ダメージ倍率")]
    public List<PartDamageMultiplier> partDamageMultipliers = new();

    public float GetPartDamageMultiplier(PartType partType, SkillType attackType)
    {
        if (partDamageMultipliers == null) return 1f;
            

        foreach (PartDamageMultiplier entry in partDamageMultipliers)
        {
            if (entry == null || entry.partType != partType)
                continue;

            float multiplier = attackType == SkillType.Magic
                ? entry.magicMultiplier 
                : entry.physicalMultiplier;

            return multiplier > 0f ? multiplier : 1f;
        }

        return 1f;
    }

    [Header("敵の種類（強さランク）")]
    public EnemyType enemyType = EnemyType.Normal;  

    [Header("敵の役割（戦い方のタイプ）")]
    public EnemyRole role = EnemyRole.Attacker;
}
