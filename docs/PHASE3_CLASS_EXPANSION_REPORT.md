# Phase 3 — 3職・23技能・新敵3体 実装報告

日付: 2026-10-25  
対象: C:\mygame1 / Unity 2023.2.22f1  
ブランチ: main / HEAD 831725864bb4adb4ae4c96a551efb2130e0bf2f1（Phase2complete）

## 結果

3職をTownから選択可能にし、固有9技能・共通5技能・敵9技能を追加した。新敵3体は独立したPrefab・部位Collider・AIを持ち、非ボス階2・3の出現候補に接続済み。既存4階・既存敵5種・最終階の騎士王を保持した。

実Unityのコンパイル・EditMode 55件・PlayMode 5件が最終実行で成功。既存23件を含む今回の実行結果であり、過去の件数の流用ではない。PlayModeは自動操作とCamera.Renderによる画像検証で、手動の通しプレイ・配布ビルド試験は未実施。追加の特殊報酬・装備・新ダンジョンへは進んでいない。

## 1. 調査・保護したローカル状態

- 対象版はProjectSettings/ProjectVersion.txtから2023.2.22f1と確認。
- 作業開始時のstatus/diff、変更ファイルのコピー・SHA256、ソース・Sceneの退避を `Logs/ClassExpansion/baseline*` と `source-before` に保存した。
- ユーザーが未保存Sceneを保存してEditorを終了した後、実行中Editorが0であることを確認し、同一プロジェクトの二重起動を避けた。保存されたTownの3職画像・既存Quest構造を基準にした。
- 現在のローカルには生命吸収・集中攻撃・精密射撃・ランダム3候補が既に存在していた。古いmainの状態で上書きしていない。
- 画像meta、旧フォント等の既存差分を保護。Unityによるフォントやcsproj/slnの自動更新は試験後に開始時コピーへ戻した。
- 既存敵のデータ・Prefab・metaは開始時のソース退避とSHA256照合し、変更0件。
- 新規23技能以外の既存SkillDataの設定は変更していない。Packages・ProjectSettingsの意味のある設定変更なし。既存スクリプトの元のCP932を復元した。
- design_manifest.jsonは対象プロジェクト・提供資料の場所に見つからず、提供Markdownを正として実装した。
- Gitのcommit/push/pull/reset/clean/強制checkout/履歴変更は実行していない。

### 依頼資料と違う現在値

| 項目 | ローカル確認値・扱い |
| --- | --- |
| カウンター | 実本体HP減少量の170%を維持 |
| ファイヤー | critBonus=0.05、QuestのplayerCritRate=0.02で合計7%。「確定会心」へ無断変更しない |
| 王の重撃 | power=300、2ターン構え。資料の230へ戻さない |
| 毒 | 3ターン・最大HP5%・CD4、75%戦術解禁、50%初回特殊行動を維持 |
| Mage初期値 | 800/150/30/80/0/20を維持 |

## 2. 3職・戦闘中状態・成長

| 職 | HP | MP | AT | MAG | DEF | MDEF | 初期技能 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 戦士 | 1000 | 100 | 55 | 40 | 4 | 8 | 固有3＋共通5＝8 |
| 魔法使い | 800 | 150 | 30 | 80 | 0 | 20 | 旧4＋固有3＋共通5＝12 |
| バーサーカー | 1500 | 60 | 95 | 10 | 0 | 0 | 固有3＋共通5＝8 |

Mage旧4技能はファイヤー・アイス・ヒール・マナチャージ。新規の技能は次表の職業固有3と共通5。所持アセットとSkillIdによる職業チェックで、敵技能・他職固有技の混入を防ぐ。

Townの既存画像対応（戦士img112、魔法使いimg025、バーサーカーimg221）を保持し、3つの選択ボタンを有効化。ボタン上の不要なPlayerSelectionManager重複を除き、選択保存→Quest遷移の順を接続。選択画面の能力表示、PlayerData、Questの肖像・能力・技能が同じ職を参照する。

### 戦技習得

`PlayerBattleState` の部位種HashSetで戦士だけが取得。実際の新規破壊をBattleManagerが検出した後に登録し、その破壊攻撃へは遡及しない。

- 顔: 正のMPコストをceil(元コスト×0.8)、最低1。元0は0。
- 手: 以後の直接物理ダメージ×1.15。左右を破壊しても同じ能力は1回。
- 脚: 敵の直接物理・魔法ダメージ×0.85。毒・反射・自傷は対象外。
- 腹: 取得なし。既存の顔防御低下・手効果・脚2回行動は別に維持。
- 戦闘開始・終了で戦闘限定状態をリセット。クエスト成長の取得集合は維持し、新クエストのPlayerManager.Setupで初期化する。

