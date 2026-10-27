# スキル選択画面の文字・ボタン大型化

日付: 2026-10-27。対象: `C:/mygame1`、Unity 2023.2.22f1。

文字は名前・説明・MP/CDとも固定30。技能ボタンを620×300、前へ/次へを230×90へ拡大した。説明文、各職の6技能、威力・コスト・CD・戦闘処理は変更していない。

## 1. 開始時点の保護と変更ファイル

前回作業などの未コミット変更が残っていたため、今回開始時の1726ファイルを `C:/mygame1/Logs/SkillUiLarge/baseline/` へ退避し、SHA256とGit差分も保存した。以前の作業前ではなく、今回開始時の状態と比較して保護する。

| 変更対象 | 内容 |
| --- | --- |
| `C:/mygame1/Assets/Script/UI/Battle/SkillSlotUI.cs` | 30未満へ縮める処理を削除。名前・説明・MP/CDを固定30へ。文字欄の余白と高さを拡大 |
| `C:/mygame1/Assets/Script/UI/Battle/SkillPanelPager.cs` | RectTransformによる大型化、画面幅に応じた1/2列、技能領域だけのスクロール、ページ送り・閉じる・ページ番号の再配置 |
| `C:/mygame1/Assets/Scenes/Quest.unity` | Unity APIで既存ラベルのサイズとFont Size Min/Maxを30へ、既存ボタン・文字欄の寸法を更新。既存オブジェクト・イベント参照は保持 |
| `C:/mygame1/Assets/Tests/Editor/SkillUiLargeAssets.cs`（新規） | 変更前の寸法・Canvas・文字幅の記録、およびSceneのUI設定更新 |
| `C:/mygame1/Assets/Tests/PlayMode/SkillUiLargePlayModeTests.cs`（新規） | 全文へのスクロール到達性、拡大した範囲のレイキャスト、ページ送り・再表示の検査 |
| `C:/mygame1/Assets/Tests/PlayMode/ClassExpansionPlayModeTests.cs` | 全職・全ページの5解像度検査と今回専用フォルダーへの画像保存 |
| `C:/mygame1/Assets/Tests/PlayMode/SkillPresentationPlayModeTests.cs` | 大型化の寸法・文字30・クリック領域を検査。実Buttonのポインタークリックで既存技能を実行 |
| `C:/mygame1/AGENTS.md`、`C:/mygame1/docs/GAME_DESIGN.md` | UIの文字下限を30へ更新 |
| `C:/mygame1/docs/SKILL_UI_LOADOUT_VFX_REPORT.md` | 以前のUI寸法は過去の記録であり、本報告を優先する旨を追記 |

新規C#のmetaはUnity生成。既存Prefab・既存meta・フォントアセットは変更せず、同じTMPフォントを使用する。GitのCommit/Push/Pull/Resetは行っていない。

最後に今回開始時の1726ファイルと照合し、既存ファイルの変更は上記UI・関連試験・仕様書の8件だけだった。許可範囲外の変更0、既存metaの変更0。SkillData（説明文・数値・VFX参照を含む）、PlayerData、戦闘コード、敵AI、成長報酬、既存Prefab、Packages、ProjectSettingsは開始時と一致する。Unityが自動生成したcsprojや試験設定の差分は今回の開始時コピーへ戻した。結果は `C:/mygame1/Logs/SkillUiLarge/final-audit.json`。

## 2. 寸法の変更前後

変更前はSceneに保存された寸法と実行時の寸法が異なった。Scene保存値は285×155だが、既存SkillPanelPagerが画面サイズに応じて実行時に上書きしていた。最大の実行時寸法400×195を今回の基準とし、それに対しても縦横1.5倍以上を満たした。元の読み取り記録は `C:/mygame1/Logs/SkillUiLarge/before-ui.txt`。

