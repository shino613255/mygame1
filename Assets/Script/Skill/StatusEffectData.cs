using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Status/StatusEffectData", fileName = "Status_")]
public class StatusEffectData : ScriptableObject
{
    public StatusEffectType type = StatusEffectType.None;

    [Min(1)] public int durationTurns = 1;
    [Range(0f, 1f)] public float applyChance = 0.05f;

    // Burn 用
    [Range(0f, 1f)] public float tickHpRate = 0.05f;
    public int tickDamageFlat = 0;

    // Deficiency 用
    [Range(0f, 1f)] public float statDownRate = 0.15f;

    [Header("VFX")]
    public GameObject vfxPrefab;   // 状態異常の見た目
    public int CalcTickDamage(int targetMaxHp)
    {
        if (type != StatusEffectType.Burn) return 0;

        if (tickDamageFlat > 0)
            return Mathf.Max(1, tickDamageFlat);

        int dmg = Mathf.RoundToInt(targetMaxHp * tickHpRate);
        return Mathf.Max(1, dmg);
    }
}
