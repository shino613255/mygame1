using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    menuName = "Game/Status/Buff Data",
    fileName = "Buff_"
)]
public class BuffData : ScriptableObject
{
    public BuffType type = BuffType.None;

    [Min(1)]
    public int durationTurns = 1;

    public int amount = 0;
}