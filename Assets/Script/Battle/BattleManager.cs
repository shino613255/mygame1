using DG.Tweening;
using System.Collections;
// PlayerとEnemyの戦闘を管理するクラスusing System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class BattleManager : MonoBehaviour
{
    [SerializeField] private EnemyPartsController enemyParts;                           
    [SerializeField] private Camera mainCamera;                                         
    [SerializeField] private float playerBaseAccuracy = 1f;                             
    [SerializeField] private float enemyBaseAccuracy = 1f;                              
    [SerializeField] private float elementProcChance = 0.05f;                           // 属性攻撃の追加効果が発動する確率                  
    [SerializeField] private SkillData selectedPlayerSkill;                              
    [SerializeField] private bool useSelectedSkill = false;                              

    [Header("UI References")]
    [SerializeField] private GameObject skillSelectionPanel;                            

    public Transform screenShakeTarget;                                                 // プレイヤーがダメージを受けたときに揺れすようにするため
    public QuestManager questManager;                                                   
    public PlayerUIManager playerUI;                                                    
    public EnemyUIManager enemyUI;                                                     
    public PlayerManager player;                                                        
    public PlayerData playerData;                                                      
    private EnemyManager enemy;                                                       
    private EnemyAI enemyAI;                                                            
    private bool waitingTap;                                                           
    private bool isPlayerTurn;

    void Awake()
    {
        enemyAI = GetComponent<EnemyAI>();
        Instance = this;
    }

    private void Start()
    {
        skillSelectionPanel.SetActive(false);                                          
        enemyUI.gameObject.SetActive(false);                                           
        playerData = PlayerSelectionManager.Instance.selectedPlayer;                    

        if (playerData != null && player != null)                                       
        {
            player.Setup(playerData);                                                  

            if (playerData.startSkills != null && playerData.startSkills.Count > 0)     
            {
                selectedPlayerSkill = playerData.startSkills[0];                         
            }
        }
    }

    public void Setup(EnemyManager enemymanager)        
    {
        SoundManager.instance.PlayBGM("Battle");
        enemyUI.gameObject.SetActive(true);                                            

        enemy = enemymanager;                                                           

        if (mainCamera == null) mainCamera = Camera.main;                               
        
        enemyUI.SetupUI(enemy);
        playerUI.SetupUI(player);

        StartCoroutine(BattleLoop());                                                   
    }

    private void Update()
    {

        if (!isPlayerTurn) return;                                                      

        if (Input.GetKeyDown(KeyCode.S))                                                
        {
                    skillSelectionPanel.SetActive(!skillSelectionPanel.activeSelf);     
            return;
                }

                if (skillSelectionPanel.activeSelf) 
                    return;

                if (Input.GetMouseButtonDown(0))                                        
            {
                TryPickBodyPart(Input.mousePosition);                                   
        }
        
    }

    public void OnSkillSelected(SkillData selectedSkill)                                
    {
        if (selectedSkill == null)                                                      
        { 
            Debug.LogWarning
                ("選択されたスキルがnullですわ！");
            return;
        }

        if (selectedSkill.skillType == SkillType.Heal && selectedSkill.targetType == TargetType.Self)
        {
            var result = SkillExecutor.Execute(player, player, selectedSkill);          // スキルを実行して結果を取得            
            if (!result.executed)                                                       
            {
                DialogTextManager.instance.SetScenarios(new string[]
                {
                    result.message
                });                
                return;                
            }
            PlaySkillEffect(selectedSkill,Vector3.zero);                                // プレイヤーの位置にスキルのエフェクトを再生

            playerUI.UpdateUI(player);

            skillSelectionPanel.SetActive(false);                                      

            waitingTap = false;
            isPlayerTurn = false;

            DialogTextManager.instance.SetScenarios(new string[]
                {
                    result.message
                });

            return;
        }
        selectedPlayerSkill = selectedSkill;
        useSelectedSkill = true;                                                         
        waitingTap = true;                                                              

        skillSelectionPanel.SetActive(false);                                           

        Debug.Log($"スキル「{selectedSkill.skillName}」を選択しましたわ！");
    }

    private void TryPickBodyPart(Vector2 screenPos)                                    
    {
        Vector2 worldPos = mainCamera.ScreenToWorldPoint(screenPos);                    // 画面上の座標をゲーム内の座標に変換
        RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);                   // クリックした一点にColliderがあるか確認する

        if (!hit.collider) return;                                                      

        var part = hit.collider.GetComponentInParent<BodyPart>();                       // Colliderの親にBodyPartがあるか確認する
        if (part == null) return;                                                       

        OnBodyPartTapped(part);                                                         
    }

    private int CalculateDamage(SkillData skill)
    {
        // 通常攻撃
        if (skill == null)
        {
            return DamageRule.CalcPhysical(
                player.at,
                enemy.def,
                1f,
                1
            );
        }

        // 魔法スキル
        if (skill.skillType == SkillType.Magic)
        {
            return DamageRule.CalcMagic(
                player.mag,
                enemy.mdef,
                skill.multiplier,
                1
            ) + skill.power;
        }

        // 物理スキル
        return DamageRule.CalcPhysical(
            player.at,
            enemy.def,
            skill.multiplier,
            1
        ) + skill.power;
    }

    private AttackContext CreateNormalAttackContext()                                 
    {
        return new AttackContext                                                       
        {
            baseDamage = CalculateDamage(null),                                     

            mainDamageRate = 1f,                                                        
            partDamageRate = 1f,                                                        
            canApplyStatus = false,                                                     
            sourceSkill = null
        };
    }
    private AttackContext CreateSkillAttackContext(SkillData skill)
    {
        return new AttackContext
        {
            baseDamage = CalculateDamage(skill),

            mainDamageRate = skill.mainDamageRate,
            partDamageRate = skill.partDamageRate,

            canApplyStatus = skill.statusEffect != null,                               
            sourceSkill = skill                                                         
        };
    }

    public void OnBodyPartTapped(BodyPart part)                                         
    {
        
        if (part == null) return;                                                       
        if (!isPlayerTurn) return;                                                      

            enemyParts = part.GetComponentInParent<EnemyPartsController>();             // クリックされた部位の親にEnemyPartsControllerがあるか確認する

        if (enemyParts == null)
        {
            Debug.LogWarning(
                $"部位「{part.GetPartNameJP()}」の親にEnemyPartsControllerが見つかりませんでしたわ！");
            return;
        }

        enemyParts.SetSelectedPart(part);

        // 攻撃の情報を作る
        AttackContext ctx;                                                             

        if (useSelectedSkill && selectedPlayerSkill != null)                              
        {
            if (!player.TryUseMp(selectedPlayerSkill.mpCost))                           
            {
                Debug.Log("MPが足りませんわ！");
                return;
            }

            ctx = CreateSkillAttackContext(selectedPlayerSkill);
        }
        else
        {
            ctx = CreateNormalAttackContext();                                         
        }

        waitingTap = false;                                                            
        isPlayerTurn = false;                                                             

        if (ctx.sourceSkill != null)
        {
            Vector3 clickPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);     
            clickPos.z = 0f;

            PlaySkillEffect(ctx.sourceSkill, clickPos);                                 
        }

        float accuracy =
            ctx.sourceSkill != null
                ? ctx.sourceSkill.accuracy
                : playerBaseAccuracy;

        bool hit = DamageRule.RollHit(
            accuracy,
            enemy.evasionRate
        );

        if (!hit)
        {
            if (useSelectedSkill)
            {
                useSelectedSkill = false;
                selectedPlayerSkill = null;
            }
            playerUI.UpdateUI(player);

            DialogTextManager.instance.SetScenarios(new string[]
            {
        $"{player.name}の攻撃！\nしかし{enemy.name}に当たらなかった！"
            });

            return;
        }

        //EnemyPartsControllerで本体・部位へのダメージを計算して適用する
        var result = enemyParts.ApplyAttack(ctx);                                       

        if (
            ctx.sourceSkill != null &&                                                  
            ctx.sourceSkill.statusEffect != null &&                                     
            enemy != null &&                                                            
            enemy.IsAlive                                                               
)
        {
            StatusEffectData effect =
                ctx.sourceSkill.statusEffect;                                           

            // SkillData側に個別設定があれば、StatusEffectDataより優先する
            float chance =
                ctx.sourceSkill.applyChance > 0f
                    ? ctx.sourceSkill.applyChance                                       
                    : effect.applyChance;                                               

            if (Random.value <= chance)                                                
            {
                // スキルの状態異常効果ターンに関する設定
                int duration =
                    ctx.sourceSkill.overrideDurationTurns > 0
                        ? ctx.sourceSkill.overrideDurationTurns                         
                        : effect.durationTurns;                                         

                if (effect.type == StatusEffectType.Burn)
                {
                    enemy.ApplyBurn(effect, duration);                                  
                }
            }
        }

        if (useSelectedSkill)                                                         
        {
            useSelectedSkill = false;                                                   
            selectedPlayerSkill = null;                                                  
        }

        if (enemy != null)
        {
            enemyUI.UpdateUI(enemy);
        }
        playerUI.UpdateUI(player);

        string attackMessage;                                                           

        if (ctx.sourceSkill != null)
        {
            attackMessage = $"{ctx.sourceSkill.skillName}！\n{part.GetPartNameJP()}に攻撃！";
        }
        else
        {
            attackMessage = $"{part.GetPartNameJP()}を攻撃！";
        }

        if (result.mainDamage > 0)
        {
            attackMessage += $"\n本体に{result.mainDamage}ダメージ！";
        }

        if (result.partDamage > 0)
        {
            attackMessage += $"\n部位に{result.partDamage}ダメージ！";
        }
        else if (part.IsBroken)
        {
            attackMessage += "\nその部位はもう破壊されていますわ！";
        }

        DialogTextManager.instance.SetScenarios(new[]                                   
        {
            attackMessage
        });

        Debug.Log(attackMessage);                                                       
    }    

    public static BattleManager Instance;

    
    public void PlaySkillEffect(SkillData skill, Vector3 worldPos)                    
    {        
        if (skill == null) return;                                                      
        if (skill.effectPrefab == null) return;                                        

        Vector3 pos = worldPos + (Vector3)skill.effectOffset;                           // エフェクトの表示位置をworldPosにeffectOffsetを加えた位置に設定
        pos.z = 0f;                                                                    

        GameObject effect = Instantiate(
            skill.effectPrefab,                                                        
            pos,                                                                        
            Quaternion.identity                                                         
        );

        Renderer[] renderers = effect.GetComponentsInChildren<Renderer>(true);          // エフェクトのRendererを取得

        foreach (var r in renderers)                                                    
        {
            r.sortingLayerName = "Default";                                             
            r.sortingOrder = 10;                                                        
        }

        Destroy(effect, skill.effectDuration);                                          
    }

    IEnumerator BattleLoop()                                                            
    {
        while (                                                                         
            player != null &&
            enemy != null &&
            player.IsAlive &&
            enemy.IsAlive
        )
        {
            // 必ずプレイヤーが先に行動
            yield return StartCoroutine(PlayerActByTap());                              

            if (
                player == null ||
                enemy == null ||
                !player.IsAlive ||
                !enemy.IsAlive
            )
            {
                yield break;                                                           
            }

            yield return new WaitForSeconds(0.5f);                                      

            yield return StartCoroutine(EnemyActAuto());                                

            if (
                player == null ||
                enemy == null ||
                !player.IsAlive ||
                !enemy.IsAlive
            )
            {
                yield break;                                                            
            }

            yield return new WaitForSeconds(0.5f);
        }
    }
    IEnumerator PlayerActByTap()                                                        
    {
        isPlayerTurn = true;
        waitingTap = true;

        DialogTextManager.instance.SetScenarios(new string[]                            
        {
        "プレイヤーのターン。\n敵をクリック！"
        });

        while (waitingTap)                                                              
            yield return null;                                                          

        // 攻撃のテキストメッセージを読める時間を作る
        yield return new WaitForSeconds(1.5f);                                          

        isPlayerTurn = false;                                                           

        if (enemy != null)
        {
            enemyUI.UpdateUI(enemy);                                                    
        }

        if (!enemy.IsAlive)
            yield return StartCoroutine(EndBattle());                                   
    }

    IEnumerator EnemyActAuto()
    {
        if (enemy == null || player == null) yield break;

        int burnDmg = enemy.TickBurnDamage();                                          
        if (burnDmg > 0)
        {
            enemyUI.UpdateUI(enemy);                                                    
            DialogTextManager.instance.SetScenarios(new string[]                       
            {
            $"火傷ダメージ！\n敵は{burnDmg}ダメージ受けた"
            });

            yield return new WaitForSeconds(0.5f);

            if (enemy == null || !enemy.IsAlive)                                       
            {
                yield return StartCoroutine(EndBattle());                            
                yield break;
            }

            var cooldowns = enemy.GetComponent<SkillCooldowns>();

            if (cooldowns != null)
            {
                cooldowns.Tick();
            }
        }

        yield return new WaitForSeconds(0.5f);

        // TODO: 部位破壊による命中率低下を反映
        float finalAcc = enemyBaseAccuracy - enemy.GetAccuracyPenalty();

        // TODO: 第2引数にプレイヤー回避率を渡す
        bool hit = DamageRule.RollHit(finalAcc, 0f);                                  

        if (!hit)                                                                      
        {
            DialogTextManager.instance.SetScenarios(new string[]
            {
            "敵の攻撃！\nしかし外れた！"
            });
            yield break;
        }

        SkillData skill = enemyAI.ChooseSkill(enemy);                                   

        if (skill == null)
        {
            SoundManager.instance.PlayButtonSE(1);                                      
            screenShakeTarget.DOShakePosition(0.3f, 0.5f, 20, 0, false, true);         

            int dmg = player.TakePhysical(enemy.at);                                    
            playerUI.UpdateUI(player);                                                  
            DialogTextManager.instance.SetScenarios(new string[]
            {
            $"敵の攻撃！\nプレイヤーは{dmg}ダメージ受けた"
            });
            yield break;
        }

        UnitBase target;

        if (skill.targetType == TargetType.Self)
        {
            target = enemy;
        }
        else
        {
            target = player;
        }

        var result = SkillExecutor.Execute(                                             
            enemy,
            target,
            skill,
            0f
        );

        enemyUI.UpdateUI(enemy);

        if (result.hit && skill.targetType != TargetType.Self)                           
            // 攻撃が命中し、ダメージが0より大きい場合
        {
            SoundManager.instance.PlayButtonSE(1);                                      
            screenShakeTarget.DOShakePosition(0.3f, 0.5f, 20, 0, false, true);          
        }        

        if (result.crit)                                                                
        {
            Debug.Log("クリティカル！");
        }

        playerUI.UpdateUI(player);                                                      

        DialogTextManager.instance.SetScenarios(new string[]
        {
        result.message                                                                
        });
    }
   
    
    IEnumerator EndBattle()                                                            
    {
        // 戦闘終了後の2秒間演出を待つ
        yield return new WaitForSeconds(2f);                                            
        DialogTextManager.instance.SetScenarios(new string[]
        {
            "モンスターはやられた。"
        });
        enemyUI.gameObject.SetActive(false);
        SoundManager.instance.PlayBGM("Quest");                                         
        questManager.EndBattle();                                                       
        Debug.Log("戦闘終了");
    }
}
