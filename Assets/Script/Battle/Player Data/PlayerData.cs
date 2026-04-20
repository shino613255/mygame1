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
    public int startMaxHp = 100;
    public int startMaxMp = 30;
    public int startAt = 10;
    [Range(0f, 1f)] public float damageCutRate = 0.1f;
    [Range(0f, 1f)] public float evasionRate = 0.05f;

    [Header("役職用の枠")]
    public int skillSlotCount = 3; // 役職ごとにスキルの枠を変えるための変数
    public int bodyEnhanceSlotCount = 2; // 役職ごとに体強化の枠を変えるための変数

    [Header("固有情報")]
    public string uniquePassiveName;
    [TextArea] public string uniquePassiveDescription;

    [Header("初期スキル所持（ID管理）")]
    public List<SkillData> startSkills = new();

}

public enum PlayerRole
{
    Warrior,
    Mage,
    Berserker
}
