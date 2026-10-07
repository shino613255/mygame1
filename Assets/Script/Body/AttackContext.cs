using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 1回の攻撃に必要なダメージ倍率・状態異常・クリティカル情報をまとめて渡す
public struct AttackContext
{
    public float baseDamage;
    public float mainDamageRate;
    public float partDamageRate;
    public bool canApplyStatus;
    public SkillData sourceSkill; 
    public bool isCritical;           
}
