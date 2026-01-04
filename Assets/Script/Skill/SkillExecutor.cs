using System.Collections;
using System.Collections.Generic;
// SkillExecutor.cs（スキルを実行する本体）
using UnityEngine;

public static class SkillExecutor
{
    public struct Result
    {
        public bool executed;   // MP不足/CT中で不発ならfalse
        public bool hit;
        public bool crit;
        public int value;       // ダメージ or 回復量
        public string message;  // 表示用
    }

    // attacker/target は UnitBase を想定（あなたのプロジェクトに合わせて）
    // enemy部位タップに対応するため part を受け取る（不要なら BodyPart.Body でOK）
    public static Result Execute(UnitBase attacker, UnitBase target, SkillData skill, float targetEvasion = 0f)
    {
        var r = new Result { executed = false, hit = false, crit = false, value = 0, message = "" };
        if (attacker == null || target == null || skill == null) return r;

        // CDチェック（付いてなければ無視して実行可）
        var cds = attacker.GetComponent<SkillCooldowns>();
        if (cds != null && !cds.IsReady(skill))
        {
            r.message = $"{skill.skillName} はクールダウン中！";
            return r;
        }

        // MPチェック（mpフィールドがある前提。無ければUnitBaseに足す）
        if (attacker.mp < skill.mpCost)
        {
            r.message = $"{skill.skillName} を使うMPが足りない！";
            return r;
        }

        // MP消費
        attacker.mp -= skill.mpCost;
        r.executed = true;

        // 命中（Heal/Selfは必中扱い）
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

        // 効果適用
        switch (skill.skillType)
        {
            case SkillType.Physical:
                {
                    int baseDmg = DamageRule.CalcPhysical(attacker.at, target.def, skill.multiplier, 1) + skill.power;

                    // クリティカル
                    if (skill.canCrit)
                    {
                        float rate = Mathf.Clamp01(0.05f + skill.critBonus); // ベース5% + bonus
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
                        float rate = Mathf.Clamp01(0.03f + skill.critBonus); // 魔法は3%基準（好みでOK）
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
                    // Healは target を回復（UnitBaseに Heal(int) が無い場合は TakePhysical(-x) などで代替してOK）
                    int heal = Mathf.Max(1, skill.power + Mathf.RoundToInt(attacker.mag * skill.multiplier));
                    r.value = Heal(target, heal);
                    r.message = $"{attacker.name}の{skill.skillName}！\n{r.value}回復！";
                    break;
                }
            case SkillType.Buff:
            case SkillType.Debuff:
                {
                    // 今は状態異常付与だけに寄せる（必要なら後でステータスUp/Down追加）
                    r.message = $"{attacker.name}の{skill.skillName}！";
                    break;
                }
        }

        // 追加効果（状態異常）
        ApplyStatusEffect(attacker, target, skill, ref r);

        // CD開始
        if (cds != null) cds.StartCooldown(skill);

        return r;
    }

    private static void ApplyStatusEffect(UnitBase attacker, UnitBase target, SkillData skill, ref Result r)
    {
        if (skill.statusEffect == null) return;

        float chance = (skill.applyChance > 0f) ? skill.applyChance : skill.statusEffect.applyChance;
        if (Random.value > chance) return;

        // いまは敵への Burn/Deficiency にだけ対応（あなたの EnemyManager 実装に合わせる）
        if (target is EnemyManager em)
        {
            if (skill.statusEffect.type == StatusEffectType.Burn)
            {
                em.ApplyBurn();                
                r.message += "\n火傷を与えた！";
            }
            else if (skill.statusEffect.type == StatusEffectType.Deficiency)
            {
                em.ApplyBreak();
                r.message += "\n欠損させた！";
            }
        }
    }

    // UnitBaseにHealが無い前提の簡易回復
    private static int Heal(UnitBase target, int amount)
    {
        int before = target.hp;
        target.hp = Mathf.Min(target.maxHp, target.hp + amount);
        return target.hp - before;
    }
}
