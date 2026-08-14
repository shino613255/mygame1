using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Enemy/Enemy Data", fileName = "EnemyData_")]
public class EnemyData : ScriptableObject
{
    [Header("基本情報")]
    public string enemyName;
    public Sprite icon;
    public GameObject prefab; 

    [Header("基本ステータス")]
    public int maxHp = 10;
    public int maxMp = 0;

    public int at = 1;
    public int def = 0;

    public int mag = 0;
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

    [Header("敵の種類（強さランク）")]
    // 火傷ダメージの倍率計算などに使用中。
    public EnemyType enemyType = EnemyType.Normal;  

    [Header("敵の役割（戦い方のタイプ）")]
    public EnemyRole role = EnemyRole.Attacker;
}
