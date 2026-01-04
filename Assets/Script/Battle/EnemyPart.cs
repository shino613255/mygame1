using System.Collections;
using System.Collections.Generic;
// EnemyPart.cs（あなたの既存に合わせて、変更が必要ならここだけ）
// すでにほぼ同じならそのままでOK
using UnityEngine;

public class EnemyPart : MonoBehaviour
{
    public BodyPart part;
    public BattleManager battle;

    private bool tapEnabled = true;

    public void SetTapEnabled(bool enable) => tapEnabled = enable;

    private void OnMouseDown()
    {
        if (!tapEnabled) return;
        if (battle == null) return;

        battle.OnEnemyPartTapped(part);
    }
}
