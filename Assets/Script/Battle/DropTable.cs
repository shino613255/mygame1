using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Drop/DropTable", fileName = "DropTable_")]
public class DropTable : ScriptableObject
{
    [System.Serializable]
    public class DropEntry
    {
        [Header("アイテムID")]
        public int itemId;
    
        [Range(0f, 1f)]     // ドロップ確率は0〜1の範囲で指定
        public float probability = 0.1f;

        [Header("一度しかドロップしない")]
        public bool unique = false;
    }

    public List<DropEntry> drops = new();

    // 抽選してアイテムIDを結果を返す
    public List<int> RollDrops()
    {
        var result = new List<int>();

        foreach (var d in drops)
        {
            if (d == null) continue;
            if (d.itemId < 0) continue;

            if (Random.value <= d.probability)
            {
                result.Add(d.itemId);
            }
        }

        return result;
    }
}
