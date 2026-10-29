using UnityEngine;

public static class SkillExecutor
{
    private const float DefaultCriticalRate = 0.03f;
    private const float CriticalDamageMultiplier = 1.5f;
    private const int ManaLevyAmount = 10;
    private const float GuardReduction = 0.5f;
    private const float FirstAidHealRate = 0.12f;
    private const float WarCryHealRate = 0.15f;
    private const float RenewalHealRate = 0.15f;

    public struct Result
    {
        public bool executed;
        public bool hit;
        public bool crit;
        public int value;
        public string message;
    }

    public static Result Execute(UnitBase attacker, UnitBase target, SkillData skill, float targetEvasion = 0f)
    {
        var result = new Result
        {
            executed = false,
            hit = false,
            crit = false,
            value = 0,
            message = ""
        };
        if (attacker == null || target == null || skill == null)
        {
            return result;
        }

        if (skill.skillType == SkillType.Counter &&
            (target is not EnemyManager counterTarget || counterTarget != attacker || !counterTarget.CanActivateCounter))
        {
            result.message = "今はカウンターを使えない！";
            return result;
        }

        if (!SkillUseRules.TryPay(attacker, target, skill, out string useError))
        {
            result.message = useError;
            return result;
        }

        var cooldowns = attacker.GetComponent<SkillCooldowns>();
        if (cooldowns != null && !cooldowns.IsReady(skill))
        {
            result.message = $"{skill.skillName} はクールダウン中！";
            return result;
        }

        result.executed = true;
        if (ExecuteNewSelfSkill(attacker, skill, ref result))
        {
            cooldowns?.StartCooldown(skill);
            return result;
        }

        float attackMultiplier = skill.skillType == SkillType.Physical && attacker is EnemyManager boostedEnemy
            ? boostedEnemy.BattleEffects.ConsumePhysicalMultiplier()
            : 1f;
        bool needHitCheck = (skill.skillType == SkillType.Physical || skill.skillType == SkillType.Magic ||
            skill.skillType == SkillType.Debuff);
        if (!needHitCheck || DamageRule.RollHit(skill.accuracy, targetEvasion))
        {
            result.hit = true;
        }
        else
        {
            result.hit = false;
            result.message = $"{attacker.name}の{skill.skillName}！\nしかし外れた！";
            if (cooldowns != null)
            {
                cooldowns.StartCooldown(skill);
            }

            return result;
        }

        switch (skill.skillType)
        {
            case SkillType.Counter:
                ((EnemyManager)target).ActivateCounter();
                result.message = $"{attacker.name}はカウンターを構えた！\n次のプレイヤーターン終了までダメージを反射する。";
                break;
            case SkillType.Physical:
            {
                int baseDamage = DamageRule.CalcPhysical(attacker.at, target.def, skill.power);
                if (skill.canCrit)
                {
                    float rate = Mathf.Clamp01(DefaultCriticalRate + skill.critBonus);
                    int criticalDamage = DamageRule.RollCrit(baseDamage, rate, CriticalDamageMultiplier, 1);
                    result.crit = criticalDamage != baseDamage;
                    baseDamage = criticalDamage;
                }

                baseDamage = Mathf.Max(1, Mathf.RoundToInt(baseDamage * attackMultiplier));
                result.value = target is PlayerManager physicalTarget
                    ? physicalTarget.TakeDirectDamage(baseDamage)
                    : target.TakeDamageRaw(baseDamage, false);
                result.message = $"{attacker.name}の{skill.skillName}！\n{result.value}ダメージ！";
                break;
            }

            case SkillType.Magic:
            {
                int baseDamage = DamageRule.CalcMagic(attacker.mag, target.mdef, skill.power);
                if (skill.canCrit)
                {
                    float rate = Mathf.Clamp01(DefaultCriticalRate + skill.critBonus);
                    int criticalDamage = DamageRule.RollCrit(baseDamage, rate, CriticalDamageMultiplier, 1);
                    result.crit = criticalDamage != baseDamage;
                    baseDamage = criticalDamage;
                }

                result.value = target is PlayerManager magicTarget
                    ? magicTarget.TakeDirectDamage(baseDamage, true)
                    : target.TakeDamageRaw(baseDamage, true);
                result.message = $"{attacker.name}の{skill.skillName}！\n{result.value}ダメージ！";
                if (skill.id == SkillId.Auditor_ManaLevy)
                {
                    int drained = Mathf.Min(ManaLevyAmount, target.mp);
                    target.mp -= drained;
                    result.message += $"\nMPを{drained}失った！";
                }

                break;
            }

            case SkillType.Heal:
            {
                int heal = Mathf.Max(1, skill.power + Mathf.RoundToInt(attacker.mag * skill.multiplier));
                result.value = Heal(target, heal);
                result.message = $"{attacker.name}の{skill.skillName}！\n{result.value}回復！";
                break;
            }

            case SkillType.RecoverMp:
            {
                int beforeMp = target.mp;
                target.RecoverMp(skill.power);
                result.value = target.mp - beforeMp;
                result.message = $"{attacker.name}の{skill.skillName}！\nMPが{result.value}回復！";
                break;
            }

            case SkillType.Buff:
                result.message = $"{skill.skillName}!";
                ApplyBuff(target, skill, ref result);
                break;
            case SkillType.Debuff:
            {
                result.message = $"{attacker.name}の{skill.skillName}！";
                ApplyDebuff(target, skill, ref result);
                break;
            }
        }

        ApplyStatusEffect(attacker, target, skill, ref result);
        if (cooldowns != null)
        {
            cooldowns.StartCooldown(skill);
        }

        return result;
    }