| 対象 | 変更前の幅×高さ | 変更後 | 幅の倍率 | 高さの倍率 |
| --- | --- | --- | --- | --- |
| 技能ボタン（1920×1080/1280×720/960×600/900×1200） | 400×195 | 620×300 | 1.55倍 | 約1.538倍 |
| 技能ボタン（800×600） | 365×195 | 620×300 | 約1.699倍 | 約1.538倍 |
| 前へ | 150×55 | 230×90 | 約1.533倍 | 約1.636倍 |
| 次へ | 150×55 | 230×90 | 約1.533倍 | 約1.636倍 |
| 閉じる | 160×50 | 240×75 | 1.5倍 | 1.5倍 |

寸法はRectTransform/GridLayoutGroupのcellSizeで指定する。技能ボタンのlocalScaleは1。背景ImageとButtonは同じオブジェクトをそのまま使用し、クリック領域も同時に拡大した。旧400幅の範囲を超える、中心から横286の場所でも各技能ボタンにレイキャストが届くことを検査した。

## 3. 文字サイズと行数

全技能で名前・説明は同じTMP欄に入り、固定30、Auto Sizeオフ、Font Size Min/Maxとも30。MP/CDも別のTMP欄で固定30。前へ/次へ/閉じる/ページ番号も30に統一した。

文字領域はボタン内の左右20、上20、下74を余白とし、実寸580×206。MP/CD欄は左右20、下14～60。TMPで最長の説明の必要幅を測定すると約552.94だったため、文章を書き換えずに収まる。30未満への縮小、説明省略、三点リーダー、表示行数を強制的に隠す処理は追加していない。

| 職業 | スキル | 名前＋説明の描画行数 | 文字サイズ |
| --- | --- | --- | --- |
| 戦士 | 破砕斬 | 2 | 30 |
| 戦士 | 戦巧斬 | 3 | 30 |
| 戦士 | 堅守 | 3 | 30 |
| 戦士 | 狙い澄まし | 3 | 30 |
| 戦士 | 応急処置 | 3 | 30 |
| 戦士 | 解毒 | 3 | 30 |
| 魔法使い | ファイヤー | 3 | 30 |
| 魔法使い | 炎槍・穿破 | 3 | 30 |
| 魔法使い | 氷獄・封圧 | 3 | 30 |
| 魔法使い | アーケインバースト | 3 | 30 |
| 魔法使い | ヒール | 3 | 30 |
| 魔法使い | マナチャージ | 3 | 30 |
| バーサーカー | 裂断 | 2 | 30 |
| バーサーカー | 血の代償 | 3 | 30 |
| バーサーカー | 不屈の雄叫び | 3 | 30 |
| バーサーカー | 狙い澄まし | 3 | 30 |
| バーサーカー | 堅守 | 3 | 30 |
| バーサーカー | 解毒 | 3 | 30 |

5解像度すべてで同じ結果。3職計18所有枠×5解像度=90組について、実際のTMP lineCount・fontSize・overflow・文字欄の範囲を検査した。記録は `C:/mygame1/Logs/SkillUiLarge/ui-metrics.tsv`。試験の繰り返し分も追記されるため、行数自体は90より多い。

## 4. Canvas・レイアウト・ページ送り

ルートCanvasの倍率・CanvasScaler・参照解像度は変更していない。既存パネルの全画面領域の中に、上110・下128・左右24を確保した技能用ScrollRectを作る。スクロールバーのために内側の右36も確保する。

- 広い画面は620幅×2列、列間24・行間20。1920×1080では1ページの4技能が同時に収まる。
- 狭い画面は620幅×1列。マウスホイール、ドラッグ、右のスクロールバーで上下へ移動する。
- スクロールは技能の領域に限定し、前へ/次へ、ページ番号、閉じるは固定する。
- 既存4枠と1ページ4技能、6技能で2ページという仕組みは維持する。ページを切り替えると、そのページの先頭へスクロールを戻す。
- 既存ボタンを再生時だけ新しいContentへ移す。元Prefabの保存階層やGUIDを変えず、既存OnClickとSkillSlotUIの参照を維持する。
- GridLayoutGroupとContentSizeFitterで内容の高さを計算し、ViewportのMaskで表示領域を限定する。スクロールして各ボタン全体と全文へ到達できることを確認する。
- 800×600でも技能ボタン全体を表示でき、ナビゲーションと重ならない。狭い画面で全4技能を一度に並べる仕様にはしていない。

