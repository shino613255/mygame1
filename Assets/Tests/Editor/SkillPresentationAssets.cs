using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

public static partial class SkillPresentationAssets
{
    public const string Folder = "Assets/Skill Data/Presentation";
    public const string Report = "Logs/SkillPresentation";
    public static SkillData Skill(SkillId id) => ClassExpansionAssets.LoadSkill(id);
    public static SkillData Old(string name) => AssetDatabase.LoadAssetAtPath<SkillData>("Assets/Skill Data/Skill_" + name + ".asset");

    public static SkillData[] Loadout(PlayerRole role)
    {
        if (role == PlayerRole.Warrior) return new[] { Skill(SkillId.Warrior_BreakSlash), Skill(SkillId.Warrior_TacticalSlash),
            Skill(SkillId.Common_Guard), Skill(SkillId.Common_Aim), Skill(SkillId.Common_FirstAid), Skill(SkillId.Common_Antidote) };
        if (role == PlayerRole.Berserker) return new[] { Skill(SkillId.Berserker_Rend), Skill(SkillId.Berserker_BloodPrice),
            Skill(SkillId.Berserker_WarCry), Skill(SkillId.Common_Aim), Skill(SkillId.Common_Guard), Skill(SkillId.Common_Antidote) };
        return new[] { Old("fire"), Skill(SkillId.Mage_FlamePiercer), Skill(SkillId.Mage_FrostBind),
            Skill(SkillId.Mage_ArcaneBurst), Old("Heal"), Old("MpHeal") };
    }

    public static void ConfigureProfiles()
    {
        foreach (PlayerRole role in Enum.GetValues(typeof(PlayerRole)))
        {
            var data = AssetDatabase.LoadAssetAtPath<PlayerData>("Assets/Script/Player Data/" + role + ".asset");
            if (data == null) continue;
            data.startSkills.Clear(); data.startSkills.AddRange(Loadout(role)); Save(data);
        }
    }

    public static void PrepareUi()
    {
        Directory.CreateDirectory(Report);
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Skill Data", "Presentation");
        var descriptions = new Dictionary<SkillData, string>
        {
            [Skill(SkillId.Warrior_BreakSlash)] = "物理P110/本体0.9倍・部位1.6倍",
            [Skill(SkillId.Warrior_TacticalSlash)] = "物理P150/部位1.2倍\n戦技数で直接ダメージ1～1.3倍",
            [Skill(SkillId.Common_Guard)] = "次の敵行動終了まで直接ダメージ半減\n毒・反射・自傷は軽減しない",
            [Skill(SkillId.Common_Aim)] = "次撃の命中+15pt/部位1.25倍\nミス消費・次の自ターン終了まで",
            [Skill(SkillId.Common_FirstAid)] = "最大HPの12%回復（上限あり）\nHP満タンでは使用できない",
            [Skill(SkillId.Common_Antidote)] = "毒を解除（HP回復なし）\n毒でない時は使用できない",
            [Skill(SkillId.Berserker_Rend)] = "物理P160/部位1.5倍",
            [Skill(SkillId.Berserker_BloodPrice)] = "物理P230/本体1.25倍\n最大HP10%消費/自傷HP0不可・ミス消費",
            [Skill(SkillId.Berserker_WarCry)] = "最大HP15%回復\n次の物理2回を1.25倍・ミス消費",
            [Skill(SkillId.Mage_FlamePiercer)] = "炎魔法P180/本体0.8倍・部位2倍\n部位破壊用・追加火傷なし",
            [Skill(SkillId.Mage_FrostBind)] = "氷魔法P160/本体0.9倍・部位1.2倍\n命中で凍結3T試行/AT・MAG半減",
            [Skill(SkillId.Mage_ArcaneBurst)] = "魔法P360/本体1.2倍・部位1倍\nMPを多く使う本体撃破用",
            [Old("fire")] = "炎魔法P150/本体・部位攻撃\n命中時10%で火傷を付与試行",
            [Old("Heal")] = "自分のHPを80+MAG×0.5回復\n最大HPを超えない",
            [Old("MpHeal")] = "自分のMPを30回復\n最大MPを超えない"
        };
        var text = new StringBuilder();
        foreach (var pair in descriptions)
        {
            string baseline = "Logs/SkillPresentation/baseline/" + AssetDatabase.GetAssetPath(pair.Key);
            string before = pair.Key.description;
            if (File.Exists(baseline))
            {
                string value = File.ReadAllLines(baseline).First(l => l.StartsWith("  description:")).Substring("  description:".Length).Trim();
                before = string.IsNullOrEmpty(value) ? "" :
                    value.StartsWith("\"") ? JsonUtility.FromJson<DescriptionSnapshot>("{\"description\":" + value + "}").description : value;
            }
            text.AppendLine(AssetDatabase.GetAssetPath(pair.Key)); text.AppendLine("BEFORE " + before);
            text.AppendLine("AFTER " + pair.Value); pair.Key.description = pair.Value; Save(pair.Key);
        }
        string changes = Report + "/description-changes.txt";
        File.WriteAllText(changes, text.ToString());
        ConfigureProfiles();
        TMP_FontAsset font = MenuFont();
        EditorSceneManager.OpenScene("Assets/Scenes/Quest.unity");
        ApplyFont(UnityEngine.Object.FindObjectsByType<SkillSlotUI>(FindObjectsInactive.Include, FindObjectsSortMode.None), font);
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        string path = "Assets/Scenes/Prefabs/SkillSelectionPanel.prefab";
        GameObject prefab = PrefabUtility.LoadPrefabContents(path);
        var prefabSlots = prefab.GetComponentsInChildren<SkillSlotUI>(true);
        if (prefabSlots.Length > 0)
        {
            ApplyFont(prefabSlots, font); PrefabUtility.SaveAsPrefabAsset(prefab, path);
        }
        PrefabUtility.UnloadPrefabContents(prefab);
        Debug.Log("SKILL_PRESENTATION_UI_ASSETS_OK");
    }

