using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Skill/Skill Data", fileName = "Skill_")]
public class SkillData : ScriptableObject
{
    [Header("基本")]
    public string skillName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("コスト（任意）")]
    [Min(0)] public int mpCost = 0;
    [Min(0)] public int cooldown = 0;

    [Header("種別")]
    public SkillType skillType = SkillType.Physical;

    [Header("エフェクト")]
    public GameObject effectPrefab;
    public float effectDuration = 1.5f;
    public Vector2 effectOffset;

    [Header("威力")]
    [Tooltip("固定加算ダメージ/回復（使わないなら0でOK）")]
    public int power = 0;

    [Tooltip("atk/magに掛ける倍率（通常攻撃なら1.0）")]
    public float multiplier = 1f;

    [Header("命中率（0〜1）")]
    [Range(0f, 1f)]
    public float accuracy = 1f;

    [Header("クリティカル（任意）")]
    public bool canCrit = true;

    [Tooltip("クリ率の追加（0〜1）。例：0.1=+10%")]
    [Range(0f, 1f)]
    public float critBonus = 0f;

    [Header("属性")]
    public ElementType element = ElementType.None;

    [Header("対象")]
    public TargetType targetType = TargetType.Single;

    [Header("部位攻撃設定")]
    [Tooltip("このスキルで部位を選んで攻撃できるか")]
    public bool canTargetPart = true;

    [Tooltip("本体に入るダメージ倍率")]
    public float mainDamageRate = 1f;

    [Tooltip("部位に入るダメージ倍率")]
    public float partDamageRate = 1f;

    [Header("追加効果（状態異常）")]
    public StatusEffectData statusEffect;      // 付与する状態
    [Range(0f, 1f)] public float applyChance = 0.0f;  // 付与確率（スキル側で上書き）
    [Min(0)] public int overrideDurationTurns = 0;     // 0ならStatusEffectDataのdurationを使う
}
