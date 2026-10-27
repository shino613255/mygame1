# スキル選択UI・6技能整理・既存VFX活用の実装報告

> この報告のUI寸法・文字サイズは前回作業の記録。続く大型化で文字30、技能620×300、ページ送り230×90へ変更した。現在の配置と検証結果は [大型UI報告](SKILL_UI_LARGE_REPORT.md) を優先する。所持技能・説明文・VFX・戦闘設定はそのまま保持している。

実施日: 2026-10-27。対象: `C:/mygame1`、Unity 2023.2.22f1。

今回の指示を、従来の「固有3＋共通5」「Mageの旧4技能をすべて所持」より優先した。3職とも選択式技能を合計6個に整理し、既存4枠・2ページで表示する。技能そのものの追加・削除や、能力値・威力・コスト・CDの調整は行っていない。

## 1. 変更範囲と参照の扱い

| 対象 | 変更内容 |
| --- | --- |
| `C:/mygame1/Assets/Script/UI/Battle/SkillSlotUI.cs` | TMPの名前・説明を合計3行以内へ調整。MP/CDを別のTMP欄へ表示。実描画で20から16まで調整する下限を設ける |
| `C:/mygame1/Assets/Script/UI/Battle/PlayerUIManager.cs` | UIに描かれた使用者の肖像位置をワールド座標へ変換する演出用メソッド |
| `C:/mygame1/Assets/Script/Battle/BattleManager.cs` | 成立した自己技能・命中した攻撃だけVFXを再生。攻撃位置は部位Collider中心。開始・終了・無効化時に演出を消去 |
| `C:/mygame1/Assets/Script/Battle/SkillVfxController.cs`（新規） | VFX生成・描画順・寿命・一括破棄を担当。技能効果やダメージ計算は担当しない |
| `C:/mygame1/Assets/Script/Battle/PlayerManager.cs` | `AddSkill`でも所持6個の上限を確認。既存の職業・所持判定を保持 |
| `C:/mygame1/Assets/Script/Player Data/{Warrior,Mage,Berserker}.asset` | 初期技能リストのみ6個へ整理 |
| `C:/mygame1/Assets/Scenes/Quest.unity` | 既存4スロットのTMPフォント・文字サイズ設定だけ更新。ボタン・イベント・敵・階層の参照は保持 |
| 後述の15個のSkillData | 説明文と演出参照・演出時間のみ。旧Fire/Heal/MpHealは既存の演出設定を維持し、説明文を追加 |
| `C:/mygame1/Assets/Skill Data/Presentation/`（新規） | 専用TMPフォント1個と、既存VFXを入れた小さな派生Prefab7個 |
| `C:/mygame1/Assets/Tests/Editor/{ClassExpansionAssets,ClassExpansionTests}.cs` | 旧8/12個の期待値・初期化補助を現行6個へ合わせる |
| `C:/mygame1/Assets/Tests/Editor/{SkillPresentationAssets,SkillPresentationVfx,SkillPresentationTests}.cs`（新規） | Unity APIによる設定・素材監査・描画、所持制限と数値保護の試験 |
| `C:/mygame1/Assets/Tests/PlayMode/ClassExpansionPlayModeTests.cs` | 全ページの描画・表示検査を追加し、現行の所持技能で既存試験を実行 |
| `C:/mygame1/Assets/Tests/PlayMode/SkillPresentationPlayModeTests.cs`（新規） | 全採用技能の演出・成立条件・資源・消去・敵の共有ヒールを検査 |
| `C:/mygame1/AGENTS.md`、`C:/mygame1/docs/GAME_DESIGN.md` | 各職合計6個、TMP表示、演出の現行方針を更新 |
| `C:/mygame1/docs/{BREAK_TACTICS_Classes_Skills_Enemies_v1,PHASE3_CLASS_EXPANSION_REPORT}.md` | 旧8/12技能は当時の記録であり、現在は本報告の6個構成を優先する旨を追記 |