基本成長は1.2倍を維持。戦士=最大HP/AT/DEF、魔法使い=最大HP/MAG/DEF、バーサーカー=最大HP/AT/最大MP。バーサーカーへ無効なDEF0の成長は提示しない。最大MP成長は最大値の増分だけ現在MPを増やす。

## 3. 新規23技能

全アセットのフォルダーは `Assets/Skill Data/Phase3/`。次表のIDに `.asset` を付けたものが各アセットパス。例: `Assets/Skill Data/Phase3/Warrior_BreakSlash.asset`。全23個のフル相対パス・設定値は `Logs/ClassExpansion/created-skills.tsv` にも保存した。

命中設定は全て1.0。新プレイヤー物理技は会心可・critBonus=0、新魔法と新敵技能は会心不可。旧SkillTypeの番号・旧技能データを並べ替えていない。Support相当は既存のSelf対象・Buff/Heal等とSkillIdで処理する。

| ID（=ファイル名） | 表示名 | P | MP / CD | 本体 / 部位 | 実装効果 |
| --- | --- | --- | --- | --- | --- |
| Warrior_BreakSlash | 破砕斬 | 110 | 8 / 1 | 0.9 / 1.6 | 物理、部位重視 |
| Warrior_GuardSlash | 守りの一閃 | 95 | 10 / 2 | 1 / 1 | 命中を問わず成立後に直接被ダメージ25%軽減。次の敵行動終了まで |
| Warrior_TacticalSlash | 戦巧斬 | 150 | 14 / 3 | 1 / 1.2 | 取得した戦技の種類数nで直接ダメージ×(1+0.1n)、上限1.3 |
| Mage_FlamePiercer | 炎槍・穿破 | 180 | 24 / 1 | 0.8 / 2 | 炎魔法、追加火傷なし、会心なし |
| Mage_FrostBind | 氷獄・封圧 | 160 | 30 / 3 | 0.9 / 1.2 | 氷魔法、命中時に既存Frozen付与を100%で試行、会心なし |
| Mage_ArcaneBurst | アーケインバースト | 360 | 45 / 3 | 1.2 / 1 | 無属性魔法、会心なし |
| Berserker_Rend | 裂断 | 160 | 4 / 1 | 1 / 1.5 | 物理、部位重視 |
| Berserker_BloodPrice | 血の代償 | 230 | 0 / 3 | 1.25 / 1 | 最大HP10%をceilで消費。コストで死亡する場合は使用不可。成立後のミスでも返金なし |
| Berserker_WarCry | 不屈の雄叫び | 0 | 8 / 3 | 1 / 1 | 最大HP15%回復＋次の物理攻撃試行2回×1.25。再使用は2回へ更新 |
| Common_Guard | 堅守 | 0 | 0 / 1 | 1 / 1 | 次の敵行動終了まで直接被ダメージ50%軽減。ガード同士は強い方 |
| Common_Aim | 狙い澄まし | 0 | 4 / 2 | 1 / 1 | 次の直接攻撃試行に命中+15ポイント、部位×1.25。ミスでも消費。次のプレイヤーターン終了まで |
| Common_Antidote | 解毒 | 0 | 6 / 4 | 1 / 1 | 現在の毒を解除。毒がなければ不成立、消費なし |
| Common_FirstAid | 応急処置 | 0 | 8 / 3 | 1 / 1 | 最大HP12%回復。満タンでは不成立。MAG非依存・上限あり |
| Common_PartProbe | 牽制打ち | 80 | 3 / 1 | 0.7 / 1.3 | 物理、低コストの部位攻撃 |
| Auditor_ArcaneBolt | 魔弾 | 90 | 5 / 0 | 1 / 1 | 単体魔法 |
| Auditor_ManaLevy | 魔力徴収 | 70 | 6 / 3 | 1 / 1 | 単体魔法＋命中時に対象の現在MPを最大10減少（吸収なし） |
| Auditor_ChargedLance | 禁呪光槍 | 180 | 14 / 3 | 1 / 1 | 1敵行動構え、次の敵行動で魔法。予告後の新規手破壊で中断、片手破壊で永久封印 |
| Raider_CleavingBlow | 砕き割り | 140 | 4 / 1 | 1 / 1 | 単体物理 |
| Raider_BloodFury | 血気 | 0 | 0 / 3 | 1 / 1 | 最大HP8%をceilで消費。次の物理試行2回×1.25。残回数あり・自傷死時は使用不可 |
| Raider_ChargedAxe | 破山斧 | 220 | 10 / 4 | 1 / 1 | 1敵行動構え、次の敵行動で物理。予告後の新規手破壊で中断、両手破壊で永久封印 |
| Pilgrim_TalismanBolt | 魔札投げ | 110 | 5 / 1 | 1 / 1 | 単体魔法 |
| Pilgrim_TalismanWard | 護符障壁 | 0 | 10 / 3 | 1 / 1 | 次のプレイヤーターン終了まで本体への直接ダメージ30%軽減。部位HPには影響なし |
| Pilgrim_Renewal | 再生祈祷 | 0 | 12 / 3 | 1 / 1 | 最大HP15%回復、上限あり。顔破壊で封印 |

