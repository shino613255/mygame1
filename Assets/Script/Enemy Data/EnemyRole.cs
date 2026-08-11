using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum EnemyRole
{
    Attacker, // バランス型：攻撃力・防御力ともに中間程度
    Tank,     // 硬い型：攻撃力は低いが、防御力が高い
    Speed,    // 素早い型：攻撃力は高いが、防御力が低い（回避率も高めにすることが多い）
    Magic,    // 魔法型：魔法攻撃力が高く、防御力は低い（魔法防御は中間程度）
}
