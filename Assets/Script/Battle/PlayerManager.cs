using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class PlayerManager : UnitBase
{ 
    [Header("Skill Panel")]
    [SerializeField] private SkillSlotUI[] skillSlots;

    private List<SkillData> currentSkills = new List<SkillData>();                                          // スキルスロットの中身を保持するリスト
    private SkillData currentSkill;                                                                         
    private bool acted;                                                                                     
    private EnemyManager targetEnemy;                                                                       
    public bool useSkill = false;                                                                           

    public void Setup(PlayerData data)
    {
        if (data == null) return;                                                                          

        name = data.playerName;                                                                            

        this.maxHp = data.startMaxHp;
        this.hp = data.startMaxHp;
        this.maxMp = data.startMaxMp;
        this.mp = data.startMaxMp;
        this.at = data.startAt;
        this.mag = data.startMag;
        this.def = data.startDef;
        this.mdef = data.startMdef;


        Debug.Log($"[完了] {data.playerName}のステータスを同期しましたわ！ (AT:{this.at})");

        currentSkills.Clear();                                                                              

        if (data.startSkills != null)                                                                         
        {
            currentSkills.AddRange(data.startSkills);                                               // スタートスキルを現在のスキルリストに追加
        }

        UpdateSkillPanel();                                                                         // UIのスキルパネルを更新
        Debug.Log("初期スキル数: " + currentSkills.Count);
    }  

    public void SelectSkill(SkillData skill)
    {
        if (skill == null) return;                                                                                  

        currentSkill = skill;                                                                                  
        useSkill = true;

        Debug.Log("スキル「" + skill.skillName + "」を選択しました");
    }

    public void AddSkill(SkillData skill)
    {
        if (skill == null) return;                                                                                  

        currentSkills.Add(skill);                                                                   // スキルリストに追加
        UpdateSkillPanel();                                                                                     

        Debug.Log("スキル「" + skill.skillName + "」を習得しました");
    }
    private void UpdateSkillPanel()                                                                           
    {
        Debug.Log(skillSlots == null);
        for (int i = 0; i < skillSlots.Length; i++)
        {
            if (i < currentSkills.Count)
            {
                skillSlots[i].SetSkill(currentSkills[i], this);                                     // スキルスロットにスキルをセット
            }
            else
            {
                skillSlots[i].SetSkill(null, this);
            }
        }
    }

    public bool TrySkillAttack(EnemyManager enemy)
    {
        if (enemy == null) return false;                                                                        

        if (!TryUseMp(currentSkill.mpCost))                                                                     
        {
            Debug.Log("MPが足りない！");
            return false;
        }

        int dmg = MakeMagicDamage();                                                                           
        enemy.TakeMagic(dmg);                                                                                   
        return true;
    }

    // プレイヤーの行動を処理する
    public override IEnumerator Act()                                                                           
    {  
        Debug.Log("プレイヤーの行動（入力待ち）");

        acted = false;

        // 現在は1対1戦闘前提のため、シーン内のEnemyManagerを1体取得
        targetEnemy = UnityEngine.Object.FindAnyObjectByType<EnemyManager>();                                   
        if (targetEnemy == null)                                                                                                            
        {
            acted = true;
            yield break;
        }

        // プレイヤー入力で行動が確定するまで待機
        while (!acted)                                                                                         
        {
            yield return null;                                                                               
        }

        yield break;
    }
    
    void Update()
    {
           
    }
}