Scene・PlayerData・SkillData・新規Prefab・フォントは対象版UnityのAPIで更新した。Questの既存オブジェクトを作り直していない。既存`.meta`の変更・技能アセットの削除はない。`SkillSelectionPanel.prefab`には今回のSkillSlotUIがなく、最終的に元のファイルを保持した。独立した新規VFX Prefabを用い、元の共有VFXは変更していない。

既存日本語フォントに広範な字形追加をせず、現在の採用技能に必要な字形を既存NotoSansJPから専用の静的TMPアトラスへ生成した。`SkillMenuUI.asset`のGUIDは `6a15d5996a422b34d9766b5c26b8eff2`。既存4枠が同じフォントを使い、実行時に複製するMP/CD欄もそれを継承する。Inspectorでの手動接続は不要。

## 2. 各職の最終所持技能

通常攻撃・常時パッシブ・成長報酬は、この6個に数えない。SkillDataの初期リスト、PlayerManagerの実行時所持リスト、ページ送りで到達する技能が一致する。

| 職業 | 採用した6技能 | 選定理由 |
| --- | --- | --- |
| 戦士 | 破砕斬、戦巧斬、堅守、狙い澄まし、応急処置、解毒 | 部位破壊を起点に、戦技を活かす攻撃・準備・大技対処・HP管理・毒対処を残す |
| 魔法使い | ファイヤー、炎槍・穿破、氷獄・封圧、アーケインバースト、ヒール、マナチャージ | 低消費の反復魔法、部位特化、状態異常、本体撃破、HP・MP回復を分担する |
| バーサーカー | 裂断、血の代償、不屈の雄叫び、狙い澄まし、堅守、解毒 | 部位攻撃・HP代償の本体火力・回復付き強化を中心に、準備と危険行動・毒への対処を残す |

### 所持から外した技能

| 職業 | 外した技能 | 理由 |
| --- | --- | --- |
| 戦士 | 守りの一閃 | 攻撃兼軽減の役割が破砕斬・戦巧斬・堅守と重なる。部位破壊後の攻撃と、毒・反射を除く大技軽減を別の選択肢にする |
| 戦士 | 牽制打ち | 低消費部位攻撃が破砕斬と重なる。MP不要の通常攻撃も残る |
| 魔法使い | アイス | 氷獄・封圧と氷属性・凍結の役割が重なる。低消費反復枠にはファイヤーを残す |
| 魔法使い | 堅守、狙い澄まし、応急処置、解毒、牽制打ち | 6枠で攻撃魔法4用途とHP/MP管理を優先。応急処置はMAGを活かすヒール、牽制打ちは部位用魔法と重なる。防御・解毒を外すのは職業の取捨選択であり、万能化はしない |
| バーサーカー | 応急処置 | 同じMP/CDで最大HP15%回復と物理強化を得る雄叫びに集約する |
| バーサーカー | 牽制打ち | 裂断と部位攻略の役割が重なる |

外したSkillData・共有効果・VFXは削除していない。敵のヒール利用も保持する。所持していない技能の直接実行は既存のSkillUseRulesで拒否する。

戦士の現在値はHP1000/MP100/AT55/MAG40/DEF4/MDEF8、Mageは800/150/30/80/0/20、Berserkerは1500/60/95/10/0/0。戦士はHP/ATが中間で、DEFは3職中最高のため「基礎が平均より少し低め」に全面的には一致しない。今回は調整禁止に従い維持した。調整する場合は、まず同じ敵への破壊前・頭/手/脚破壊後の与被ダメージと生存ターンを比較し、戦技による伸び幅を確認する案とする。変更値は今回決めていない。

## 3. 説明文の変更前後

表の`／改行／`は実際の改行。MP消費とCDは別欄へ表示するため、ここに重複記載しない。Pは現在のSkillDataの威力を表し、直接の確定ダメージ値ではない。計算式・耐性・部位倍率などによって最終値が決まる。

