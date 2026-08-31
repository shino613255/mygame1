using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IEnemyRoleAction
{
    SkillData ChooseSkill(
        EnemyManager enemy,
        List<SkillData> pool
    );
}