using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static/*ヒエラルキービューでオブジェクトを作らなくてよい*/ class SkillExecutor
{
    public struct Result
    {
        public bool executed;       
        public bool hit;            
        public bool crit;           
        public int value;           // ダメージ量や回復量などの値
        public string message;      
    }

    public static Result Execute(
        UnitBase attacker,          　　　　　　　　　　　　                                                                                                
        UnitBase target,                                                                                                                                   
        SkillData skill,                                                                                                                                   
        float targetEvasion = 0f                                                                                                                            
        )
    {
        var r = new Result
        {                                                                                                                                                   
            executed = false, 
            hit = false, 
            crit = false, 
            value = 0, 
            message = "" 
        };

        if (attacker == null || target == null || skill == null) return r;

        var cds = attacker.GetComponent<SkillCooldowns>();                                                                                                  

        if (cds != null && !cds.IsReady(skill))                                                                                                             
        {
            r.message = $"{skill.skillName} はクールダウン中！";
            return r;
        }

        if (attacker.mp < skill.mpCost)
        {
            r.message = $"{skill.skillName} を使うMPが足りない！";
            return r;
        }

        attacker.mp -= skill.mpCost;                                                                                                                                
        r.executed = true;                                                                                                                                  

        bool needHitCheck = (skill.skillType == SkillType.Physical || skill.skillType == SkillType.Magic || skill.skillType == SkillType.Debuff);           

        if (!needHitCheck || DamageRule.RollHit(skill.accuracy, targetEvasion))                                                                             
        {
            r.hit = true;
        }
        else
        {
            r.hit = false;
            r.message = $"{attacker.name}の{skill.skillName}！\nしかし外れた！";
            if (cds != null) cds.StartCooldown(skill);                                                                                                                                                                                                       
            return r;
        }

        switch (skill.skillType)
        {
            case SkillType.Physical:
                {
                    // 基礎ダメージにスキル固有の固定値を加算
                    int baseDmg = DamageRule.CalcPhysical(attacker.at, target.def, skill.multiplier, 1) + skill.power;                                      

                    if (skill.canCrit)
                    {
                        // 基礎クリティカル率3%にスキル固有の補正を加える
                        float rate = Mathf.Clamp01(0.03f + skill.critBonus);
                        // クリティカル時は1.5倍
                        int after = DamageRule.RollCrit(baseDmg, rate, 1.5f, 1);                                                                           
                        r.crit = after != baseDmg;
                        baseDmg = after;
                    }

                    r.value = target.TakePhysical(baseDmg);                                                                                                 
                    r.message = $"{attacker.name}の{skill.skillName}！\n{r.value}ダメージ！";
                    break;
                }
            case SkillType.Magic:
                {
                    int baseDmg = DamageRule.CalcMagic(attacker.mag, target.mdef, skill.multiplier, 1) + skill.power;                                      

                    if (skill.canCrit)
                    {
                        float rate = Mathf.Clamp01(0.03f + skill.critBonus);
                        int after = DamageRule.RollCrit(baseDmg, rate, 1.5f, 1);
                        r.crit = after != baseDmg;
                        baseDmg = after;
                    }

                    r.value = target.TakeMagic(baseDmg);                                                                                                   
                    r.message = $"{attacker.name}の{skill.skillName}！\n{r.value}ダメージ！";
                    break;
                }
            case SkillType.Heal:
                {
                    // 回復量 = 固定値 + MAG × スキル倍率（最低1）
                    int heal = Mathf.Max(1, skill.power + Mathf.RoundToInt(attacker.mag * skill.multiplier));                                               
                    r.value = Heal(target, heal);                                                                                                           
                    r.message = $"{attacker.name}の{skill.skillName}！\n{r.value}回復！";
                    break;
                }
            case SkillType.Buff:
                r.message = $"{skill.skillName}!";
                ApplyBuff(target, skill, ref r);
                break;
            case SkillType.Debuff:
                {
                    r.message = $"{attacker.name}の{skill.skillName}！";
                    ApplyDebuff(target, skill, ref r);
                    break;
                }
        }

        ApplyStatusEffect(attacker, target, skill, ref r);

        if (cds != null) cds.StartCooldown(skill);

        return r;
    }
    private static void ApplyBuff(
    UnitBase target,
    SkillData skill,
    ref Result r)
    {
        if (skill.buff == null)
            return;

        if (target is not EnemyManager enemy)
            return;

        switch (skill.buff.type)
        {
            case BuffType.DefenseUp:
                enemy.ApplyDefenseBuff(
                    skill.buff,
                    skill.overrideDurationTurns
                );

                r.message += "\n防御力が上がった！";
                break;

            case BuffType.MagicDefenseUp:
                enemy.ApplyMagicDefenseBuff(
                    skill.buff,
                    skill.overrideDurationTurns
                );

                r.message += "\n魔法防御力が上がった！";
                break;
        }
    }

    private static void ApplyDebuff(
    UnitBase target,
    SkillData skill,
    ref Result r)
    {
        if (skill.debuff == null)
            return;

        if (Random.value > skill.applyChance)
        {
            r.message += "\nデバフは効かなかった！";
            return;
        }

        if (target is not EnemyManager enemy)
            return;

        switch (skill.debuff.type)
        {
            case DebuffType.Deficiency:
                r.message += "\n欠損させた！";
                break;
        }
    }

    private static void ApplyStatusEffect(UnitBase attacker, UnitBase target, SkillData skill, ref Result r)                                                    
    {
        if (skill.statusEffect == null) return;

        float chance = (skill.applyChance > 0f) 
            ? skill.applyChance                                                                                                                                 
            : skill.statusEffect.applyChance;                                                                                                                   

        if (Random.value > chance) return;

        if (target is EnemyManager enemy)
        {
            if (skill.statusEffect.type == StatusEffectType.Burn)
            {
                bool applied = enemy.ApplyBurn(
                    skill.statusEffect,
                    skill.overrideDurationTurns
                );

                if (applied)
                {
                    r.message += "\n火傷を与えた！";
                }
                else
                {
                    r.message += "\n別の状態異常が有効なため、火傷は付与されなかった！";
                }
            }
        }
    }

    private static int Heal(UnitBase target, int amount)
    {
        int before = target.hp;
        // 最大HPを超えない範囲で、実際に回復した量を返す
        target.hp = Mathf.Min(target.maxHp, target.hp + amount);                                                                                                
        return target.hp - before;                                                                                                                             
    }
}