| スキル | 変更前 | 変更後 |
| --- | --- | --- |
| 破砕斬 | 物理P110。本体0.9倍、部位1.6倍。部位破壊を狙う。 | 物理P110/本体0.9倍・部位1.6倍 |
| 戦巧斬 | 物理P150、部位1.2倍。戦技の種類ごとに直接ダメージ+10%、最大1.3倍。 | 物理P150/部位1.2倍／改行／戦技数で直接ダメージ1～1.3倍 |
| 堅守 | 次の敵行動終了まで直接物理・魔法ダメージ50%軽減。毒・反射・自傷は軽減しない。 | 次の敵行動終了まで直接ダメージ半減／改行／毒・反射・自傷は軽減しない |
| 狙い澄まし | 次の直接攻撃の命中+15ポイント、部位1.25倍。本体は増えない。ミスでも消費。次のプレイヤーターン終了まで。 | 次撃の命中+15pt/部位1.25倍／改行／ミス消費・次の自ターン終了まで |
| 応急処置 | MAGに関係なく最大HPの12%回復。満タンでは使用不可。最低1、最大HPまで。 | 最大HPの12%回復（上限あり）／改行／HP満タンでは使用できない |
| 解毒 | 現在の毒のみ解除。毒でなければ消費なし。永続免疫なし。 | 毒を解除（HP回復なし）／改行／毒でない時は使用できない |
| 裂断 | 物理P160。部位1.5倍。 | 物理P160/部位1.5倍 |
| 血の代償 | 最大HPの10%を自傷コストに物理P230、本体1.25倍。自傷死するHPでは使用不可。ミスでも自傷は戻らない。 | 物理P230/本体1.25倍／改行／最大HP10%消費/自傷HP0不可・ミス消費 |
| 不屈の雄叫び | 最大HP15%回復。次の物理攻撃2回を1.25倍。ミスでも回数消費。再使用は残り2回へ更新。 | 最大HP15%回復／改行／次の物理2回を1.25倍・ミス消費 |
| 炎槍・穿破 | 炎魔法P180。本体0.8倍、部位2倍。追加火傷なし。会心なし。 | 炎魔法P180/本体0.8倍・部位2倍／改行／部位破壊用・追加火傷なし |
| 氷獄・封圧 | 氷魔法P160。本体0.9倍、部位1.2倍。命中時に既存の凍結を100%で付与試行。会心なし。 | 氷魔法P160/本体0.9倍・部位1.2倍／改行／命中で凍結3T試行/AT・MAG半減 |
| アーケインバースト | 無属性魔法P360。本体1.2倍。MPを大きく使う撃破用魔法。会心なし。 | 魔法P360/本体1.2倍・部位1倍／改行／MPを多く使う本体撃破用 |
| ファイヤー | 空欄 | 炎魔法P150/本体・部位攻撃／改行／命中時10%で火傷を付与試行 |
| ヒール | 空欄 | 自分のHPを80+MAG×0.5回復／改行／最大HPを超えない |
| マナチャージ | 空欄 | 自分のMPを30回復／改行／最大MPを超えない |

凍結は付与「試行」であり、既存の火傷中は凍結しないルールを変更していない。新魔法の会心可否も変更していない。元の全文とアセットの対応は `C:/mygame1/Logs/SkillPresentation/description-changes.txt` に保存した。

## 4. UIの実描画検証

UnityのCamera/RenderTextureで、各職2ページ×3解像度=18画面を生成した。TMPのForceMeshUpdate後のlineCount・fontSize・overflow・表示範囲と、ボタン同士・ページ送りとの重なりを検査した。キャプチャ時だけCanvasをScreenSpaceCameraへ切り替え、CanvasScalerの設定は維持している。SceneのCanvasモードは変更していない。

| 解像度 | 各技能ボタンの幅×高さ | 名前・説明の文字サイズ | 名前・説明の描画行数 | MP/CD欄 |
| --- | --- | --- | --- | --- |
| 1920×1080（基準） | 400×195 | 20 | 2～3行 | 16 |
| 960×600 | 400×195 | 20 | 2～3行 | 16 |
| 800×600 | 365×195 | 18～20 | 2～3行 | 16 |

800×600の「血の代償」は18、その他の検証枠は20。最低16は設定上の下限で、検証した54表示（18所有技能×3解像度）では18未満になっていない。ボタンを一括縮小して条件を満たす処理は追加していない。切り取り・省略記号・maxVisibleLinesでの隠蔽・空の説明も使っていない。

