using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

public static partial class ClassExpansionAssets
{
    private static void EnsureExpansionFont()
    {
        string path = SkillFolder + "/Phase3UI.asset";
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if (font == null || font.atlasTextures == null || font.atlasTextures.Length == 0 || font.atlasTextures[0] == null)
        {
            var source = AssetDatabase.LoadAssetAtPath<Font>("Assets/Effect Assets/NotoSansJP-VariableFont_wght.ttf");
            var created = TMP_FontAsset.CreateFontAsset(source, 48, 5, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, false);
            if (font == null)
            {
                font = created; font.name = "Phase3UI"; AssetDatabase.CreateAsset(font, path);
            }
            else
            {
                EditorUtility.CopySerialized(created, font);
                font.name = "Phase3UI";
            }
            foreach (Texture2D texture in font.atlasTextures) AssetDatabase.AddObjectToAsset(texture, font);
            AssetDatabase.AddObjectToAsset(font.material, font);
            if (created != font)
            {
                created.atlasTextures = Array.Empty<Texture2D>(); created.material = null;
                UnityEngine.Object.DestroyImmediate(created);
            }
        }
        string text = string.Concat(AssetDatabase.FindAssets("t:SkillData").Select(g =>
        {
            var skill = AssetDatabase.LoadAssetAtPath<SkillData>(AssetDatabase.GUIDToAssetPath(g));
            return skill.skillName + skill.description;
        })) + "最大HP上昇魔法攻撃上昇防御上昇攻撃力上昇最大MP上昇生命吸収集中攻撃精密射撃戦士魔法使いバーサーカー前へ次へ閉じるスキル魔導監察官血斧の略奪者護符の巡礼者：敵切替部位枠線部位をクリックして判定を確認破壊可不可" +
            new string(Enumerable.Range(32, 95).Select(i => (char)i).ToArray());
        uint[] characters = text.Where(c => !char.IsControl(c)).Select(c => (uint)c).Distinct().ToArray();
        if (characters.All(c => font.HasCharacter((int)c)))
        {
            font.atlasPopulationMode = AtlasPopulationMode.Static;
            Save(font);
            Debug.Log("CLASS_EXPANSION_FONT_GLYPHS_OK " + characters.Length);
            return;
        }
        font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        var serializedFont = new SerializedObject(font);
        serializedFont.FindProperty("m_SourceFontFile").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<Font>("Assets/Effect Assets/NotoSansJP-VariableFont_wght.ttf");
        serializedFont.ApplyModifiedPropertiesWithoutUndo();
        font.TryAddCharacters(characters, out uint[] missing);
        if (missing != null && missing.Length > 0) throw new InvalidOperationException("Phase3 font is missing: " + string.Join(",", missing));
        font.atlasPopulationMode = AtlasPopulationMode.Static;
        foreach (Texture2D texture in font.atlasTextures)
        {
            if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(texture)))
            {
                texture.name = "Phase3UI Atlas"; AssetDatabase.AddObjectToAsset(texture, font);
            }
        }
        if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(font.material)))
        {
            font.material.name = "Phase3UI Material"; AssetDatabase.AddObjectToAsset(font.material, font);
        }
        Save(font);
        Debug.Log("CLASS_EXPANSION_FONT_GLYPHS_OK " + characters.Length);
    }

    public static void FixExpansionUI()
    {
        EnsureExpansionFont();
        SetupTown(Player("Warrior"), Player("Mage"), Player("Berserker"));
        SetupQuestUI();
        CreateEnemyValidationScene();
        var canvas = Find<Canvas>().Single(); canvas.sortingOrder = 60;
        var label = Find<TMP_Text>().Single();
        var rect = (RectTransform)label.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1); rect.pivot = new Vector2(.5f, 1);
        rect.anchoredPosition = new Vector2(0, -10); rect.sizeDelta = new Vector2(620, 100);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("CLASS_EXPANSION_UI_GLYPHS_OK");
    }
}