### 実行時の契約

- SkillUseRulesで生存・対象・所持・職業・MP・HP・CD・封印・状態条件を先に検証。失敗時はMP/HP/CD/狙い/雄叫びを消費しない。
- 成立後にコストを支払う。血の代償はHPがコスト以下なら拒否。ミス判定より前の支払いなので、成立後のミスで自傷は戻らない。
- 狙い・雄叫びは攻撃「試行」で消費。Aimは同じターンの脚追加行動へ持ち越せるが、未使用なら次のプレイヤーターン終了で失効。
- 堅守50%と守りの一閃25%は強い方を採用。戦士の脚0.85倍とは乗算。敵が行動スキップした場合も、その敵行動終了でガード失効。
- プレイヤーの通常攻撃と部位攻撃スキルはPreparePlayerAttackを一度経由する。集中攻撃×狙いは部位HPだけに各1回、本体は従来の弱点倍率・技能本体倍率で計算。
- 反射は実際の本体HP減少量×1.7をRaw経路へ渡す。部位増幅とガードは反射を変更しない。
- 敵への直接攻撃と継続ダメージの入口を分け、護符障壁は本体への直接ダメージだけ30%軽減。
- 魔力徴収は命中後に現在MPからmin(10, MP)を減らす。敵自身のMPは技能コスト分だけ減少し、吸収しない。
- CDの既存ターン更新を保持し、UIでは「残り/設定CD」を表示する。

## 4. UIと参照

- Town.unity: 既存3ボタンを再利用。職業・能力ラベルと背景を追加、表題の大きさを調整。
- Quest.unity: 既存SkillSlotUI 4枠を2×2で再利用し、前/次/閉じる・ページ表示を追加。戦士/バーサーカー2ページ、Mage3ページ。
- SkillPanelPagerが画面寸法に応じて枠と送りボタンを配置。名前・実MPコスト・CD・説明をTMPで表示する。
- 新規専用フォント `Assets/Skill Data/Phase3/Phase3UI.asset` に必要356文字を収録。既存NotoSansJPのttfから静的AtlasとMaterialをサブアセット生成し、既存フォントファイルは変更しない。
- スキル画面CanvasのsortingOrder=60と背景で、既存の進行ボタンや肖像が手前へ重なる問題を修正。
- PlayerUIManagerは成長後の最大HP/MPをSliderにも反映。
- GrowthSelectionManagerは既存3ボタン名を限定取得し、ButtonClickedEventを置換して永続/実行時の古いコールバックを除去。確定ガードとQuestManager側の進行ガードを維持。
- Scene・Prefab・新metaはUnity APIで保存。再実行で対象42アセットのGUID維持と3シーンのMissing Script=0を確認した。記録は `stable-guids.tsv`。

## 5. 新敵・素材監査・部位形状

| 敵 / データ・Prefab名 | HP/MP/AT/MAG/DEF/MDEF | 採用画像 | 画像GUID |
| --- | --- | --- | --- |
| 魔導監察官 / Auditor | 440/80/10/26/12/28 | Assets/Script/Charcter/img139.png | f1145c6ce61efa240a39349e31772ad6 |
| 血斧の略奪者 / Raider | 700/45/24/0/8/8 | Assets/Script/Charcter/img250.png | 659e6b87b66a65944a07bb98f2a1e6bb |
| 護符の巡礼者 / Pilgrim | 560/90/8/18/20/25 | Assets/Script/Charcter/img133.png | f6b8235838ace7f489f06a69b47e8194 |

EnemyData・Prefabは `Assets/Script/Enemy/Expansion/{Auditor,Raider,Pilgrim}.{asset,prefab}`。

