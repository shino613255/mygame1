using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class DamageRule
{
    private const float DamageConstant = 3f;
    private const int NormalAttackPower = 100;
    private const int MinDamage = 1;

    public static int CalcNormalAttack(
        int atk,
        int def
    )
    {
        return CalcPhysical(
            atk,
            def,
            NormalAttackPower
        );
    }

    public static int CalcPhysical(
        int atk,
        int def,
        int skillPower
    )
    {
        float safeDef = Mathf.Max(1, def);

        int damage =
            Mathf.RoundToInt(
                Mathf.Sqrt(
                    skillPower * atk / safeDef
                )
                * DamageConstant
            );

        return Mathf.Max(
            MinDamage,
            damage
        );
    }

    public static int CalcMagic(
        int mag,
        int mdef,
        int skillPower
    )
    {
        float safeMdef = Mathf.Max(1, mdef);

        int damage =
            Mathf.RoundToInt(
                Mathf.Sqrt(
                    skillPower * mag / safeMdef
                )
                * DamageConstant
            );

        return Mathf.Max(
            MinDamage,
            damage
        );
    }

    // 命中：accuracy/evasion は 0〜1
    public static bool RollHit(float accuracy, float evasion)
    {
        accuracy = Mathf.Clamp01(accuracy);
        evasion = Mathf.Clamp01(evasion);

        float final = Mathf.Clamp01(accuracy - evasion);
        return Random.value < final;
    }

    public static int RollCrit(int baseDamage, float rate, float bonusMultiplier = 1.5f, int minDamage = 1)
    {
        rate = Mathf.Clamp01(rate);

        baseDamage = Mathf.Max(minDamage, baseDamage);

        if (Random.value >= rate) return baseDamage;

        int crit = Mathf.RoundToInt(baseDamage * bonusMultiplier);
        return Mathf.Max(minDamage, crit);
    }
}