名前と説明の合計が2行の技を除き、基本は名前1行＋説明2行。MP/CDは既存TMPを一度だけ複製した子オブジェクトに分離する。ボタン本体と既存OnClickは再利用し、4枠をページ単位で再設定する。UIレイキャストによって部位選択に操作を流さない既存経路を保持し、試験でも確認した。

`C:/mygame1/Logs/SkillPresentation/ui-metrics.tsv` に測定値を保存した。複数回試験の記録を追記しているため、同じ画面・技能の行が複数ある。

### 各職・各ページの保存画像

| 職業 | 1920×1080 | 960×600 | 800×600 |
| --- | --- | --- | --- |
| 戦士 | [1頁](C:/mygame1/Logs/SkillPresentation/ui-Warrior-1920x1080-page-1.png) / [2頁](C:/mygame1/Logs/SkillPresentation/ui-Warrior-1920x1080-page-2.png) | [1頁](C:/mygame1/Logs/SkillPresentation/ui-Warrior-960x600-page-1.png) / [2頁](C:/mygame1/Logs/SkillPresentation/ui-Warrior-960x600-page-2.png) | [1頁](C:/mygame1/Logs/SkillPresentation/ui-Warrior-800x600-page-1.png) / [2頁](C:/mygame1/Logs/SkillPresentation/ui-Warrior-800x600-page-2.png) |
| 魔法使い | [1頁](C:/mygame1/Logs/SkillPresentation/ui-Mage-1920x1080-page-1.png) / [2頁](C:/mygame1/Logs/SkillPresentation/ui-Mage-1920x1080-page-2.png) | [1頁](C:/mygame1/Logs/SkillPresentation/ui-Mage-960x600-page-1.png) / [2頁](C:/mygame1/Logs/SkillPresentation/ui-Mage-960x600-page-2.png) | [1頁](C:/mygame1/Logs/SkillPresentation/ui-Mage-800x600-page-1.png) / [2頁](C:/mygame1/Logs/SkillPresentation/ui-Mage-800x600-page-2.png) |
| バーサーカー | [1頁](C:/mygame1/Logs/SkillPresentation/ui-Berserker-1920x1080-page-1.png) / [2頁](C:/mygame1/Logs/SkillPresentation/ui-Berserker-1920x1080-page-2.png) | [1頁](C:/mygame1/Logs/SkillPresentation/ui-Berserker-960x600-page-1.png) / [2頁](C:/mygame1/Logs/SkillPresentation/ui-Berserker-960x600-page-2.png) | [1頁](C:/mygame1/Logs/SkillPresentation/ui-Berserker-800x600-page-1.png) / [2頁](C:/mygame1/Logs/SkillPresentation/ui-Berserker-800x600-page-2.png) |

## 5. VFXの素材監査と割り当て

Prefab名だけで選ばず、元素材9種類の構成・ParticleSystemのループ/寿命・マテリアル・シェーダー対応状況と、0.12/0.3/0.6秒の実描画を確認した。使用する主マテリアルの欠落や非対応シェーダーは検出していない。ParticleSystemの未使用Trail Materialの空欄は、トレイル無効時の任意欄として記録した。

最初に検討した `C:/mygame1/Assets/Effect Assets/motion/CFXR Prefabs/Nature/CFXR3 Shield Leaves A (Lit).prefab` は、現状の2D照明では暗く、葉の演出も堅守の判別に不向きだったため採用しなかった。比較画像は `C:/mygame1/Logs/SkillPresentation/rejected-GuardLeaves.png`。堅守には青紫の保護バブル、狙いには集中する光、解毒には煙と星の解除表現を採用した。解毒は色だけで毒解除を説明する専用演出ではないため、毒アイコン消去とログも併せて意味を伝える。

### 派生Prefabと元素材

