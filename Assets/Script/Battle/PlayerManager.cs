using System.Collections;
using UnityEngine;

public class PlayerManager : UnitBase
{
    private bool acted;                 //行動したか
    private EnemyManager targetEnemy;   //今のターンのターゲット

    [Header("Skill")]
    public int skillMpCost = 10;        //スキル消費MP
    public bool useSkill = false;       //スキル使用フラグ（UIボタンで切り替える想定）

    //魔法・スキルで攻撃
    public bool TrySkillAttack(EnemyManager enemy)
    {
        if (enemy == null) return false;

        if (!TrySpendMp(skillMpCost))
        {
            Debug.Log("MPが足りない！");
            return false;
        }

        int dmg = MakeMagicDamage();
        enemy.TakeMagic(dmg);
        return true;
    }

    //SPD順で呼ばれた「自分のターン」
    public override IEnumerator Act()
    {
        Debug.Log("プレイヤーの行動（入力待ち）");

        acted = false;

        // いまは敵1体想定：存在する敵を取得
        targetEnemy = UnityEngine.Object.FindAnyObjectByType<EnemyManager>();
        if (targetEnemy == null)
        {
            acted = true;
            yield break;
        }

        // タップされるまで待つ（＝プレイヤー入力待ち）
        while (!acted)
        {
            yield return null;
        }

        yield break;
    }

    //ここを追加：敵がタップされた時に実行（ここで攻撃→ターン終了）
    private void OnEnemyTapped()
    {
        if (acted) return; //二重入力防止

        if (useSkill)
        {
            // スキルONなら魔法（MP足りなければ通常攻撃にフォールバック）
            if (!TrySkillAttack(targetEnemy))
            {
                int dmg = MakePhysicalDamage();
                targetEnemy.TakePhysical(dmg);
            }
        }
        else
        {
            // 通常攻撃
            int dmg = MakePhysicalDamage();
            targetEnemy.TakePhysical(dmg);
        }

        acted = true; //ターン終了
    }

    //（任意）テスト用：TキーでEXPを10追加（後で消す）
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            AddExp(10);
            Debug.Log($"EXP+10 → Lv:{level} / EXP:{exp}");
        }

        //（任意）テスト用：MキーでスキルON/OFF切り替え（後でUIに置き換える）
        if (Input.GetKeyDown(KeyCode.M))
        {
            useSkill = !useSkill;
            Debug.Log("スキル使用: " + useSkill);
        }
    }
}
