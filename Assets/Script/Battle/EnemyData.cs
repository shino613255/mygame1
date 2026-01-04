using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Enemy/Enemy Data", fileName = "EnemyData_")]
public class EnemyData : ScriptableObject
{
    [Header("基本情報")]
    public string enemyName;
    public Sprite icon;
    public GameObject prefab; // 必要なら配置用

    [Header("基本ステータス")]
    public int maxHp = 10;
    public int maxMp = 0;

    public int at = 1;
    public int def = 0;

    public int mag = 0;
    public int mdef = 0;

    public int spd = 1;

    [Header("報酬")]
    public int expReward = 0;
    public int goldReward = 0;
    public DropTable dropTable; // 任意（あとで作る）

    [Header("パフォーマンス / 演出")]
    public GameObject damageEffect;
    public GameObject deathEffect; // 将来用（今は未使用でもOK）
    public AudioClip hitSE;
    public AudioClip deathSE;

    [Tooltip("使えるスキル一覧（任意）")]
    
    public SkillData attackSkill;                 // 通常攻撃（保険）
    public List<SkillData> skillList = new();    // ここに最大4つ入れる（Inspector）

}