| 派生Prefab（すべて新規） | 既存の元素材 | スケール |
| --- | --- | --- |
| `C:/mygame1/Assets/Skill Data/Presentation/PhysicalVfx.prefab` | `C:/mygame1/Assets/Effect Assets/motion/CFXR Prefabs/Sword Trails/Plain/CFXR4 Sword Hit PLAIN (Cross).prefab` | 1.0 |
| `C:/mygame1/Assets/Skill Data/Presentation/ArcaneVfx.prefab` | `C:/mygame1/Assets/Effect Assets/motion/CFXR Prefabs/Impacts/CFXR Impact Glowing HDR (Blue).prefab` | 0.65 |
| `C:/mygame1/Assets/Skill Data/Presentation/GuardVfx.prefab` | `C:/mygame1/Assets/Effect Assets/motion/CFXR Prefabs/Magic Misc/CFXR4 Bouncing Glows Bubble (Blue Purple).prefab` | 0.55 |
| `C:/mygame1/Assets/Skill Data/Presentation/AimVfx.prefab` | `C:/mygame1/Assets/Effect Assets/motion/CFXR Prefabs/Light/CFXR3 LightGlow A (Loop).prefab` | 1.0 |
| `C:/mygame1/Assets/Skill Data/Presentation/DispelVfx.prefab` | `C:/mygame1/Assets/Effect Assets/motion/CFXR Prefabs/Misc/CFXR Magic Poof.prefab` | 0.6 |
| `C:/mygame1/Assets/Skill Data/Presentation/FireVfx.prefab` | `C:/mygame1/Assets/Skill Data/Skill Effect/Fire.prefab` | 0.75 |
| `C:/mygame1/Assets/Skill Data/Presentation/IceVfx.prefab` | `C:/mygame1/Assets/Skill Data/Skill Effect/Ice.prefab` | 0.75 |

派生側だけでループ・カメラ揺れ・ライトを無効化し、CFXR側の自動破棄と新コントローラーの寿命管理が競合しないようにした。元Prefab・マテリアル・画像のGUIDを変えていない。外部素材や画像生成は利用していない。

### 技能ごとの最終参照

| 技能 | effectPrefabのパス | 寿命 | 場所 |
| --- | --- | --- | --- |
| 破砕斬 | `C:/mygame1/Assets/Skill Data/Presentation/PhysicalVfx.prefab` | 1秒 | 命中部位 |
| 戦巧斬 | `C:/mygame1/Assets/Skill Data/Presentation/PhysicalVfx.prefab` | 1秒 | 命中部位 |
| 裂断 | `C:/mygame1/Assets/Skill Data/Presentation/PhysicalVfx.prefab` | 1秒 | 命中部位 |
| 血の代償 | `C:/mygame1/Assets/Skill Data/Presentation/PhysicalVfx.prefab` | 1秒 | 命中部位 |
| 堅守 | `C:/mygame1/Assets/Skill Data/Presentation/GuardVfx.prefab` | 1秒 | 使用者 |
| 狙い澄まし | `C:/mygame1/Assets/Skill Data/Presentation/AimVfx.prefab` | 0.8秒 | 使用者 |
| 応急処置 | `C:/mygame1/Assets/Skill Data/Skill Effect/Heal.prefab` | 1秒 | 使用者 |
| 解毒 | `C:/mygame1/Assets/Skill Data/Presentation/DispelVfx.prefab` | 1秒 | 使用者 |
| ファイヤー | `C:/mygame1/Assets/Skill Data/Skill Effect/Fire.prefab`（既存） | 1秒（維持） | 命中部位 |
| 炎槍・穿破 | `C:/mygame1/Assets/Skill Data/Presentation/FireVfx.prefab` | 1秒 | 命中部位 |
| 氷獄・封圧 | `C:/mygame1/Assets/Skill Data/Presentation/IceVfx.prefab` | 1秒 | 命中部位 |
| アーケインバースト | `C:/mygame1/Assets/Skill Data/Presentation/ArcaneVfx.prefab` | 1秒 | 命中部位 |
| ヒール | `C:/mygame1/Assets/Skill Data/Skill Effect/Heal.prefab`（既存） | 1秒（維持） | 使用者。敵の共有利用時も敵自身 |
| マナチャージ | `C:/mygame1/Assets/Skill Data/Skill Effect/Mp Heal.prefab`（既存） | 1.5秒（維持） | 使用者 |