Charcterの全19画像をコンタクトシート化して確認し、3職用素材は除外した。Script側とImages側に同一バイトの画像があるため、GUIDだけでなくSHA256が一致する全別名を調べた。Scene/Prefab/assetのGUID全文検索、AssetDatabase.GetDependencies、Resources/Addressables/文字列読み込みも確認した。

候補img316はImages側の既存エリート敵に使用されていたため除外。役割に合う未使用img133へ変更した。img139は杖を持つ術師、img133は杖・盾を持つ巡礼者に対応。img250は筋肉質の格闘家で、斧は描かれていない。HP/AT型の外見として採用し、専用の斧素材制作は範囲外とした。

- 初期監査: `Logs/ClassExpansion/unity-audit.txt`
- 同一画像別名・GUID・ハッシュと依存監査: `enemy-artwork-audit.txt`
- 全素材: [コンタクトシート](../Logs/ClassExpansion/character-contact-sheet.png)
- 部位形状: [監察官](../Logs/ClassExpansion/Auditor-colliders.png)、[略奪者](../Logs/ClassExpansion/Raider-colliders.png)、[巡礼者](../Logs/ClassExpansion/Pilgrim-colliders.png)
- PlayMode描画: [監察官](../Logs/ClassExpansion/new-enemy-playmode-0.png)、[略奪者](../Logs/ClassExpansion/new-enemy-playmode-1.png)、[巡礼者](../Logs/ClassExpansion/new-enemy-playmode-2.png)

顔・左右の手・脚・腹の5つを各画像に合わせた新PolygonCollider2Dで作成。古い敵Colliderは流用していない。部位HPは監察官100/手120/脚110、略奪者150/手180/脚150、巡礼者130/手150/脚120。腹は破壊不能。

検証Scene `Assets/Scenes/EnemyValidation.unity` を新規作成し、Build Settingsは変更していない。1/2/3で敵切替、Vで枠線、部位をクリックで対象表示。自動試験では各5部位に他部位と区別できるPhysics2D.OverlapPoint領域があり、LineRenderer枠線が表示されることを確認した。

### AI・封印

- 監察官: 対象MPがある場合は徴収→光槍→魔弾→通常攻撃。顔で徴収、片手で光槍封印。
- 略奪者: 破山斧→血気（強化残数0・HP支払い可）→砕き割り→通常攻撃。顔で血気、両手で破山斧封印。予告後の片手新規破壊はその構えを中断できる。
- 巡礼者: HP60%以下なら回復→障壁未付与なら障壁→魔札→通常攻撃。顔で回復、片手で障壁封印。
- 全て既存MP/CDフィルターを通す。封印は以後の使用を防ぎ、付与済みの血気/障壁を遡って解除しない。
- 監察官・略奪者は既存の構え・行動保持・新規手破壊中断経路を使用。構え開始時はMP/CDを消費せず、実行成立時に一度消費する。

### 通常進行への接続

単独のPrefab/Collider/AI試験後にUnity APIで接続した。階2=監察官・巡礼者、階3=新敵3体を既存候補へ追加。階1と最終階は保持。既存4階の最終階はisBossFloor=falseだが候補が騎士王のみのため、Boss候補を含む階を追加対象から除外した。既存ボス設定を変更していない。`enemy-encounters.txt` に記録。

## 6. 変更ファイル

| 分類 | 変更・追加 |
| --- | --- |
| 戦闘進行・ダメージ | Assets/Script/Battle/BattleManager.cs、PlayerManager.cs、EnemyManager.cs、Body/AttackContext.cs、Body/EnemyPartsController.cs |
| 戦闘限定状態（追加） | Assets/Script/Battle/PlayerBattleState.cs、EnemyBattleState.cs |
| 技能 | Assets/Script/Skill/SkillData.cs、SkillExecutor.cs、SkillCooldowns.cs、新SkillId.cs・SkillUseRules.cs |
| 敵AI | Assets/Script/Enemy/EnemyRole.cs、EnemyAI/EnemyAI.cs、新ExpansionEnemyAction.cs |
| 職業選択・データ | Assets/Script/Town/PlayerSelectButton.cs、PlayerSelectionManager.cs、Player Data/PlayerData.cs・Warrior.asset・Mage.asset・Berserker.asset |
| 成長 | Assets/Script/Quest/GrowthSelectionManager.cs、GrowthRewardId.cs（基本AT/MP IDを末尾追加） |
| 戦闘UI | Assets/Script/UI/Battle/SkillSlotUI.cs、PlayerUIManager.cs、新SkillPanelPager.cs |
| 追加資産 | Assets/Skill Data/Phase3/の23技能とPhase3UIフォント、Assets/Script/Enemy/Expansion/の3データ・3Prefab、Unity生成meta |
| Scene | Assets/Scenes/Town.unity、Quest.unity、新EnemyValidation.unity |
| 検証 | 新EnemyValidationHarness.cs、Assets/Tests/Editor/ClassExpansion*.cs・ExpansionEnemyTests.cs、PlayMode/ClassExpansionPlayModeTests.csとasmdef |
| 既存テスト調整 | FocusedAttackTests.csのfixtureにEditMode用Awake初期化を追加。テスト期待値は変更なし |
| 資料 | AGENTS.md、docs/GAME_DESIGN.md、設計書コピー、本文書 |

