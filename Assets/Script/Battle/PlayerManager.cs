using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class PlayerManager : UnitBase
{ 
    [Header("Skill Panel")]
    [SerializeField] private SkillSlotUI[] skillSlots;

    private List<SkillData> currentSkills = new List<SkillData>();                                          // スキルスロットの中身を保持するリスト
    private SkillData currentSkill;                                                                         // 選択されているスキル
    private bool acted;                                                                                     // 行動済みフラグ
    private EnemyManager targetEnemy;                                                                       // 今のターンのターゲット
    public bool useSkill = false;                                                                           //  スキル使用フラグ

    public void Setup(PlayerData data)
    {
        if (data == null) return;                                                                           // nullの場合何もしない

        this.maxHp = data.startMaxHp;
        this.hp = data.startMaxHp;
        this.maxMp = data.startMaxMp;
        this.mp = data.startMaxMp;
        this.at = data.startAt;
        this.def = data.startDef;
        this.mdef = data.startMdef;


        Debug.Log($"[完了] {data.playerName}のステータスを同期しましたわ！ (AT:{this.at})");

        currentSkills.Clear();                                                                              // 現在のスキルリストを初期化        

        if (data.startSkills != null)                                                                       // スタートスキルがnullでない場合
        {
            currentSkills.AddRange(data.startSkills);                                                       // スタートスキルを現在のスキルリストに追加
        }

        RefreshSkillPanel();                                                                                // UIのスキルパネルを更新
        Debug.Log("初期スキル数: " + currentSkills.Count);
    }  

    public void SelectSkill(SkillData skill)
    {
        if (skill == null) return;                                                                          // nullの場合何もしない        

        currentSkill = skill;                                                                               // 選択されたスキルをcurrentSkillに設定
        useSkill = true;

        Debug.Log("スキル「" + skill.skillName + "」を選択しました");
    }

    public void AddSkill(SkillData skill)
    {
        if (skill == null) return;                                                                              // nullの場合何もしない

        currentSkills.Add(skill);                                                                               // スキルリストに追加
        RefreshSkillPanel();                                                                                    // UIのスキルパネルを更新

        Debug.Log("スキル「" + skill.skillName + "」を習得しました");
    }
    private void RefreshSkillPanel()                                                                        // スキルパネルのUIを更新するメソッド
    {
        Debug.Log(skillSlots == null);
        for (int i = 0; i < skillSlots.Length; i++)
        {
            if (i < currentSkills.Count)
            {
                skillSlots[i].SetSkill(currentSkills[i], this);                                             // スキルスロットにスキルをセット
            }
            else
            {
                skillSlots[i].SetSkill(null, this);
            }
        }
    }

    public bool TrySkillAttack(EnemyManager enemy)
    {
        if (enemy == null) return false;                                                                        // nullの場合何もしない

        if (!TrySpendMp(currentSkill.mpCost))                                                                   // MPを消費できるか確認(UnitBaseを呼び出し)
        {
            Debug.Log("MPが足りない！");
            return false;
        }

        int dmg = MakeMagicDamage();                                                                            // 魔法ダメージを計算(UnitBaseを呼び出し)
        enemy.TakeMagic(dmg);                                                                                   // 敵に魔法ダメージを与える(UnitBaseを呼び出し)
        return true;
    }

    public override IEnumerator Act()
    {  
        Debug.Log("プレイヤーの行動（入力待ち）");

        acted = false;                                                                                          // 行動フラグをリセット

        targetEnemy = UnityEngine.Object.FindAnyObjectByType<EnemyManager>();                                   // ターゲットとなる敵を取得（シーン内の最初のEnemyManagerを取得）
        if (targetEnemy == null)                                                                                // 敵が存在しない場合                                
        {
            acted = true;
            yield break;
        }

        while (!acted)                                                                                          // 行動が完了するまでの間
        {
            yield return null;                                                                                  // 1フレーム待機を続けることで選択する時間を与える
        }

        yield break;
    }
    
    void Update()
    {
           
    }
}