offsetは全対象で(0,0)。Z=0、Defaultレイヤー/Order10として、背景より前に表示する。スキル選択UIは攻撃確定前に閉じる既存の流れを維持し、演出のための待機時間は追加しない。コントローラーはBattleManagerの子に生成し、技能の寿命・戦闘Setup・EndBattle・OnDisable・Scene破棄に従って消去する。

攻撃のVFXを命中判定後へ移し、ミス時には命中演出を出さない。自己技能もSkillExecutorのexecutedが真の場合だけ再生する。演出側からSkillExecutorやダメージ・回復を呼ぶ経路はない。

監査全文・GUID・割り当て一覧は `C:/mygame1/Logs/SkillPresentation/{vfx-source-audit.txt,vfx-assignments.tsv}`。元素材プレビューは `source-*.png`、派生側は `assigned-*.png`。

## 6. Unityでの演出画像

採用技能は3職計18所有枠、重複を除く15技能を実行し、1920×1080で発生前後の画像を保存した。各画像名は `C:/mygame1/Logs/SkillPresentation/vfx-{職業}-{SkillData名}.png`。before画像も同じ場所にある。対象付近200×200ピクセル内の画像差分と実際の生成位置も検査した。

| 演出 | 確認画像 |
| --- | --- |
| 物理命中 | [血の代償](C:/mygame1/Logs/SkillPresentation/vfx-Berserker-Berserker_BloodPrice.png) |
| 炎命中 | [炎槍・穿破](C:/mygame1/Logs/SkillPresentation/vfx-Mage-Mage_FlamePiercer.png) |
| 氷命中 | [氷獄・封圧](C:/mygame1/Logs/SkillPresentation/vfx-Mage-Mage_FrostBind.png) |
| 魔力命中 | [アーケインバースト](C:/mygame1/Logs/SkillPresentation/vfx-Mage-Mage_ArcaneBurst.png) |
| 防御 | [堅守](C:/mygame1/Logs/SkillPresentation/vfx-Warrior-Common_Guard.png) |
| 準備 | [狙い澄まし](C:/mygame1/Logs/SkillPresentation/vfx-Warrior-Common_Aim.png) |
| 解除 | [解毒](C:/mygame1/Logs/SkillPresentation/vfx-Warrior-Common_Antidote.png) |
| HP回復 | [ヒール](C:/mygame1/Logs/SkillPresentation/vfx-Mage-Skill_Heal.png) |
| MP回復 | [マナチャージ](C:/mygame1/Logs/SkillPresentation/vfx-Mage-Skill_MpHeal.png) |
| 敵自身への共有ヒール | [騎士のヒール](C:/mygame1/Logs/SkillPresentation/vfx-shared-enemy-heal.png) |

共有ヒール試験の画像はEditor既定の640×480で、敵への演出位置を確認するためのもの。640×480全体のUI対応を保証する試験には含めない。

## 7. 実行環境・コマンド

Unity CLI実体は `C:/Users/shino/AppData/Local/Unity/bin/unity.exe`、版は1.0.0-beta.13。ヘルプとEditor一覧を確認した。更新通知はあったが更新していない。プロジェクト版・実行版はともに2023.2.22f1、Editor実体は `C:/Program Files/Unity/Hub/Editor/2023.2.22f1/Editor/Unity.exe`。

Unity 6向けPipelineへの接続・導入は利用せず、CLIのrun/testから指定した2023版Editorを起動する経路を使用した。ユーザーが未保存Scene/Prefabを保存してEditorを終了した後、既存Editorがないことを確認して順次実行した。同一プロジェクトの二重起動・強制終了・別プロジェクト接続・Unity/Package更新は行っていない。

PowerShellでの主要コマンド（`$cli`と`$editor`は上記の絶対パス）:

```powershell
& $cli run C:\mygame1 --editor-path $editor --timeout 300 --log-file Logs/SkillPresentation/prepare-ui-confirmed.log --no-tail --format json -- -executeMethod SkillPresentationAssets.PrepareUi -nographics
& $cli run C:\mygame1 --editor-path $editor --timeout 300 --log-file Logs/SkillPresentation/finalize-vfx.log --no-tail --format json -- -executeMethod SkillPresentationAssets.FinalizeVfx
& $cli test C:\mygame1 --editor-path $editor --mode EditMode --output Logs/SkillPresentation/editmode-final.xml --timeout 300 --format json -- -nographics -logFile C:\mygame1\Logs\SkillPresentation\editmode-final.log
& $cli test C:\mygame1 --editor-path $editor --mode PlayMode --filter ClassExpansionPlayModeTests --output Logs/SkillPresentation/playmode-verified.xml --timeout 600 --format json -- -logFile C:\mygame1\Logs\SkillPresentation\playmode-verified.log
```

描画を必要とする実行では`-nographics`を付けていない。外部C#スタブによる検証は今回実施しておらず、対象版Unityが実際にコンパイル・実行している。

## 8. 自動テストの結果と範囲

EditMode: 60件成功、失敗0、スキップ0。

| テスト群 | 件数 | 主な確認 |
| --- | --- | --- |
| ClassExpansionTests | 19 | 3職の現能力値、所持6個、職業制限、戦技の戦闘リセット、HPコスト非致死、ガード・狙い・雄叫び・毒・MP/HP不足、23技能IDの保持、字形 |
| ExpansionEnemyTests | 13 | 新敵のAI/MP/封印/構え/妨害、部位Collider、騎士王の毒閾値・重撃300・封印 |
| FocusedAttackTests | 11 | 1→1.2→1.4、部位限定、敵変更/ミス/破壊後リセット、生命吸収同時取得、実本体HPに基づく反射、弱点耐性 |
| GrowthSelectionManagerTests | 8 | 重複なし3候補、取得済み除外、20%成長、二重クリック、精密射撃・新クエスト初期化 |
| PlayerLifeDrainTests | 4 | 最大HP5%、上限、同部位重複なし、破壊不能除外、新クエスト初期化 |
| SkillPresentationTests（追加） | 5 | 初期/実行時の各職6個、外した技能の直接実行拒否、6個の追加上限、演出参照、全SkillDataの数値保護 |

数値保護試験は作業前バックアップとの16項目の比較で、MP/CD/威力/倍率/命中/会心/属性/対象/部位対象可否/本体・部位率/状態/バフ/デバフ/付与率が対象。バックアップはローカルのLogs配下に保存しているため、別PCでそれがない場合、この比較1件は明示的にIgnoreする。他の所持・演出・実行試験は実行できる。

PlayMode: 8件成功、失敗0、スキップ0。最終結果XMLは `C:/mygame1/Logs/SkillPresentation/playmode-verified.xml`。EditMode/PlayModeの最終コンパイルにC#の警告・エラーはない。

| テスト | 検証内容 |
| --- | --- |
| TownSelectionAndEverySkillPageWorkForAllClasses | Townから各職選択、Quest遷移、全技能到達、表示名/説明/MP/CD、18画面と54表示の実レイアウト |
| ThreeClassesAttackAllEightEnemiesWithoutDoubleSpending | 3職×既存/新敵8体へ選択技能で部位攻撃、連続部位入力によるMP二重消費防止 |
| NewEnemySceneAdvancesFramesRendersAndSupportsAllPartClicks | 既存検証Sceneの新敵3体、描画・フレーム進行・5領域Collider |
| GrowthAdvancesOnceBossRecoversAllClassesAndNewQuestResetsRewards | 成長選択からの一度だけの進行、全職のボス前HP/MP回復、新クエストで成長・所持を初期化 |
| CounterDeathReturnsToTownAndNewQuestClearsRewards | 反射死の敗北・Town帰還・再出発・成長初期化 |
| EveryRetainedSkillUsesOneVfxAndCleansUpWithoutChangingCombatCosts（追加） | 18所有枠を実選択して使用、位置・可視性・1回生成・MP/HP/CD・寿命後消去・演出による二重回復や二重コストなし |
| FailedSelectionMissAndUiClicksDoNotCreateImpactEffects（追加） | HP/MP不足・CD・選択だけ・ミスで命中VFXなし、UIから部位へ入力が流れない、終了時消去 |
| SharedHealUsesTheEnemyPositionAndBattleEndRemovesEffects（追加） | 既存騎士AIから共有ヒール、HP100→180の1回回復、敵自身への表示・終了時消去 |