    private static void ApplyFont(IEnumerable<SkillSlotUI> slots, TMP_FontAsset font)
    {
        foreach (SkillSlotUI slot in slots)
        {
            var serialized = new SerializedObject(slot);
            var label = (TMP_Text)serialized.FindProperty("skillNameText").objectReferenceValue;
            if (label == null) throw new InvalidOperationException("Missing TMP skill label: " + slot.name);
            label.font = font; label.fontSharedMaterial = font.material;
            label.fontSize = 20; label.fontSizeMin = 16; label.enableAutoSizing = false;
            EditorUtility.SetDirty(label); PrefabUtility.RecordPrefabInstancePropertyModifications(label);
        }
    }

    private static TMP_FontAsset MenuFont()
    {
        string path = Folder + "/SkillMenuUI.asset";
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if (font != null) return font;
        var source = AssetDatabase.LoadAssetAtPath<Font>("Assets/Effect Assets/NotoSansJP-VariableFont_wght.ttf");
        font = TMP_FontAsset.CreateFontAsset(source, 48, 5, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, false);
        font.name = "SkillMenuUI"; AssetDatabase.CreateAsset(font, path);
        foreach (Texture2D texture in font.atlasTextures) AssetDatabase.AddObjectToAsset(texture, font);
        AssetDatabase.AddObjectToAsset(font.material, font);
        string text = string.Concat(Enum.GetValues(typeof(PlayerRole)).Cast<PlayerRole>().SelectMany(Loadout)
            .Distinct().Select(s => s.skillName + s.description)) + "MP CD /前へ次へ閉じるスキル" +
            new string(Enumerable.Range(32, 95).Select(c => (char)c).ToArray());
        font.TryAddCharacters(text, out string missing);
        if (!string.IsNullOrEmpty(missing)) throw new InvalidOperationException("Missing font glyphs: " + missing);
        font.atlasPopulationMode = AtlasPopulationMode.Static; Save(font); return font;
    }

    private static void Save(UnityEngine.Object asset) { EditorUtility.SetDirty(asset); AssetDatabase.SaveAssetIfDirty(asset); }
    [Serializable] private class DescriptionSnapshot { public string description; }
}