    private static void ApplyBuff(UnitBase target, SkillData skill, ref Result result)
    {
        if (skill.buff == null)
        {
            return;
        }

        if (target is not EnemyManager enemy)
        {
            return;
        }

        switch (skill.buff.type)
        {
            case BuffType.DefenseUp:
                enemy.ApplyDefenseBuff(skill.buff);
                result.message += "\n防御力が上がった！";
                break;
            case BuffType.MagicDefenseUp:
                enemy.ApplyMagicDefenseBuff(skill.buff);
                result.message += "\n魔法防御力が上がった！";
                break;
        }
    }

    private static void ApplyDebuff(UnitBase target, SkillData skill, ref Result result)
    {
        if (skill.debuff == null)
        {
            return;
        }

        if (Random.value > skill.applyChance)
        {
            result.message += "\nデバフは効かなかった！";
            return;
        }

        if (target is not EnemyManager enemy)
        {
            return;
        }

        switch (skill.debuff.type)
        {
            case DebuffType.Deficiency:
                result.message += "\n欠損させた！";
                break;
        }
    }

    private static void ApplyStatusEffect(UnitBase attacker, UnitBase target, SkillData skill, ref Result result)
    {
        if (skill.statusEffect == null)
        {
            return;
        }

        if (Random.value > skill.applyChance)
        {
            return;
        }

        if (target is EnemyManager enemy)
        {
            if (skill.statusEffect.type == StatusEffectType.Burn)
            {
                bool applied = enemy.ApplyBurn(skill.statusEffect);
                if (applied)
                {
                    result.message += "\n火傷を与えた！";
                }
                else
                {
                    result.message += "\n別の状態異常が有効なため、火傷は付与されなかった！";
                }
            }
        }

        if (target is PlayerManager player)
        {
            if (skill.statusEffect.type == StatusEffectType.Poison)
            {
                bool applied = player.ApplyPoison(skill.statusEffect);
                if (applied)
                {
                    result.message += "\n毒状態になった！";
                }
            }
        }
    }

    public static void PreparePlayerAttack(PlayerManager player, ref AttackContext context)
    {
        float multiplier = player.BattleEffects.ConsumeAttackEffects(
            context.sourceSkill,
            out float aimAccuracy,
            out float aimPartMultiplier);
        context.baseDamage = Mathf.Max(1, Mathf.RoundToInt(context.baseDamage * multiplier));
        context.accuracyBonus = player.GetPrecisionShotAccuracyBonus() + aimAccuracy;
        context.preparedPartDamageMultiplier = aimPartMultiplier;
    }

    private static bool ExecuteNewSelfSkill(UnitBase user, SkillData skill, ref Result result)
    {
        switch (skill.id)
        {
            case SkillId.Common_Guard:
                ((PlayerManager)user).BattleEffects.GrantGuard(GuardReduction);
                break;
            case SkillId.Common_Aim:
                ((PlayerManager)user).BattleEffects.GrantAim();
                break;
            case SkillId.Common_Antidote:
                ((PlayerManager)user).RemovePoison();
                break;
            case SkillId.Common_FirstAid:
                result.value = Heal(user, Mathf.Max(1, Mathf.RoundToInt(user.maxHp * FirstAidHealRate)));
                break;
            case SkillId.Berserker_WarCry:
                result.value = Heal(user, Mathf.Max(1, Mathf.RoundToInt(user.maxHp * WarCryHealRate)));
                ((PlayerManager)user).BattleEffects.GrantWarCry();
                break;
            case SkillId.Raider_BloodFury:
                ((EnemyManager)user).BattleEffects.GrantBloodFury();
                break;
            case SkillId.Pilgrim_TalismanWard:
                ((EnemyManager)user).BattleEffects.GrantWard();
                break;
            case SkillId.Pilgrim_Renewal:
                result.value = Heal(user, Mathf.Max(1, Mathf.RoundToInt(user.maxHp * RenewalHealRate)));
                break;
            default:
                return false;
        }

        result.hit = true;
        result.message = $"{user.name}の{skill.skillName}！";
        if (result.value > 0)
        {
            result.message += $"\nHPを{result.value}回復した！";
        }

        if (user is PlayerManager player)
        {
            player.ResetFocusedAttackChain();
        }

        return true;
    }

    private static int Heal(UnitBase target, int amount)
    {
        int before = target.hp;
        target.hp = Mathf.Min(target.maxHp, target.hp + amount);
        return target.hp - before;
    }
}
