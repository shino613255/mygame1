using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Player/Player Data", fileName = "PlayerData_")]
public class PlayerData : ScriptableObject
{
    [Header("基本情報")]
    public string playerName;

    [Header("初期ステータス")]
    public int startMaxHp = 100;
    public int startMaxMp = 30;
    public int startAt = 10;
    public int startDef = 5;
    public int startMag = 5;

    [Header("初期スキル所持（ID管理）")]
    public List<int> startSkillIds = new();

    [Header("初期装備（ID管理：後でItemDataに差し替え）")]
    public List<int> startEquipItemIds = new();
}

[System.Serializable]
public class LevelGrowth
{
    public int addHp;
    public int addMp;
    public int addAt;
    public int addDef;
    public int addMag;
}
public enum PlayerRole
{
    Warrior,
    Mage,
    Berserker
}