BattleManager/PlayerManager/GrowthSelectionManager等は、既存のPhase 3未コミット差分の上に今回分を追加した。GitのHEADとの差分には作業前からの変更も含まれる。`QuestManager.cs` は元のエンコーディングへ戻し、ゲームコードの変更なし。既存DamageRule.csの精密射撃差分も今回の新規実装ではない。

## 7. 実Unity検証

使用経路: UnityCLI 1.0.0-beta.13 → Unity 2023.2.22f1 Editor CLI。Test Framework 1.3.9。Unity 6用Pipelineの導入なし。CLI beta.14更新通知は確認したが更新しなかった。

実行ファイル:
- `C:\Users\shino\AppData\Local\Unity\bin\unity.exe`
- `C:\Program Files\Unity\Hub\Editor\2023.2.22f1\Editor\Unity.exe`

代表コマンド（PowerShell、プロジェクトルート）:

```powershell
$cli = 'C:\Users\shino\AppData\Local\Unity\bin\unity.exe'
$editor = 'C:\Program Files\Unity\Hub\Editor\2023.2.22f1\Editor\Unity.exe'
& $cli run C:\mygame1 --editor-path $editor --timeout 300 --log-file Logs/ClassExpansion/idempotency-pass.log --no-tail --format json -- -executeMethod ClassExpansionAssets.RebuildAndVerifyStableReferences
& $cli test C:\mygame1 --editor-path $editor --mode EditMode --output Logs/ClassExpansion/regression-editmode.xml --timeout 300 --format json -- -nographics -logFile C:\mygame1\Logs\ClassExpansion\regression-editmode.log
& $cli test C:\mygame1 --editor-path $editor --mode PlayMode --filter ClassExpansionPlayModeTests --output Logs/ClassExpansion/final-playmode.xml --timeout 300 --format json -- -logFile C:\mygame1\Logs\ClassExpansion\final-playmode.log
```

| 最終実行 | 作成/対象件数 | 実行 | 成功 | 失敗/未実行 | CLI / Editor終了コード |
| --- | --- | --- | --- | --- | --- |
| 新規EditMode | 32 | 32 | 32 | 0 / 0 | 0 / 0（全体実行） |
| 既存EditMode | 23 | 23 | 23 | 0 / 0 | 0 / 0（全体実行） |
| 新規PlayMode | 5 | 5 | 5 | 0 / 0 | 0 / 0 |
| API再実行・参照検証 | 42アセット、3シーン | 実施 | GUID維持、Missing Scriptなし | なし | 0 / 0 |

最終EditModeの内訳: ClassExpansionTests 19、ExpansionEnemyTests 13、FocusedAttackTests 11、GrowthSelectionManagerTests 8、PlayerLifeDrainTests 4。

- [EditMode XML](../Logs/ClassExpansion/regression-editmode.xml) / [ログ](../Logs/ClassExpansion/regression-editmode.log)
- [PlayMode XML](../Logs/ClassExpansion/final-playmode.xml) / [ログ](../Logs/ClassExpansion/final-playmode.log)
- [GUID維持ログ](../Logs/ClassExpansion/idempotency-pass.log) / [GUID一覧](../Logs/ClassExpansion/stable-guids.tsv)

最初の全EditMode実行 `final-editmode.xml` は54件中46成功・8失敗（CLI1 / Editor2）。EditModeで部位/EnemyPartsControllerのAwakeが呼ばれない既存fixtureを補った後、全55件を再実行して成功した。初期PlayModeでは撮影タイミングの問題で4件失敗し、Camera.Renderへ変更後4件成功、さらに反射死試験を追加して最終5件成功した。フォント作成の初期例外・一時的なEditor補助コードのコンパイルエラーも修正済み。これらの途中ログは削除していない。

