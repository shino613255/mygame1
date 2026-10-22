using System.Collections;
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
    public Sprite icon;
    public GameObject prefab; 

    [Header("基本ステータス")]
    public int maxHp = 10;
    public int maxMp = 0;

    public int at = 1;
    public int mag = 0;

    public int def = 0;
    public int mdef = 0;

    [Range(0f, 1f)]
    public float evasionRate = 0f;             

    [Header("パフォーマンス / 演出")]
    public GameObject damageEffect;
    // TODO: 死亡時のエフェクトを設定する
    public GameObject deathEffect;                  
    public AudioClip hitSE;
    public AudioClip deathSE;

    [Tooltip("使えるスキル一覧（任意）")]
    
    public SkillData attackSkill;                  
    public List<SkillData> skillList = new();       

    [Header("Attack Telegraph")]
    public SkillData telegraphedSkill;

    [Header("Weak Point Stagger")]
    public bool enableStagger;
    public PartType staggerWeakPoint = PartType.Face;
    [Min(1)] public int staggerGaugeMax = 100;
    [Min(1)] public int staggerGainPerHit = 35;
    [Min(1)] public int staggerRecoveryTurns = 1;

    [Header("Part Damage Multipliers")]
    public List<PartDamageMultiplier> partDamageMultipliers = new();

    public float GetPartDamageMultiplier(PartType partType, SkillType attackType)
    {
        if (partDamageMultipliers == null ||
            (attackType != SkillType.Physical && attackType != SkillType.Magic))
            return 1f;

        foreach (PartDamageMultiplier entry in partDamageMultipliers)
        {
            if (entry == null || entry.partType != partType)
                continue;

            float multiplier = attackType == SkillType.Magic
                ? entry.magicMultiplier : entry.physicalMultiplier;
            return multiplier > 0f && !float.IsInfinity(multiplier) ? multiplier : 1f;
        }

        return 1f;
    }

    [Header("敵の種類（強さランク）")]
    // 敵の強さ区分やBoss判定など、種類ごとの処理分岐に使用する
    public EnemyType enemyType = EnemyType.Normal;  

    [Header("敵の役割（戦い方のタイプ）")]
    public EnemyRole role = EnemyRole.Attacker;
}
