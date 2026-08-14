using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Player/Player Data", fileName = "PlayerData_")]
public class PlayerData : ScriptableObject
{
    [Header("基本情報")]
    public string playerName;
    public PlayerRole role;

    [Header("初期ステータス")]
    public int startMaxHp;
    public int startMaxMp;
    public int startAt;
    public int startMag;
    public int startDef;
    public int startMdef;
    [Range(0f, 1f)] public float damageCutRate;
    [Range(0f, 1f)] public float evasionRate;

    [Header("役職用の枠")]
    public int skillSlotCount = 3;                                                              
    public int bodyEnhanceSlotCount = 2;                                                        // 役職ごとに体強化の枠を変えるための変数

    [Header("固有情報")]
    public string uniquePassiveName;                                                            // 固有パッシブスキルの名前
    [TextArea] public string uniquePassiveDescription;                                          // 固有パッシブスキルの説明文

    [Header("初期スキル所持（ID管理）")]
    public List<SkillData> startSkills = new();

}

public enum PlayerRole
{
    Warrior,
    Mage,
    Berserker
}
