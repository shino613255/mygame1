using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
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
            case SkillType.Debuff:
                {
                    r.message = $"{attacker.name}の{skill.skillName}！";
                    break;
                }
        }

        ApplyStatusEffect(attacker, target, skill, ref r);

        if (cds != null) cds.StartCooldown(skill);

        return r;
    }


    private static void ApplyStatusEffect(UnitBase attacker, UnitBase target, SkillData skill, ref Result r)                                                    
    {
        if (skill.statusEffect == null) return;

        float chance = (skill.applyChance > 0f) 
            ? skill.applyChance                                                                                                                                 
            : skill.statusEffect.applyChance;                                                                                                                   

        if (Random.value > chance) return;                                                                                                                      

        if (target is EnemyManager em)                                                                                                                          
        {
            if (skill.statusEffect.type == StatusEffectType.Burn)
            {
                em.ApplyBurn(skill.statusEffect, skill.overrideDurationTurns);                                                                                  
                r.message += "\n火傷を与えた！";
            }
            else if (skill.statusEffect.type == StatusEffectType.DefenseUp)
            {
                em.ApplyDefenseBuff(
                    skill.statusEffect,
                    skill.overrideDurationTurns
                );

                r.message += "\n防御力が上がった！";
            }
            else if (skill.statusEffect.type == StatusEffectType.MagicDefenseUp)
            {
                em.ApplyMagicDefenseBuff(
                    skill.statusEffect,
                    skill.overrideDurationTurns
                );

                r.message += "\n魔法防御力が上がった！";
            }
            else if (skill.statusEffect.type == StatusEffectType.Deficiency)
            {
                r.message += "\n欠損させた！";
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