スキルパネルを繰り返し開いてもScrollRectやボタンを重複生成しない。前へ/次への既存のリスナー登録・ページ計算は変更していない。

## 5. Unity CLIと自動試験

CLI: `C:/Users/shino/AppData/Local/Unity/bin/unity.exe`、1.0.0-beta.13。
Editor: `C:/Program Files/Unity/Hub/Editor/2023.2.22f1/Editor/Unity.exe`。
プロジェクトのProjectVersion.txtとPackagesを確認し、アップグレードやPipeline導入はしていない。

開始時、CLIの接続一覧は空だったが、実プロセスではmygame1のEditorが起動していた。AssetImportWorkerとEditorを区別してPID/対象パスを確認し、ユーザーが保存・終了したことを確認してから順番に実行した。二重起動や強制終了は行っていない。

主なコマンド（`$cli`と`$editor`は上記の絶対パス）:

```powershell
& $cli run C:\mygame1 --editor-path $editor --timeout 300 --log-file Logs/SkillUiLarge/before-ui.log --no-tail --format json -- -executeMethod SkillUiLargeAssets.RecordBefore -nographics
& $cli run C:\mygame1 --editor-path $editor --timeout 300 --log-file Logs/SkillUiLarge/apply-ui.log --no-tail --format json -- -executeMethod SkillUiLargeAssets.Apply -nographics
& $cli test C:\mygame1 --editor-path $editor --mode PlayMode --output Logs/SkillUiLarge/playmode-final.xml --timeout 600 --format json -- -logFile C:\mygame1\Logs\SkillUiLarge\playmode-final.log
& $cli test C:\mygame1 --editor-path $editor --mode EditMode --output Logs/SkillUiLarge/editmode.xml --timeout 300 --format json -- -nographics -logFile C:\mygame1\Logs\SkillUiLarge\editmode.log
```

最初に全職・全ページのレイアウト試験1件を実行し成功。その後の全体試験は9件中7件成功、試験用Canvasの扱いに起因する2件が失敗した。非表示Canvasも試験用カメラへ含めること、ScreenSpaceCameraでUnity自身が変換するCanvasの尺度ではなくCanvasScalerを検査することへ修正し、失敗2件だけの再実行は2件とも成功。ゲームのレイアウトや能力値を回避的に変更して通してはいない。

全体試験では、技能の実Buttonへのポインタークリック、拡大したクリック範囲、スクロールとバー、次へ/前へ、再表示、全職・全技能の実行、HP/MP/CDの一度だけの消費、既存VFX、成長進行、反射死と帰還も検査する。Unityコンパイル・EditMode/PlayModeの結果は各XMLとログで確認する。

最終結果はPlayMode **9/9成功**、EditMode **60/60成功**。失敗・スキップ0、最終コンパイルのC#エラー・警告0。結果は `C:/mygame1/Logs/SkillUiLarge/{playmode-final.xml,editmode.xml}`。途中の失敗ログ・XMLは最終結果とは区別して残している。

## 6. 確認画像

自動試験では実UnityのCamera/RenderTextureで5解像度を描画した。Root Canvasを試験中だけScreenSpaceCameraに切り替え、CanvasScalerは維持する。Sceneの描画モードの保存変更はしていない。Game Viewの通常操作とは区別する。

| 解像度 | 比率 | 配置・確認画像の例 |
| --- | --- | --- |
| 1920×1080 | 16:9、基準 | 2列：[戦士1頁](C:/mygame1/Logs/SkillUiLarge/ui-Warrior-1920x1080-page-1.png)、[魔法使い2頁](C:/mygame1/Logs/SkillUiLarge/ui-Mage-1920x1080-page-2.png) |
| 1280×720 | 16:9 | 1列スクロール：[戦士1頁・4枠目](C:/mygame1/Logs/SkillUiLarge/ui-Warrior-1280x720-page-1-card-4.png) |
| 960×600 | 16:10 | 1列スクロール：[魔法使い1頁・1枠目](C:/mygame1/Logs/SkillUiLarge/ui-Mage-960x600-page-1-card-1.png) |
| 800×600 | 4:3 | 1列スクロール：[血の代償](C:/mygame1/Logs/SkillUiLarge/ui-Berserker-800x600-page-1-card-2.png) |
| 900×1200 | 3:4、縦長 | 1列スクロール：[魔法使い2頁](C:/mygame1/Logs/SkillUiLarge/ui-Mage-900x1200-page-2.png) |