### 確認できた範囲

- クラス能力/技能/職業制限、1.2倍成長、戦技の部位種重複防止・リセット・クエスト報酬保持。
- HP/MP/対象/CD事前拒否、回復上限、解毒、ガードの強い方・脚との乗算、Raw反射非軽減、Aim期限、雄叫び2試行。
- Aim×集中攻撃の部位175/本体70（障壁30%）という分離、既存弱点耐性・実本体HPからの170%反射。
- 生命吸収・集中攻撃・報酬候補/除外/二重選択の既存テスト。
- 新敵3体の能力・5部位の識別可能領域・AI優先/MP/CD・自傷死拒否・封印・構え予約/中断/一度の消費。
- 騎士王の75%/50%毒行動・35%重撃意図・両手封印、毒3回×5%・CD4。
- 実Town/Questで3職選択、全技能ページの名前/MP/CD/説明と所持技能対応。
- 実スキルボタンのonClick→部位入力で3職×既存5敵＋新3敵の24組合せを実行し、MPの二重消費なし。これは1試験内の24攻撃試行であり、24件の戦闘完走ではない。
- DebugKillEnemyによる勝利→成長→次戦が一重に進行、全3職のボス前回復、Setupによる新クエスト初期化。
- カウンター死→実QuestFailed→Town→再選択→新QuestでHP/取得状態が初期化。
- 新敵のPlayModeフレーム進行・描画・Collider領域・枠線。

### 描画と未実施を区別

PlayMode内で960×720のRenderTextureへCamera.Renderし、必要なCanvasを撮影時だけScreenSpaceCameraへ切り替えて保存した画像を目視確認した。実行中のゲームフレーム・Unity描画であり、OS画面のキャプチャや人によるマウス操作の通しプレイではない。

確認画像: [Town](../Logs/ClassExpansion/town-three-classes.png)、[技能1](../Logs/ClassExpansion/mage-skills-page-1.png)、[技能2](../Logs/ClassExpansion/mage-skills-page-2.png)、[技能3](../Logs/ClassExpansion/mage-skills-page-3.png)。欠字・旧進行ボタン重なりを修正後に再撮影して確認した。旧4技能の説明欄は元アセットが空欄のため保持している。

外部スタブC#試験をUnity動作確認として扱っていない。配布ビルド、全技能の全敵への実戦完走、通常/エリート/騎士王の全行動パターンを網羅する手動通しプレイ、全画面比率での確認は未実施。最終試験で未実行のテストケースは0だが、これらの未検証範囲がなくなるという意味ではない。

## 8. Unityでの操作・残る事項

追加Inspector設定は不要。Town/Quest/EnemyValidationとPrefabの参照をAPIで保存済み。

1. Townを開いてPlayし、3職の絵・能力と選択結果を確認する。
2. Questでスキルを開き、前/次で全2〜3ページへ到達し、MP/CD/説明・閉じる・通常部位入力を確認する。
3. 戦士で顔/左右の手/脚を新規破壊し、次の攻撃のMP/物理/被ダメージ、次戦でのリセットを確認する。
4. 成長を選択し、職業別の基本候補、既存特殊報酬の引継ぎと新クエスト初期化を確認する。
5. EnemyValidationを開いてPlay。1/2/3、V、顔・両手・脚・腹の実マウスクリックを確認する。
6. 通常進行の階2・3で新敵が出現し、MP枯渇、構え/中断、封印、勝利/敗北/街帰還を確認する。

残る懸念:
- 数値は指定された初期値のまま。新魔法のMP24/30/45は既存FireのMP10に比べ高い。Fireを将来確定会心へ変更するなら相対バランスの再検討が必要だが、今回変更していない。
- 魔法の基礎計算はpowerに単純比例しないため、P360でもP150の2.4倍ダメージにはならない。部位倍率・回復・防御0・各敵のDEF/MDEFを含む通し調整は別途必要。
- 略奪者の採用絵は斧を持たない。機能は実装済みだが、名称に完全一致する専用美術は未制作。
- 現在の最終階isBossFloor=falseは既存設定を保護した。候補が騎士王だけなので出現自体は保たれる。
- 自動試験と描画確認を実施したが、人によるUnity Editorでの通し操作・配布先実機の動作は未確認。
- 今回の対象範囲に未解決のコンパイルエラー・失敗テストはない。追加の特殊報酬には着手していない。
