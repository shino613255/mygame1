using UnityEngine;

[CreateAssetMenu(menuName = "Game/Status/StatusEffectData", fileName = "Status_")]
public class StatusEffectData : ScriptableObject
{
    public StatusEffectType type = StatusEffectType.None;

    [Min(1)] 
    public int durationTurns = 1; 

    [Range(0f, 1f)] 
    public float tickHpRate = 0.05f;    
    public int tickDamageFlat = 0;

    [Range(0f, 1f)] 
    public float statDownRate = 0.15f;

    public int CalcTickDamage(int targetMaxHp)
    {
        if (
            type != StatusEffectType.Burn &&
            type != StatusEffectType.Poison
        )
        {
            return 0;
        }

        // 固定ダメージが設定されている場合は割合ダメージより優先する
        if (tickDamageFlat > 0)
        {
            return Mathf.Max(
                1,
                tickDamageFlat
            );
        }

        int dmg =
            Mathf.RoundToInt(
                targetMaxHp * tickHpRate
            );

        return Mathf.Max(1, dmg);
    }
}