全職・全ページ・スクロール後の各枠を合計75画像に保存。ファイル名は `C:/mygame1/Logs/SkillUiLarge/ui-{Warrior/Mage/Berserker}-{解像度}-page-{頁}[-card-{枠}].png`。スクロールが不要な画面はページ全体1枚、必要な画面は各技能へ移動した画像を記録している。

### 通常のEditorのGame Viewによる実操作

自動試験終了後、同じ2023.2.22f1をCLIのopenで1つだけ起動し、Townから魔法使いを選択して戦闘へ入った。通常のGame Viewでスキル画面を開き、ホイール、次へ、前へ、拡大部分からのファイヤー選択を実際のマウス操作で確認した。技能選択だけではMPを消費せず、背後の敵への攻撃にもならなかった。Game Viewの解像度を元のFull HDへ戻したうえで再生を停止し、検証用Editorを通常終了した。

| 実Game Viewの画像 | 確認内容 |
| --- | --- |
| [1366×768・1頁上段](C:/mygame1/Logs/SkillUiLarge/gameview-Mage-1366x768-page-1-top.png) | 2列配置、固定ヘッダー・フッター |
| [1366×768・1頁下段](C:/mygame1/Logs/SkillUiLarge/gameview-Mage-1366x768-page-1-bottom.png) | ホイール後に下段の全文・MP/CDまで表示 |
| [1366×768・2頁](C:/mygame1/Logs/SkillUiLarge/gameview-Mage-1366x768-page-2.png) | 次へ/前へ、ヒール・マナチャージ |
| [ファイヤー選択直後](C:/mygame1/Logs/SkillUiLarge/gameview-Mage-selected-Fire.png) | 拡大したクリック範囲から選択、HP/MP維持 |
| [1920×1080・1頁](C:/mygame1/Logs/SkillUiLarge/gameview-Mage-1920x1080-page-1.png) | 4技能同時表示、スクロール不要 |

これら5枚はWindows上のEditorウィンドウをそのまま記録したもの。Game Viewのズーム・Windows表示倍率によって画像上の見かけの大きさは変わるが、ゲーム内TMPの30を縮める処理はない。戦士・バーサーカーの全ページは自動試験で確認しており、通常Game Viewでの同じ操作を両職について繰り返したとは報告しない。

## 7. 残る確認と操作方法

Inspectorでの追加設定は不要。Townから各職で出発し、スキル画面を開く。狭い画面ではホイール・ドラッグ・右のバーで技能をスクロールし、下の前へ/次へでページを切り替える。

今回の自動試験範囲は800×600以上の上記5サイズ。これより小さい表示で文字やボタンを縮める処理は追加せず、必要に応じてスクロールする。検証していない極小ウィンドウやOSの特殊な拡大率での使い心地まで確認済みとは扱わない。

UIの確認と、通常速度でのダンジョン全階層の手動通しプレイは別であり、後者は今回実施しない。各職の能力・所持構成、スキルの説明と数値、パッシブ、敵AI、部位破壊、カウンター、成長報酬は今回の変更対象外。

1366×768の実Game Viewでは、背後の既存の戦闘HUDで上端の敵HP表示や下のダイアログの一部が画面外に出る配置も確認した。スキル選択パネル内の表示・スクロール・ナビゲーションとは別の既存配置であり、ルートCanvasや戦闘UI全体を変更しないという今回の範囲に従い、修正していない。Full HDではその表示も画面内に収まる。

このUI改善の範囲で停止し、新しいスキルやゲーム機能は追加しない。
