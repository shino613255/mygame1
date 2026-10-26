using UnityEngine;

public sealed class EnemyBattleState
{
    public bool HasWard { get; private set; }
    public int EmpoweredAttacks { get; private set; }

    public void GrantWard() => HasWard = true;
    public void EndPlayerTurn() => HasWard = false;
    public void GrantBloodFury() => EmpoweredAttacks = 2;

    public float ConsumePhysicalMultiplier()
    {
        if (EmpoweredAttacks <= 0) return 1f;
        EmpoweredAttacks--;
        return 1.25f;
    }

    public int ReduceMainDirectDamage(int damage) => HasWard
        ? Mathf.Max(1, Mathf.RoundToInt(damage * 0.7f)) : damage;

    public void Reset()
    {
        HasWard = false;
        EmpoweredAttacks = 0;
    }
}
