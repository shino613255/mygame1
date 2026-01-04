using System.Collections;
using System.Collections.Generic;
// EnemyPartData.cs
using UnityEngine;

[CreateAssetMenu(
    menuName = "Game/Enemy/Enemy Part Data",
    fileName = "EnemyPartData_"
)]
public class EnemyPartData : ScriptableObject
{    
    public StatusEffectData statusOnHit;

    [Header("部位")]
    public BodyPart partId;
    public string partName;

    [Header("命中補正")]
    [Tooltip("武器補正・欠損時にも使用")]
    public float hitBonus = 0f;

    [Header("追加効果（直接付与）")]

    [Header("属性効果（5%で発動）")]
    public ElementType element = ElementType.None;

    [Range(0f, 1f)]
    public float elementProcChance = 0.05f;

    [Header("部位特性")]
    public bool canBeBroken = true;
}
