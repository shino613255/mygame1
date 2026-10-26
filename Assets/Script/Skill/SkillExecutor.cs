using UnityEngine;

public static class SkillExecutor
{
    public struct Result
    {
        public bool executed;       
        public bool hit;            
        public bool crit;           
        public int value;   // ダメージ量や回復量などの値
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

        if (skill.skillType == SkillType.Counter &&
            (target is not EnemyManager counterTarget || counterTarget != attacker ||
             !counterTarget.CanActivateCounter))
        {
            r.message = "今はカウンターを使えない！";
            return r;
        }

        if (!SkillUseRules.TryPay(attacker, target, skill, out string useError))
        {
            r.message = useError;
            return r;
        }

        var cds = attacker.GetComponent<SkillCooldowns>();

        if (cds != null && !cds.IsReady(skill))                                                                                                             
        {
            r.message = $"{skill.skillName} はクールダウン中！";
            return r;
        }

        r.executed = true;                                                                                                                                  

        if (ExecuteNewSelfSkill(attacker, skill, ref r))
        {
            cds?.StartCooldown(skill);
            return r;
        }

        float attackMultiplier = skill.skillType == SkillType.Physical && attacker is EnemyManager boostedEnemy
            ? boostedEnemy.BattleEffects.ConsumePhysicalMultiplier() : 1f;

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
            case SkillType.Counter:
                ((EnemyManager)target).ActivateCounter();
                r.message = $"{attacker.name}はカウンターを構えた！\n次のプレイヤーターン終了までダメージを反射する。";
                break;
            case SkillType.Physical:
                {
                    int baseDmg =
                        DamageRule.CalcPhysical(
                            attacker.at,
                            target.def,
                            skill.power
                        );                                  

                    if (skill.canCrit)
                    {
                        float rate = Mathf.Clamp01(0.03f + skill.critBonus);
                        // クリティカル時は1.5倍
                        int after = DamageRule.RollCrit(baseDmg, rate, 1.5f, 1);                                                                           
                        r.crit = after != baseDmg;
                        baseDmg = after;
                    }

                    baseDmg = Mathf.Max(1, Mathf.RoundToInt(baseDmg * attackMultiplier));
                    r.value = target is PlayerManager physicalTarget
                        ? physicalTarget.TakeDirectDamage(baseDmg)
                        : target.TakeDamageRaw(baseDmg, false);
                    r.message = $"{attacker.name}の{skill.skillName}！\n{r.value}ダメージ！";
                    break;
                }
            case SkillType.Magic:
                {
                    int baseDmg =
                        DamageRule.CalcMagic(
                            attacker.mag,
                            target.mdef,
                            skill.power
                        );

                    if (skill.canCrit)
                    {
                        float rate = Mathf.Clamp01(0.03f + skill.critBonus);
                        int after = DamageRule.RollCrit(baseDmg, rate, 1.5f, 1);
                        r.crit = after != baseDmg;
                        baseDmg = after;
                    }

                    r.value = target is PlayerManager magicTarget
                        ? magicTarget.TakeDirectDamage(baseDmg, true)
                        : target.TakeDamageRaw(baseDmg, true);
                    r.message = $"{attacker.name}の{skill.skillName}！\n{r.value}ダメージ！";
                    if (skill.id == SkillId.Auditor_ManaLevy)
                    {
                        int drained = Mathf.Min(10, target.mp);
                        target.mp -= drained;
                        r.message += $"\nMPを{drained}失った！";
                    }
                    break;
                }
            case SkillType.Heal:
                {
                    int heal = Mathf.Max(1, skill.power + Mathf.RoundToInt(attacker.mag * skill.multiplier));                                               
                    r.value = Heal(target, heal);                                                                                                           
                    r.message = $"{attacker.name}の{skill.skillName}！\n{r.value}回復！";
                    break;
                }
            case SkillType.RecoverMp:
                {
                    int beforeMp = target.mp;

                    target.RecoverMp(skill.power);

                    r.value = target.mp - beforeMp;

                    r.message =
                        $"{attacker.name}の{skill.skillName}！\nMPが{r.value}回復！";

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
                    skill.buff
                );

                r.message += "\n防御力が上がった！";
                break;

            case BuffType.MagicDefenseUp:
                enemy.ApplyMagicDefenseBuff(
                    skill.buff
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

        if (Random.value > skill.applyChance) return;

        if (target is EnemyManager enemy)
        {
            if (skill.statusEffect.type == StatusEffectType.Burn)
            {
                bool applied = enemy.ApplyBurn(
                    skill.statusEffect
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

        if (target is PlayerManager player)
        {
            if (
                skill.statusEffect.type ==
                StatusEffectType.Poison
            )
            {
                bool applied =
                    player.ApplyPoison(
                        skill.statusEffect
                    );

                if (applied)
                {
                    r.message +=
                        "\n毒状態になった！";
                }
            }
        }
    }
    public static void PreparePlayerAttack(PlayerManager player, ref AttackContext context)
    {
        float multiplier = player.BattleEffects.ConsumeAttackEffects(
            context.sourceSkill, out float aimAccuracy, out float aimPartMultiplier);
        context.baseDamage = Mathf.Max(1, Mathf.RoundToInt(context.baseDamage * multiplier));
        context.accuracyBonus = player.GetPrecisionShotAccuracyBonus() + aimAccuracy;
        context.preparedPartDamageMultiplier = aimPartMultiplier;
    }

    private static bool ExecuteNewSelfSkill(UnitBase user, SkillData skill, ref Result result)
    {
        switch (skill.id)
        {
            case SkillId.Common_Guard:
                ((PlayerManager)user).BattleEffects.GrantGuard(0.5f);
                break;
            case SkillId.Common_Aim:
                ((PlayerManager)user).BattleEffects.GrantAim();
                break;
            case SkillId.Common_Antidote:
                ((PlayerManager)user).RemovePoison();
                break;
            case SkillId.Common_FirstAid:
                result.value = Heal(user, Mathf.Max(1, Mathf.RoundToInt(user.maxHp * 0.12f)));
                break;
            case SkillId.Berserker_WarCry:
                result.value = Heal(user, Mathf.Max(1, Mathf.RoundToInt(user.maxHp * 0.15f)));
                ((PlayerManager)user).BattleEffects.GrantWarCry();
                break;
            case SkillId.Raider_BloodFury:
                ((EnemyManager)user).BattleEffects.GrantBloodFury();
                break;
            case SkillId.Pilgrim_TalismanWard:
                ((EnemyManager)user).BattleEffects.GrantWard();
                break;
            case SkillId.Pilgrim_Renewal:
                result.value = Heal(user, Mathf.Max(1, Mathf.RoundToInt(user.maxHp * 0.15f)));
                break;
            default:
                return false;
        }

        result.hit = true;
        result.message = $"{user.name}の{skill.skillName}！";
        if (result.value > 0) result.message += $"\nHPを{result.value}回復した！";
        if (user is PlayerManager player) player.ResetFocusedAttackChain();
        return true;
    }

    private static int Heal(UnitBase target, int amount)
    {
        int before = target.hp;
        target.hp = Mathf.Min(target.maxHp, target.hp + amount);                                                                                                
        return target.hp - before;                                                                                                                             
    }
}
