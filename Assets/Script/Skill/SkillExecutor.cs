using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public static/*ヒエラルキービューでオブジェクトを作らなくてよい*/ class SkillExecutor
{
    public struct Result
    {
        public bool executed;       // スキルが実行のフラグ
        public bool hit;            // 命中のフラグ
        public bool crit;           // クリティカルのフラグ
        public int value;           // ダメージ量や回復量などの値
        public string message;      // 戦闘中に表示するメッセージ
    }

    public static Result Execute(
        UnitBase attacker,          　　　　　　　　　　　　                                                                                                // スキルを使用するユニット
        UnitBase target,                                                                                                                                    // スキルの対象ユニット
        SkillData skill,                                                                                                                                    // 使用するスキルのデータ
        float targetEvasion = 0f                                                                                                                            // 対象の回避率                                                           
        )
    {
        var r = new Result
        {                                                                                                                                                   //スキル処理の結果を保存する構造体(初期化状態)
            executed = false, 
            hit = false, 
            crit = false, 
            value = 0, 
            message = "" 
        };

        if (attacker == null || target == null || skill == null) return r;

        var cds = attacker.GetComponent<SkillCooldowns>();                                                                                                  // スキルのクールダウン管理コンポーネントを取得（無ければnull）

        if (cds != null && !cds.IsReady(skill))                                                                                                             // スキルクールダウンのコンポーネントがありクールダウン中の場合
        {
            r.message = $"{skill.skillName} はクールダウン中！";
            return r;
        }

        if (attacker.mp < skill.mpCost)
        {
            r.message = $"{skill.skillName} を使うMPが足りない！";
            return r;
        }

        attacker.mp -= skill.mpCost;                                                                                                                        // MP消費                                     
        r.executed = true;                                                                                                                                  // スキルが実行されたフラグを立てる

        bool needHitCheck = (skill.skillType == SkillType.Physical || skill.skillType == SkillType.Magic || skill.skillType == SkillType.Debuff);           // 命中判定が必要なスキルタイプかどうかを判定

        if (!needHitCheck || DamageRule.RollHit(skill.accuracy, targetEvasion))                                                                             // スキルの命中率と対象の回避率の判定に成功した場合
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
                    int baseDmg = DamageRule.CalcPhysical(attacker.at, target.def, skill.multiplier, 1) + skill.power;                                      // ダメージ計算（物理攻撃力、対象の防御力、スキル倍率、最小ダメージ1） + スキル固定値

                    if (skill.canCrit)
                    {
                        float rate = Mathf.Clamp01(0.03f + skill.critBonus);                                                                                // クリティカル率の計算（基本3% + スキルの追加クリティカル率）
                        int after = DamageRule.RollCrit(baseDmg, rate, 1.5f, 1);                                                                            // クリティカル判定（ダメージ、クリティカル率、クリティカル倍率1.5、最小ダメージ1）
                        r.crit = after != baseDmg;
                        baseDmg = after;
                    }

                    r.value = target.TakePhysical(baseDmg);                                                                                                 // 対象に物理ダメージを与える
                    r.message = $"{attacker.name}の{skill.skillName}！\n{r.value}ダメージ！";
                    break;
                }
            case SkillType.Magic:
                {
                    int baseDmg = DamageRule.CalcMagic(attacker.mag, target.mdef, skill.multiplier, 1) + skill.power;                                       // ダメージ計算（魔法攻撃力、対象の魔法防御力、スキル倍率、最小ダメージ1） + スキル固定値

                    if (skill.canCrit)
                    {
                        float rate = Mathf.Clamp01(0.03f + skill.critBonus);
                        int after = DamageRule.RollCrit(baseDmg, rate, 1.5f, 1);
                        r.crit = after != baseDmg;
                        baseDmg = after;
                    }

                    r.value = target.TakeMagic(baseDmg);                                                                                                    // 対象に魔法ダメージを与える
                    r.message = $"{attacker.name}の{skill.skillName}！\n{r.value}ダメージ！";
                    break;
                }
            case SkillType.Heal:
                {
                    int heal = Mathf.Max(1, skill.power + Mathf.RoundToInt(attacker.mag * skill.multiplier));                                               // 回復量計算（1、スキル固定値 + 魔法攻撃力 * スキル倍率）最大値の方を入れる
                    r.value = Heal(target, heal);                                                                                                           // 対象に回復を与える
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

        // （状態異常）
        ApplyStatusEffect(attacker, target, skill, ref r);

        if (cds != null) cds.StartCooldown(skill);

        return r;
    }


    private static void ApplyStatusEffect(UnitBase attacker, UnitBase target, SkillData skill, ref Result r)                                                    // 状態異常の付与処理
    {
        if (skill.statusEffect == null) return;

        float chance = (skill.applyChance > 0f) 
            ? skill.applyChance                                                                                                                                 // skilldateの付与率を使う
            : skill.statusEffect.applyChance;                                                                                                                   // tatuseffectの付与率を使う

        if (Random.value > chance) return;                                                                                                                      // 付与率判定に失敗した場合は何もしないで返す

        if (target is EnemyManager em)                                                                                                                          // 対象がEnemyManagerの場合のみ状態異常を付与する
        {
            if (skill.statusEffect.type == StatusEffectType.Burn)
            {
                em.ApplyBurn(skill.statusEffect, skill.overrideDurationTurns);                                                                                  // 状態異常を付与する（状態異常データ、ターン数）  
                r.message += "\n火傷を与えた！";
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
        target.hp = Mathf.Min(target.maxHp, target.hp + amount);                                                                                                // 最大HPを超えないよう回復量を加算する
        return target.hp - before;                                                                                                                              // 実際に回復した量を返す
    }
}
