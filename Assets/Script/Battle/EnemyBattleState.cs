using UnityEngine;

public sealed class EnemyBattleState
{
    private const int BloodFuryAttackCount = 2;
    private const float BloodFuryDamageMultiplier = 1.25f;
    private const float WardDamageMultiplier = 0.7f;

    public bool HasWard { get; private set; }

    public int EmpoweredAttacks { get; private set; }

    public void GrantWard() => HasWard = true;

    public void EndPlayerTurn() => HasWard = false;

    public void GrantBloodFury() => EmpoweredAttacks = BloodFuryAttackCount;

    public float ConsumePhysicalMultiplier()
    {
        if (EmpoweredAttacks <= 0)
        {
            return 1f;
        }

        EmpoweredAttacks--;
        return BloodFuryDamageMultiplier;
    }

    public int ReduceMainDirectDamage(int damage) => HasWard ? Mathf.Max(1, Mathf.RoundToInt(damage * WardDamageMultiplier)) : damage;

    public void Reset()
    {
        HasWard = false;
        EmpoweredAttacks = 0;
    }
}
