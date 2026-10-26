using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static partial class ClassExpansionAssets
{
    public const string SkillFolder = "Assets/Skill Data/Phase3";
    public const string EnemyFolder = "Assets/Script/Enemy/Expansion";
    public const string ReportFolder = "Logs/ClassExpansion";

    public static void Audit()
    {
        Directory.CreateDirectory(ReportFolder);
        var report = new StringBuilder();
        report.AppendLine($"Unity {Application.unityVersion}; project {Application.dataPath}");
        foreach (string path in new[] { "Assets/Scenes/Town.unity", "Assets/Scenes/Quest.unity" })
        {
            Scene scene = EditorSceneManager.OpenScene(path);
            report.AppendLine($"SCENE {path} dirty={scene.isDirty}");
            foreach (GameObject root in scene.GetRootGameObjects()) Dump(root.transform, report, "");
            foreach (QuestManager quest in Find<QuestManager>())
            {
                var floors = new SerializedObject(quest).FindProperty("floors");
                for (int i = 0; i < floors.arraySize; i++)
                {
                    var floor = floors.GetArrayElementAtIndex(i);
                    report.AppendLine($"FLOOR {i + 1} boss={floor.FindPropertyRelative("isBossFloor").boolValue} candidates={floor.FindPropertyRelative("enemyDatas").arraySize}");
                }
            }
        }
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Script/Charcter", "Assets/Images/Charcter" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var references = AssetDatabase.GetAllAssetPaths().Where(p =>
                (p.EndsWith(".unity") || p.EndsWith(".prefab") || p.EndsWith(".asset")) &&
                AssetDatabase.GetDependencies(p, true).Contains(path));
            report.AppendLine($"IMAGE {path} GUID={guid} REFERENCES={string.Join(";", references)}");
        }
        File.WriteAllText(ReportFolder + "/unity-audit.txt", report.ToString());
        Debug.Log("CLASS_EXPANSION_AUDIT_OK");
    }

    private static void Dump(Transform target, StringBuilder report, string indent)
    {
        var rect = target as RectTransform;
        string components = string.Join(",", target.GetComponents<Component>().Select(c => c != null ? c.GetType().Name : "MISSING"));
        report.AppendLine($"{indent}{target.name} active={target.gameObject.activeSelf} {components} {(rect != null ? rect.rect.ToString() : target.localPosition.ToString())}");
        foreach (Transform child in target) Dump(child, report, indent + "  ");
    }

    public static void BuildClassesAndSkills()
    {
        Folder(SkillFolder);
        CreateSkills();
        EnsureExpansionFont();
        PlayerData warrior = Player("Warrior");
        PlayerData mage = Player("Mage");
        PlayerData berserker = Player("Berserker");
        warrior.startMaxHp = 1000; warrior.startMaxMp = 100; warrior.startAt = 55;
        warrior.startMag = 40; warrior.startDef = 4; warrior.startMdef = 8;
        warrior.uniquePassiveName = "戦技習得";
        warrior.uniquePassiveDescription = "新規の顔破壊でMPコスト20%軽減、手破壊で物理1.15倍、脚破壊で敵直接ダメージ0.85倍。各種1回、その戦闘中のみ。";
        berserker.startMaxHp = 1500; berserker.startMaxMp = 60; berserker.startAt = 95;
        berserker.startMag = 10; berserker.startDef = 0; berserker.startMdef = 0;
        warrior.portrait = Sprite("Assets/Script/Charcter/img112.png");
        mage.portrait = Sprite("Assets/Script/Charcter/img025.png");
        berserker.portrait = Sprite("Assets/Script/Charcter/img221.png");
        foreach (PlayerData data in new[] { warrior, mage, berserker })
        {
            if (data != mage) data.startSkills.Clear();
            foreach (SkillId id in Enum.GetValues(typeof(SkillId)))
            {
                SkillData skill = LoadSkill(id);
                if (skill != null && SkillUseRules.IsAllowedForRole(skill, data.role) && !data.startSkills.Contains(skill))
                    data.startSkills.Add(skill);
            }
            Save(data);
        }
        SetupTown(warrior, mage, berserker);
        SetupQuestUI();
        ValidateClassesAndSkills();
        Debug.Log("CLASS_EXPANSION_STAGE_1_2_OK");
    }

    private static void CreateSkills()
    {
        Skill(SkillId.Warrior_BreakSlash, "破砕斬", SkillType.Physical, 110, 8, 1, .9f, 1.6f, true,
            "物理P110。本体0.9倍、部位1.6倍。部位破壊を狙う。" );
        Skill(SkillId.Warrior_GuardSlash, "守りの一閃", SkillType.Physical, 95, 10, 2, 1, 1, true,
            "物理P95。命中にかかわらず次の敵行動終了まで直接ダメージ25%軽減。ガードは強い方のみ。");
        Skill(SkillId.Warrior_TacticalSlash, "戦巧斬", SkillType.Physical, 150, 14, 3, 1, 1.2f, true,
            "物理P150、部位1.2倍。戦技の種類ごとに直接ダメージ+10%、最大1.3倍。");
        Skill(SkillId.Mage_FlamePiercer, "炎槍・穿破", SkillType.Magic, 180, 24, 1, .8f, 2, false,
            "炎魔法P180。本体0.8倍、部位2倍。追加火傷なし。会心なし。", ElementType.Fire);
        SkillData frost = Skill(SkillId.Mage_FrostBind, "氷獄・封圧", SkillType.Magic, 160, 30, 3, .9f, 1.2f, false,
            "氷魔法P160。本体0.9倍、部位1.2倍。命中時に既存の凍結を100%で付与試行。会心なし。", ElementType.Ice);
        frost.statusEffect = AssetDatabase.LoadAssetAtPath<SkillData>("Assets/Skill Data/Skill_Ice.asset").statusEffect;
        frost.applyChance = 1f; Save(frost);
        Skill(SkillId.Mage_ArcaneBurst, "アーケインバースト", SkillType.Magic, 360, 45, 3, 1.2f, 1, false,
            "無属性魔法P360。本体1.2倍。MPを大きく使う撃破用魔法。会心なし。");
        Skill(SkillId.Berserker_Rend, "裂断", SkillType.Physical, 160, 4, 1, 1, 1.5f, true,
            "物理P160。部位1.5倍。");
        Skill(SkillId.Berserker_BloodPrice, "血の代償", SkillType.Physical, 230, 0, 3, 1.25f, 1, true,
            "最大HPの10%を自傷コストに物理P230、本体1.25倍。自傷死するHPでは使用不可。ミスでも自傷は戻らない。");
        Skill(SkillId.Berserker_WarCry, "不屈の雄叫び", SkillType.Buff, 0, 8, 3, 1, 1, false,
            "最大HP15%回復。次の物理攻撃2回を1.25倍。ミスでも回数消費。再使用は残り2回へ更新。");
        Skill(SkillId.Common_Guard, "堅守", SkillType.Buff, 0, 0, 1, 1, 1, false,
            "次の敵行動終了まで直接物理・魔法ダメージ50%軽減。毒・反射・自傷は軽減しない。");
        Skill(SkillId.Common_Aim, "狙い澄まし", SkillType.Buff, 0, 4, 2, 1, 1, false,
            "次の直接攻撃の命中+15ポイント、部位1.25倍。本体は増えない。ミスでも消費。次のプレイヤーターン終了まで。");
        Skill(SkillId.Common_Antidote, "解毒", SkillType.Buff, 0, 6, 4, 1, 1, false,
            "現在の毒のみ解除。毒でなければ消費なし。永続免疫なし。");
        Skill(SkillId.Common_FirstAid, "応急処置", SkillType.Heal, 0, 8, 3, 1, 1, false,
            "MAGに関係なく最大HPの12%回復。満タンでは使用不可。最低1、最大HPまで。");
        Skill(SkillId.Common_PartProbe, "牽制打ち", SkillType.Physical, 80, 3, 1, .7f, 1.3f, true,
            "物理P80。本体0.7倍、部位1.3倍。");
        Skill(SkillId.Auditor_ArcaneBolt, "魔弾", SkillType.Magic, 90, 5, 0, 1, 1, false, "単体魔法P90。");
        Skill(SkillId.Auditor_ManaLevy, "魔力徴収", SkillType.Magic, 70, 6, 3, 1, 1, false, "単体魔法P70。命中時に対象MPを最大10失わせる。吸収しない。顔破壊で封印。");
        Skill(SkillId.Auditor_ChargedLance, "禁呪光槍", SkillType.Magic, 180, 14, 3, 1, 1, false, "1敵行動の構え後に魔法P180。新規手破壊で中断、片手破壊で封印。");
        Skill(SkillId.Raider_CleavingBlow, "砕き割り", SkillType.Physical, 140, 4, 1, 1, 1, false, "単体物理P140。");
        Skill(SkillId.Raider_BloodFury, "血気", SkillType.Buff, 0, 0, 3, 1, 1, false, "最大HP8%自傷。次の物理攻撃2回1.25倍。顔破壊で再使用封印。");
        Skill(SkillId.Raider_ChargedAxe, "破山斧", SkillType.Physical, 220, 10, 4, 1, 1, false, "1敵行動の構え後に物理P220。新規手破壊で中断、両手破壊で封印。");
        Skill(SkillId.Pilgrim_TalismanBolt, "魔札投げ", SkillType.Magic, 110, 5, 1, 1, 1, false, "単体魔法P110。");
        Skill(SkillId.Pilgrim_TalismanWard, "護符障壁", SkillType.Buff, 0, 10, 3, 1, 1, false, "次のプレイヤーターン終了まで本体直接ダメージ30%軽減。部位・継続ダメージは対象外。片手破壊で再使用封印。");
        Skill(SkillId.Pilgrim_Renewal, "再生祈祷", SkillType.Heal, 0, 12, 3, 1, 1, false, "最大HP15%回復。HP60%以下で選択。顔破壊で封印。");
    }

    private static SkillData Skill(SkillId id, string name, SkillType type, int power, int mp, int cd,
        float main, float part, bool crit, string description, ElementType element = ElementType.None)
    {
        string path = SkillFolder + "/" + id + ".asset";
        var data = AssetDatabase.LoadAssetAtPath<SkillData>(path);
        if (data == null) { data = ScriptableObject.CreateInstance<SkillData>(); AssetDatabase.CreateAsset(data, path); }
        data.id = id; data.skillName = name; data.description = description; data.skillType = type;
        data.power = power; data.mpCost = mp; data.cooldown = cd; data.accuracy = 1f;
        data.mainDamageRate = main; data.partDamageRate = part; data.canCrit = crit; data.critBonus = 0;
        data.element = element; data.canTargetPart = type == SkillType.Physical || type == SkillType.Magic;
        data.targetType = data.canTargetPart ? TargetType.Single : TargetType.Self;
        data.statusEffect = null; data.buff = null; data.debuff = null; data.applyChance = 0;
        Save(data);
        return data;
    }

    private static void SetupTown(params PlayerData[] profiles)
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Town.unity");
        PlayerSelectButton[] buttons = profiles.Select(p => Find<PlayerSelectButton>().Single(b =>
            b.name.Trim().StartsWith(p.role.ToString(), StringComparison.Ordinal))).ToArray();
        if (buttons.Length != 3) throw new InvalidOperationException("Expected exactly three existing class buttons.");
        var transition = Find<SceneTransitionManager>().Single();
        foreach (PlayerSelectButton button in buttons)
            foreach (PlayerSelectionManager duplicate in button.GetComponents<PlayerSelectionManager>())
                UnityEngine.Object.DestroyImmediate(duplicate);
        Find<PlayerSelectionManager>().Single().selectedPlayer = Player("Mage");
        for (int i = 0; i < buttons.Length; i++)
        {
            PlayerSelectButton select = buttons[i];
            PlayerData data = profiles[i];
            select.playerData = data;
            select.gameObject.name = data.role + "Button";
            select.gameObject.SetActive(true);
            var rect = (RectTransform)select.transform;
            rect.anchorMin = rect.anchorMax = new Vector2((i + .5f) / 3f, .65f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(190, 240);
            var image = select.GetComponent<UnityEngine.UI.Image>();
            image.sprite = data.portrait; image.preserveAspect = true;
            var button = select.GetComponent<UnityEngine.UI.Button>();
            button.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
            UnityEventTools.AddPersistentListener(button.onClick, select.OnClick);
            UnityEventTools.AddStringPersistentListener(button.onClick, transition.LoadTo, "Quest");
            TMP_Text label = Text(rect, "ClassDetails", new Vector2(0, -175), new Vector2(205, 105));
            label.fontSize = 18;
            label.text = $"{data.playerName}\nHP {data.startMaxHp}  MP {data.startMaxMp}\nAT {data.startAt}  MAG {data.startMag}\nDEF {data.startDef}  MDEF {data.startMdef}";
            Transform backing = rect.Find("ClassDetailsBacking");
            if (backing == null) backing = new GameObject("ClassDetailsBacking", typeof(RectTransform), typeof(UnityEngine.UI.Image)).transform;
            backing.SetParent(rect, false); backing.SetAsFirstSibling();
            var backingRect = (RectTransform)backing;
            backingRect.anchorMin = backingRect.anchorMax = backingRect.pivot = new Vector2(.5f, .5f);
            backingRect.anchoredPosition = new Vector2(0, -175); backingRect.sizeDelta = new Vector2(205, 110);
            backing.GetComponent<UnityEngine.UI.Image>().color = new Color(.03f, .04f, .06f, .85f);
            backing.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
        }
        Transform sign = Find<Canvas>().Single(c => c.name == "Signboard Canvas").transform.GetChild(0);
        var signRect = (RectTransform)sign;
        signRect.localScale = Vector3.one; signRect.anchorMin = signRect.anchorMax = new Vector2(.5f, 1);
        signRect.pivot = new Vector2(.5f, 1); signRect.anchoredPosition = new Vector2(0, -15); signRect.sizeDelta = new Vector2(420, 60);
        var signText = sign.GetComponentInChildren<UnityEngine.UI.Text>();
        Stretch(signText.rectTransform, 5); signText.fontSize = 24; signText.resizeTextForBestFit = false;
        signText.alignment = TextAnchor.MiddleCenter; signText.text = "職業を選択してください";
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
    }

    private static void SetupQuestUI()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Quest.unity");
        var player = Find<PlayerManager>().Single();
        var battle = Find<BattleManager>().Single();
        var panel = (GameObject)new SerializedObject(battle).FindProperty("skillSelectionPanel").objectReferenceValue;
        var panelRect = (RectTransform)panel.transform;
        panel.GetComponent<Canvas>().sortingOrder = 60;
        var background = panel.GetComponent<UnityEngine.UI.Image>();
        if (background == null) background = panel.AddComponent<UnityEngine.UI.Image>();
        background.color = new Color(.04f, .07f, .12f, .96f);
        SkillSlotUI[] slots = panel.GetComponentsInChildren<SkillSlotUI>(true).OrderBy(s => s.name).ToArray();
        if (slots.Length != 4) throw new InvalidOperationException($"Expected four existing skill slots, got {slots.Length}.");
        for (int i = 0; i < slots.Length; i++)
        {
            var rect = (RectTransform)slots[i].transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2(i % 2 == 0 ? -210 : 210, i / 2 == 0 ? 135 : -80);
            rect.sizeDelta = new Vector2(400, 195);
            TMP_Text label = slots[i].GetComponentInChildren<TMP_Text>(true);
            Stretch((RectTransform)label.transform, 12);
            label.font = Font(); label.fontSharedMaterial = Font().material; label.fontSize = 21; label.enableAutoSizing = true;
            label.fontSizeMin = 16; label.fontSizeMax = 23; label.raycastTarget = false;
        }
        SkillPanelPager pager = panel.GetComponent<SkillPanelPager>();
        if (pager == null) pager = panel.AddComponent<SkillPanelPager>();
        var previous = Button(panelRect, "PreviousSkills", "前へ", new Vector2(-180, -255), new Vector2(150, 55));
        var next = Button(panelRect, "NextSkills", "次へ", new Vector2(180, -255), new Vector2(150, 55));
        TMP_Text page = Text(panelRect, "SkillPage", new Vector2(0, -255), new Vector2(180, 50));
        Set(pager, "slots", slots); Set(pager, "previousButton", previous); Set(pager, "nextButton", next);
        Set(pager, "pageLabel", page); Set(pager, "player", player); Set(player, "skillPager", pager);
        var close = Button(panelRect, "CloseSkills", "閉じる", new Vector2(0, 205), new Vector2(160, 50));
        close.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
        UnityEventTools.AddPersistentListener(close.onClick, battle.ToggleSkillPanel);
        var closeRect = (RectTransform)close.transform;
        closeRect.anchorMin = closeRect.anchorMax = new Vector2(.5f, 1);
        closeRect.pivot = new Vector2(.5f, 1); closeRect.anchoredPosition = new Vector2(0, -20);
        pager.RefreshLayout();
        var growth = Find<GrowthSelectionManager>().Single();
        foreach (UnityEngine.UI.Button button in growth.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            foreach (TMP_Text label in button.GetComponentsInChildren<TMP_Text>(true)) { label.font = Font(); label.fontSharedMaterial = Font().material; }
        var canvas = player.GetComponentInParent<Canvas>() ?? Find<Canvas>().First(c => c.name == "PlayerUICanvas");
        Transform portrait = canvas.transform.Find("ClassPortrait");
        if (portrait == null) portrait = new GameObject("ClassPortrait", typeof(RectTransform), typeof(UnityEngine.UI.Image)).transform;
        portrait.SetParent(canvas.transform, false);
        var portraitRect = (RectTransform)portrait;
        portraitRect.anchorMin = portraitRect.anchorMax = portraitRect.pivot = new Vector2(0, 0);
        portraitRect.anchoredPosition = new Vector2(15, 220); portraitRect.sizeDelta = new Vector2(140, 180);
        var portraitImage = portrait.GetComponent<UnityEngine.UI.Image>();
        portraitImage.raycastTarget = false; portraitImage.preserveAspect = true;
        Set(player, "portraitView", portraitImage);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
    }

    public static void ValidateClassesAndSkills()
    {
        SkillData[] skills = AssetDatabase.FindAssets("t:SkillData", new[] { SkillFolder })
            .Select(g => AssetDatabase.LoadAssetAtPath<SkillData>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
        if (skills.Length != 23 || skills.Select(s => s.id).Distinct().Count() != 23)
            throw new InvalidOperationException("Expected 23 unique expansion skills.");
        foreach (PlayerData data in new[] { Player("Warrior"), Player("Mage"), Player("Berserker") })
        {
            if (data.portrait == null || data.startSkills.Any(s => s == null || !SkillUseRules.IsAllowedForRole(s, data.role)))
                throw new InvalidOperationException("Invalid class asset: " + data.name);
            int newCount = data.startSkills.Count(s => s.id != SkillId.None);
            if (newCount != 8) throw new InvalidOperationException("Each class needs eight new skills.");
        }
        Directory.CreateDirectory(ReportFolder);
        File.WriteAllLines(ReportFolder + "/created-skills.tsv", skills.OrderBy(s => s.id).Select(s =>
            $"{s.id}\t{s.skillName}\t{AssetDatabase.GetAssetPath(s)}\tP={s.power}\tMP={s.mpCost}\tCD={s.cooldown}\tMAIN={s.mainDamageRate}\tPART={s.partDamageRate}\tCRIT={s.canCrit}"));
    }

    public static SkillData LoadSkill(SkillId id) => id == SkillId.None ? null :
        AssetDatabase.LoadAssetAtPath<SkillData>(SkillFolder + "/" + id + ".asset");
    private static PlayerData Player(string name) => AssetDatabase.LoadAssetAtPath<PlayerData>("Assets/Script/Player Data/" + name + ".asset");
    private static Sprite Sprite(string path) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().First();
    private static TMP_FontAsset Font() => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Skill Data/Phase3/Phase3UI.asset");
    private static T[] Find<T>() where T : UnityEngine.Object => UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);

    private static void Folder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = path.Substring(0, path.LastIndexOf('/'));
        Folder(parent); AssetDatabase.CreateFolder(parent, path.Substring(path.LastIndexOf('/') + 1));
    }

    private static void Save(UnityEngine.Object asset) { EditorUtility.SetDirty(asset); AssetDatabase.SaveAssetIfDirty(asset); }

    private static void Set(UnityEngine.Object owner, string field, UnityEngine.Object value)
    {
        var serialized = new SerializedObject(owner);
        serialized.FindProperty(field).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Set(UnityEngine.Object owner, string field, UnityEngine.Object[] values)
    {
        var serialized = new SerializedObject(owner);
        var array = serialized.FindProperty(field); array.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static TMP_Text Text(Transform parent, string name, Vector2 position, Vector2 size)
    {
        Transform existing = parent.Find(name);
        var go = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = position; rect.sizeDelta = size;
        var text = go.GetComponent<TMP_Text>(); text.font = Font(); text.fontSize = 24;
        text.color = Color.white; text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
        return text;
    }

    private static UnityEngine.UI.Button Button(Transform parent, string name, string label, Vector2 position, Vector2 size)
    {
        Transform existing = parent.Find(name);
        var go = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = position; rect.sizeDelta = size;
        go.GetComponent<UnityEngine.UI.Image>().color = new Color(.12f, .18f, .27f, 1);
        TMP_Text text = Text(rect, "Label", Vector2.zero, size); text.text = label;
        return go.GetComponent<UnityEngine.UI.Button>();
    }

    private static void Stretch(RectTransform rect, float margin)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(margin, margin); rect.offsetMax = new Vector2(-margin, -margin);
    }
}
