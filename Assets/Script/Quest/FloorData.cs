using System.Collections.Generic;

[System.Serializable]
public class FloorData
{
    public bool isBossFloor;
    public List<EnemyData> enemyDatas = new();
}
