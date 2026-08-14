using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct AttackContext
{
    public float baseDamage;
    public float mainDamageRate;
    public float partDamageRate;
    public bool canApplyStatus;
    public SkillData sourceSkill;   // 攻撃の元となるスキルデータ
}
