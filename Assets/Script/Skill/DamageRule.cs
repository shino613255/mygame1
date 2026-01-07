using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class DamageRule
{
    // 物理：atk - def をベースに倍率を掛ける
    public static int CalcPhysical(int atk, int def, float multiplier = 1f, int minDamage = 1)
    {
        float raw = (atk - def) * multiplier;
        int dmg = Mathf.RoundToInt(raw);
        return Mathf.Max(minDamage, dmg);
    }

    // 魔法：mag - mdef をベースに倍率を掛ける
    public static int CalcMagic(int mag, int mdef, float multiplier = 1f, int minDamage = 1)
    {
        float raw = (mag - mdef) * multiplier;
        int dmg = Mathf.RoundToInt(raw);
        return Mathf.Max(minDamage, dmg);
    }

    // 命中：accuracy/evasion は 0〜1
    public static bool RollHit(float accuracy, float evasion)
    {
        accuracy = Mathf.Clamp01(accuracy);
        evasion = Mathf.Clamp01(evasion);

        // 最終命中率（簡単式）
        float final = Mathf.Clamp01(accuracy - evasion);
        return Random.value < final;
    }

    public static int RollCrit(int baseDamage, float rate, float bonusMultiplier = 1.5f, int minDamage = 1)
    {
        rate = Mathf.Clamp01(rate);

        // ★ 最低1に寄せる
        baseDamage = Mathf.Max(minDamage, baseDamage);

        if (Random.value >= rate) return baseDamage;

        int crit = Mathf.RoundToInt(baseDamage * bonusMultiplier);
        return Mathf.Max(minDamage, crit);
    }

}
