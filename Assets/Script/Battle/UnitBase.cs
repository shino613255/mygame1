using UnityEngine;

public abstract class UnitBase : MonoBehaviour
{
    [Header("Stats")]
    public int maxHp = 1;
    public int hp    = 1;
    public int maxMp = 1;
    public int mp    = 1;
    public int at    = 1;
    public int def   = 1;
    public int mag   = 1;
    public int mdef  = 1;

    [Header("Evasion")]
    [Range(0f, 1f)]
    public float evasionRate = 0f; 

    [Header("Critical")]
    [Range(0f, 1f)]
    public float critRate = 0.03f;
    public float critMultiplier = 1.5f;

    public bool IsAlive => hp > 0;

    public void RecoverMp(int amount)
    {
        mp = Mathf.Min(maxMp, mp + Mathf.Max(0, amount));
    }

    public virtual int TakeDamageRaw(
    int damage,
    bool isMagic = false
)
    {
        damage = Mathf.Max(1, damage);

        hp = Mathf.Clamp(
            hp - damage,
            0,
            maxHp
        );

        OnDamaged(damage, isMagic);

        if (hp <= 0)
        {
            OnDied();
            Destroy(gameObject);
        }

        return damage;
    }

    protected virtual void OnDamaged(int damage, bool isMagic)
    {
        Debug.Log(name + "は" + damage + "のダメージを受けた");
    }

    protected virtual void OnDied()
    {
        Debug.Log(name + "は倒れた");
    }
}
