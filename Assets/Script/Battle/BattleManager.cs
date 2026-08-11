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
    [SerializeField] private EnemyPartsController enemyParts;   // 敵の部位を管理するコンポーネント
    [SerializeField] private Camera mainCamera;                 // メインカメラ。マウスの画面座標をゲーム内の座標へ変換するために使う
    [SerializeField] private float playerBaseAccuracy = 0.9f;   // プレイヤーの命中率（0.0～1.0）
    [SerializeField] private float enemyBaseAccuracy = 0.9f;    // 敵の命中率（0.0～1.0）
    [SerializeField] private float elementProcChance = 0.05f;   // 属性攻撃の追加効果が発動する確率（0.0～1.0）
    [SerializeField] private SkillData playerDefaultSkill;      // プレイヤーのデフォルトスキル（通常攻撃の代わりに使うスキル）
    [SerializeField] private bool useDefaultSkill = false;      // デフォルトスキルを使うかどうかのフラグ（trueならplayerDefaultSkillを使う）
    [Header("UI References")]
    [SerializeField] private GameObject skillSelectionPanel;    // スキル選択UIパネルのもと

    public Transform screenShakeTarget;     // プレイヤーがダメージを受けたときに揺れすようにするため
    public QuestManager questManager;       // QuestManagerの参照
    public PlayerUIManager playerUI;        // PlayerUIManagerの参照
    public EnemyUIManager enemyUI;          // EnemyUIManagerの参\参照
    public PlayerManager player;            // PlayerManagerの参照
    public PlayerData playerData;           // PlayerDataの参照
    private EnemyManager enemy;             // EnemyManagerの参照
    private bool waitingTap;                // タップ待ち中かのフラグ
    private bool isPlayerTurn;              // プレイヤーのターンかのフラグ

    private void Start()
    {
        skillSelectionPanel.SetActive(false);                                           // スキル選択UIは最初は非表示
        enemyUI.gameObject.SetActive(false);                                            // 敵UIは最初は非表示
        playerData = PlayerSelectionManager.Instance.selectedPlayer;                    // PlayerSelectionManagerから選択されたPlayerDataを取得

        if (playerData != null && player != null)                                       // PlayerDateとplayerが存在している場合
        {
            player.Setup(playerData);                                                   // PlayerManagerにPlayerDataをセットアップ

            if (playerData.startSkills != null && playerData.startSkills.Count > 0)     // PlayerDataにstartSkillsが1以上存在している場合
            {
                playerDefaultSkill = playerData.startSkills[0];                         // PlayerDataのstartSkillsの1つ目をplayerDefaultSkillにセット
            }
        }
    }

    public void Setup(EnemyManager enemymanager)        
    {
        SoundManager.instance.PlayBGM("Battle");
        enemyUI.gameObject.SetActive(true);                                             // 敵UIを表示

        enemy = enemymanager;                                                          // EnemyManagerをセット

        if (mainCamera == null) mainCamera = Camera.main;                              // メインカメラを取得
        
        enemyUI.SetupUI(enemy);
        playerUI.SetupUI(player);

        StartCoroutine(BattleLoop());                                                  // バトルループを開始
    }

    private void Update()
    {

        if (!isPlayerTurn) return;                                                      // プレイヤーターンでない場合は何もしない

        if (Input.GetKeyDown(KeyCode.S))                                                // Sキーを押すと
        {
                    skillSelectionPanel.SetActive(!skillSelectionPanel.activeSelf);     // スキル選択UIの表示・非表示を切り替える
            return;
                }

                if (skillSelectionPanel.activeSelf) 
                    return;

                if (Input.GetMouseButtonDown(0))                                        // 左クリックで攻撃対象を選択
            {
                TryPickBodyPart(Input.mousePosition);                                   // 画面上の座標から敵の部位をピックする処理
        }
        
    }

    public void OnSkillSelected(SkillData selectedSkill)                                // スキル選択UIでスキルが選択されたときの処理
    {
        if (selectedSkill == null)                                                      // 選択されたスキルがnullの場合は警告を出して処理を中断
        { 
            Debug.LogWarning
                ("選択されたスキルがnullですわ！");
            return;
        }

        if (selectedSkill.skillType == SkillType.Heal && selectedSkill.targetType == TargetType.Self)
        {
            var result = SkillExecutor.Execute(player, player, selectedSkill);          // スキルを実行して結果を取得            
            if (!result.executed)                                                       // スキルが実行されなかった場合は処理を中断
            {
                DialogTextManager.instance.SetScenarios(new string[]
                {
                    result.message
                });                
                return;                
            }
            PlaySkillEffect(selectedSkill,Vector3.zero);                  // プレイヤーの位置にスキルのエフェクトを再生

            playerUI.UpdateUI(player);

            skillSelectionPanel.SetActive(false);                                       // スキル選択UIを閉じる

            waitingTap = false;
            isPlayerTurn = false;

            DialogTextManager.instance.SetScenarios(new string[]
                {
                    result.message
                });

            return;
        }
        playerDefaultSkill = selectedSkill;                                             // 選択されたスキルをplayerDefaultSkillにセット
        useDefaultSkill = true;                                                         // スキル使用モードON        
        waitingTap = true;                                                              // 敵をクリックするのを待つ状態にする

        skillSelectionPanel.SetActive(false);                                           // スキル選択UIを閉じる        

        Debug.Log($"スキル「{selectedSkill.skillName}」を選択しましたわ！");
    }

    private void TryPickBodyPart(Vector2 screenPos)                                     // 画面上の座標から敵の部位をピックする処理
    {
        Vector2 worldPos = mainCamera.ScreenToWorldPoint(screenPos);                    // 画面上の座標をゲーム内の座標に変換
        RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);                   // クリックした一点にColliderがあるか確認する

        if (!hit.collider) return;                                                      // Colliderが無い場合は何もしない

        var part = hit.collider.GetComponentInParent<BodyPart>();                       // Colliderの親にBodyPartがあるか確認する
        if (part == null) return;                                                       // BodyPartが無い場合は何もしない

        OnBodyPartTapped(part);                                                         // BodyPartがクリックされたときの処理を呼び出す
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

    private AttackContext CreateNormalAttackContext()                                   // 通常攻撃のAttackContextを作って返す
    {
        return new AttackContext                                                        // 新しいAttackContextを1つ作る
        {
            baseDamage = CalculateDamage(null),                                         // 通常攻撃のダメージ計算

            mainDamageRate = 1f,                                                        // 本体へのダメージ倍率
            partDamageRate = 1f,                                                        // 部位へのダメージ倍率
            canApplyStatus = false,                                                     // 状態異常は無し
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

            canApplyStatus = skill.statusEffect != null,                                // 状態異常効果があるかどうか
            sourceSkill = skill                                                         // 攻撃の元となるスキルデータ
        };
    }

    public void OnBodyPartTapped(BodyPart part)                                         // プレイヤーが敵の部位をクリックしたときの処理
    {
        
        if (part == null) return;                                                        // BodyPartがnullの場合は何もしない
        if (!isPlayerTurn) return;                                                      // プレイヤーターンでない場合は何もしない

            enemyParts = part.GetComponentInParent<EnemyPartsController>();             // クリックされた部位の親にEnemyPartsControllerがあるか確認する

        if (enemyParts == null)
        {
            Debug.LogWarning(
                $"部位「{part.GetPartNameJP()}」の親にEnemyPartsControllerが見つかりませんでしたわ！");
            return;
        }

        enemyParts.SetSelectedPart(part);                                               // 選択された部位を設定

        AttackContext ctx;                                                              // 攻撃の情報を作る

        if (useDefaultSkill && playerDefaultSkill != null)                              // スキル使用モードがONで、playerDefaultSkillが設定されている場合
        {
            if (!player.TrySpendMp(playerDefaultSkill.mpCost))                          // スキルのMPコストが足りない場合は警告を出して処理を中断
            {
                Debug.Log("MPが足りませんわ！");
                return;
            }

            ctx = CreateSkillAttackContext(playerDefaultSkill);
        }
        else
        {
            ctx = CreateNormalAttackContext();                                          // 通常攻撃のAttackContextを作る
        }

        waitingTap = false;                                                             // タップ待ち状態を解除
        isPlayerTurn = false;                                                           // プレイヤーターン終了

        if (ctx.sourceSkill != null)
        {
            Vector3 clickPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);     // クリックした位置をワールド座標に変換
            clickPos.z = 0f;

            PlaySkillEffect(ctx.sourceSkill, clickPos);                                 // クリックした位置にスキルのエフェクトを再生
        }

        var result = enemyParts.ApplyAttack(ctx);                                       // 攻撃を適用してダメージ計算を行う

        if (
            ctx.sourceSkill != null &&                                                  // スキルがあり、かつ
            ctx.sourceSkill.statusEffect != null &&                                     // スキルに状態異常効果があり、かつ
            enemy != null &&                                                            // 敵が存在し、かつ
            enemy.IsAlive                                                               // 敵が生存している場合
)
        {
            StatusEffectData effect =
                ctx.sourceSkill.statusEffect;                                           // スキルの状態異常効果をeffectに代入

            float chance =
                ctx.sourceSkill.applyChance > 0f
                    ? ctx.sourceSkill.applyChance                                       // スキルのapplyChanceが0より大きい場合はスキルのapplyChanceを使用
                    : effect.applyChance;                                               // スキルのapplyChanceが0の場合は状態異常効果のapplyChanceを使用

            if (Random.value <= chance)                                                 // ランダムで状態異常効果を適用するか判定
            {
                int duration =
                    ctx.sourceSkill.overrideDurationTurns > 0
                        ? ctx.sourceSkill.overrideDurationTurns                         // スキルのoverrideDurationTurnsが0より大きい場合はスキルのoverrideDurationTurnsを使用
                        : effect.durationTurns;                                         // スキルのoverrideDurationTurnsが0の場合は状態異常効果のdurationTurnsを使用

                if (effect.type == StatusEffectType.Burn)
                {
                    enemy.ApplyBurn(effect, duration);                                  // 敵に火傷状態異常を付与
                }
            }
        }

        if (useDefaultSkill)                                                            // スキル使用モードだった場合
        {
            useDefaultSkill = false;                                                    // スキル使用モードをOFFにする
            playerDefaultSkill = null;                                                  // playerDefaultSkillをnullにする
        }

        if (enemy != null)
        {
            enemyUI.UpdateUI(enemy);
        }
        playerUI.UpdateUI(player);

        string attackMessage;                                                           // 攻撃のメッセージを作る。この時点ではまだ空

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

        DialogTextManager.instance.SetScenarios(new[]                                   // ダイアログに攻撃メッセージを表示
        {
            attackMessage
        });

        Debug.Log(attackMessage);                                                       // 攻撃メッセージをデバッグログに出力
    }    

    public static BattleManager Instance;

    void Awake()
    {
        Instance = this;                                                                // BattleManagerを1つのインスタンスに設定
    }
    public void PlaySkillEffect(SkillData skill, Vector3 worldPos)                      // SkillDataからもらったデータをworldPosの座標を基準にしてエフェクトを再生する
    {        
        if (skill == null) return;                                                      // SkillDataがnullの場合は何もしない
        if (skill.effectPrefab == null) return;                                         // SkillDataのeffectPrefabがnullの場合は何もしない

        Vector3 pos = worldPos + (Vector3)skill.effectOffset;                           // エフェクトの表示位置をworldPosにeffectOffsetを加えた位置に設定
        pos.z = 0f;                                                                     // Zは0固定

        GameObject effect = Instantiate(
            skill.effectPrefab,                                                         // エフェクトのPrefabを生成
            pos,                                                                        // 生成位置はpos
            Quaternion.identity                                                         // 回転は無し
        );

        Renderer[] renderers = effect.GetComponentsInChildren<Renderer>(true);          // エフェクトのRendererを取得（子オブジェクトも含む）
        foreach (var r in renderers)                                                    // 取得したRendererに対して
        {
            r.sortingLayerName = "Default";                                             // Sorting Layerを"Default"に設定
            r.sortingOrder = 10;                                                        // Sorting Orderを10に設定
        }

        Destroy(effect, skill.effectDuration);                                          // エフェクトの表示時間が経過したら削除
    }

    IEnumerator BattleLoop()                                                            // プレイヤーと敵のターンを交互に繰り返す
    {
        while (                                                                         // 条件が成立している間繰り返す
            player != null &&
            enemy != null &&
            player.IsAlive &&
            enemy.IsAlive
        )
        {

            yield return StartCoroutine(PlayerActByTap());                              // 必ずプレイヤーが先に行動

            if (
                player == null ||
                enemy == null ||
                !player.IsAlive ||
                !enemy.IsAlive
            )
            {
                yield break;                                                            // 条件が成立しない場合はループを抜ける
            }

            yield return new WaitForSeconds(0.5f);                                      // 少し待機

            yield return StartCoroutine(EnemyActAuto());                                // プレイヤーの後に敵が行動

            if (
                player == null ||
                enemy == null ||
                !player.IsAlive ||
                !enemy.IsAlive
            )
            {
                yield break;                                                            // 条件が成立しない場合はループを抜ける
            }

            yield return new WaitForSeconds(0.5f);
        }
    }
    IEnumerator PlayerActByTap()                                                        // プレイヤーターン：タップ待ちで攻撃
    {
        isPlayerTurn = true;
        waitingTap = true;

        DialogTextManager.instance.SetScenarios(new string[]                            // 画面に表示するメッセージを設定
        {
        "プレイヤーのターン。\n敵をクリック！"
        });

        while (waitingTap)                                                              
            yield return null;                                                          // 敵がクリックされるまで1フレーム待つ

        yield return new WaitForSeconds(1.5f);                                          // 攻撃のテキストメッセージを読める時間を作る

        isPlayerTurn = false;                                                           // プレイヤーターン終了

        if (enemy != null)
        {
            enemyUI.UpdateUI(enemy);                                                    // 敵のUIを更新
        }

        if (!enemy.IsAlive)
            yield return StartCoroutine(EndBattle());                                   // 敵が倒れていた場合EndBattle()を呼び出し、終わるまで待つ(yield return)
    }

    IEnumerator EnemyActAuto()
    {
        if (enemy == null || player == null) yield break;

        int burnDmg = enemy.TickBurnDamage();                                           // 敵の火傷ダメージを計算して返す
        if (burnDmg > 0)
        {
            enemyUI.UpdateUI(enemy);                                                    // 敵のHPのUIを更新
            DialogTextManager.instance.SetScenarios(new string[]                        // ダイアログ表示するメッセージを設定
            {
            $"火傷ダメージ！\n敵は{burnDmg}ダメージ受けた"
            });

            yield return new WaitForSeconds(0.5f);

            if (enemy == null || !enemy.IsAlive)                                        // 敵が存在していない、または敵が倒れている場合
            {
                yield return StartCoroutine(EndBattle());                               // EndBattle()を呼び出し、終わるまで待つ(yield return)
                yield break;
            }

            var cooldowns = enemy.GetComponent<SkillCooldowns>();

            if (cooldowns != null)
            {
                cooldowns.Tick();
            }
        }

        yield return new WaitForSeconds(0.5f);

        float finalAcc = enemyBaseAccuracy - enemy.GetAccuracyPenalty();                // 敵の命中率ー命中率のデバフ(デバフは開発途中)
        bool hit = DamageRule.RollHit(finalAcc, 0f);                                    // 命中判定を行う（敵の命中率、プレイヤーの回避率。現在プレイヤーの回避率は未実装）

        if (!hit)                                                                       // 攻撃が外れた場合
        {
            DialogTextManager.instance.SetScenarios(new string[]
            {
            "敵の攻撃！\nしかし外れた！"
            });
            yield break;
        }

        SkillData skill = PickEnemySkillOrNormal();                                     // 敵のスキルをランダムで1つ選ぶ（通常攻撃も含む）

        if (skill == null)
        {
            SoundManager.instance.PlayButtonSE(1);                                      // ボタンSEを再生
            screenShakeTarget.DOShakePosition(0.3f, 0.5f, 20, 0, false, true);          // プレイヤー中の画面を揺らす

            int dmg = player.TakePhysical(enemy.at);                                    // プレイヤーに物理ダメージを与える
            playerUI.UpdateUI(player);                                                  // プレイヤーのHPのUIを更新
            DialogTextManager.instance.SetScenarios(new string[]
            {
            $"敵の攻撃！\nプレイヤーは{dmg}ダメージ受けた"
            });
            yield break;
        }

        var result = SkillExecutor.Execute(                                             // スキルを実行して結果を取得
            enemy,
            player,
            skill,
            0f
        );

        if (result.crit)                                                                // クリティカルが発生した場合
        {
            Debug.Log("クリティカル！");
        }

        playerUI.UpdateUI(player);                                                      // プレイヤーのHPのUIを更新

        DialogTextManager.instance.SetScenarios(new string[]
        {
        result.message                                                                  // SkillExecutor.Execute()の結果メッセージを表示
        });
    }
    private SkillData PickEnemySkillOrNormal()                                          // 敵が持っているスキルの中からランダムで1つ選ぶ（通常攻撃も含む）
    {
        if (enemy == null || enemy.data == null) return null;                       

        List<SkillData> pool = new();                                                   // スキルの候補に空のリスト

        if (enemy.data.attackSkill != null)                                             // 通常攻撃スキルが設定されている場合
            pool.Add(enemy.data.attackSkill);                                           // 通常攻撃スキルを候補に追加

        if (enemy.data.skillList != null && enemy.data.skillList.Count > 0)             // スキルリストが設定されている場合
        {
            foreach (var s in enemy.data.skillList)                                     // スキルリストの中身を1つずつ確認
            {
                if (s == null) continue;                                                // nullのスキルはパスする
                pool.Add(s);                                                            // スキルを候補に追加
            }
        }

        if (pool.Count == 0) return null;                                               // 候補が0の場合はnullを返す

        return pool[Random.Range(0, pool.Count)];                                       // 候補の中からランダムで1つ返す
    }

   
    
    IEnumerator EndBattle()                                                             // 戦闘終了時の処理
    {
        yield return new WaitForSeconds(2f);                                            // 戦闘終了後の2秒間演出を待つ
        DialogTextManager.instance.SetScenarios(new string[]
        {
            "モンスターはやられた。"
        });
        enemyUI.gameObject.SetActive(false);
        SoundManager.instance.PlayBGM("Quest");                                         // BGMの切り替え
        questManager.EndBattle();                                                       // QuestManagerのEndBattle()を呼び出す
        Debug.Log("戦闘終了");
    }
}
