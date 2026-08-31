using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    menuName = "Game/Status/Debuff Data",
    fileName = "Debuff_"
)]
public class DebuffData : ScriptableObject
{
    public DebuffType type = DebuffType.None;

    [Min(1)]
    public int durationTurns = 1;

    [Range(0f, 1f)]
    public float statDownRate = 0.15f;

    public GameObject vfxPrefab;
}