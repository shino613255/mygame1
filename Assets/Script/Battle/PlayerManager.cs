using System.Collections;
using UnityEngine;

public class PlayerManager : UnitBase
{
    private bool acted;                 //行動したか
    private EnemyManager targetEnemy;   //今のターンのターゲット

    [Header("Skill")]
    public int skillMpCost = 10;        //スキル消費MP
    public bool useSkill = false;       //スキル使用フラグ（UIボタンで切り替える想定）
    private SkillData currentSkill;        //（任意）現在選択中のスキルデータ（UIで選択させるなら必要）

    // PlayerManager.cs 内に追加
    public void Setup(PlayerData data)
    {
        if (data == null) return;

        // UnitBaseから継承しているステータス変数に、役職データを代入します
        // ※変数名はご自身のUnitBaseでの定義に合わせて調整してください
        this.maxHp = data.startMaxHp;
        this.hp = data.startMaxHp;
        this.maxMp = data.startMaxMp;
        this.mp = data.startMaxMp;
        this.at = data.startAt;

        Debug.Log($"[完了] {data.playerName}のステータスを同期しましたわ！ (AT:{this.at})");
    }
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
                Vector3 clickPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                clickPos.z = 0;
                BattleManager.Instance.PlaySkillEffect(currentSkill, clickPos);
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

    
    void Update()
    {
           
    }
}
