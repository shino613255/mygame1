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

    [Header("部位データ（任意）")]
    public List<EnemyPartData> parts = new(); // 既に使ってるならここでOK

    [Header("アクションAI（最小）")]
    [Tooltip("とりあえず通常攻撃に使うスキルID（未実装なら-1）")]
    public int attackSkillId = -1;

    [Tooltip("使えるスキル一覧（任意）")]
    public List<int> skillList = new();
}