以前のPlayMode実行では8件中1件が失敗した。共有ヒール試験がBattleManagerの既存の先頭子オブジェクトをVFXと誤認していたことが原因で、生成したVFXを取得するよう試験を修正した。ゲームの回復位置を直した失敗ではない。修正後の実行では8件成功を確認した。素材監査初回の任意Trail欄の誤判定と、実装途中のコンパイルエラーも解消済み。途中ログも残しているため、最終XMLと区別する。

## 9. 保護した設定と、検証の限界

カウンター170%、FireのcritBonus0.05＋Quest基礎0.02（7%）、王の重撃300・2ターン構え、毒3ターン/最大HP5%/CD4を維持した。生命吸収・集中攻撃・抽選処理、敵AI・弱点耐性・忍者の体勢崩し・階層構成・文字送り・クリック消費・Vキー・自動ハイライトなしの処理に変更を加えていない。

既存の未コミット変更は開始時点でなかった。元のファイルを `C:/mygame1/Logs/SkillPresentation/baseline/` とSHA256一覧へ記録し、変更範囲を確認した。1686ファイルとの最終比較で、既存.meta、敵データ/画像/Prefab、共有VFX、Packages、ProjectSettingsに変更はなく、許可した範囲外の差分も0件だった。結果は `C:/mygame1/Logs/SkillPresentation/final-preservation-audit.json`。AGENTSは元のUTF-16、既存の日本語C#ファイルは元の文字コードを保持した。Unityが自動再生成したcsproj/slnや不要な保存差分は作業前のコピーへ戻した。GitのCommit/Push/Pull/Reset/Cleanは行っていない。

自動試験で確認できたのは、上記の制御された成立条件・コスト・遷移・描画である。3職それぞれで通常速度のダンジョン全階層を人が操作して攻略する通しプレイ、全敵の全AI分岐と全技能・全部位の組合せ、実ビルド、別GPUでの描画は未実施。画像は実Unityの描画だが、EditorのGameビューを人が連続操作した記録ではない。Phase 2のすべての体感・バランスまで自動試験で保証したとは扱わない。

## 10. Unityでの確認操作と残る確認

1. 2023.2.22f1で `C:/mygame1` を開き、Townから各職を選んでQuestへ入る。スキル画面で1/2、2/2を送り、上記6個だけが表示・実行できることを確認する。Inspectorの追加設定は不要。
2. Gameビューを1920×1080、960×600、800×600にして、文字の読み心地とクリック範囲を確認する。800未満、縦長、極端に横長の表示は今回の保証範囲外。
3. 通常速度で破砕斬/血の代償/炎・氷・魔力攻撃を部位へ使い、回復・補助は左の肖像位置へ出ることを確認する。ミス・資源不足・CDでは演出が出ず、戦闘終了・帰還で残らないことも見る。
4. 戦士は破壊で戦技を得た後のMP表示と次戦リセット、MageはMP不足からのマナチャージ、BerserkerはHPコストと反射を合わせた生存判断を試す。
5. 騎士王の毒と構え、忍者の体勢崩し、予告の妨害、成長選択、文字送りを挟む通常速度の通しプレイで、操作感に違和感がないか確認する。

実装を妨げる参照破損や、今回の最終自動試験に残った失敗は検出していない。解毒VFXの直感的な分かりやすさ、短い物理命中の見え方、各職6枠での攻略バランスは手動試遊の評価が必要。専用静的フォントは今回の文字集合に合わせているため、今後説明を変更する際は必要字形と3行条件を再確認する。

今回のUI・技能構成・既存VFX活用の範囲で停止する。追加技能・敵・成長報酬には進まない。
