using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct AttackContext     // 攻撃のコンテキスト（ダメージ計算に必要な情報をまとめる）
{
    public float baseDamage;        // 攻撃の基本ダメ（通常攻撃/スキルで決まる）
    public float mainDamageRate;    // 本体に入る倍率（例: 1.0）
    public float partDamageRate;    // 部位に入る倍率（例: 1.0）
    public bool canApplyStatus;     // 状態異常を乗せるか
    public SkillData sourceSkill;   // スキルなら入れる（通常攻撃ならnullでもOK）
}

