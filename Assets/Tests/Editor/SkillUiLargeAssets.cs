using System;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SkillUiLargeAssets
{
    public const string Report = "Logs/SkillUiLarge";

    public static void Apply()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Quest.unity");
        var pager = UnityEngine.Object.FindObjectsByType<SkillPanelPager>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single();
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Skill Data/Presentation/SkillMenuUI.asset");
        foreach (var slot in pager.GetComponentsInChildren<SkillSlotUI>(true))
        {
            ((RectTransform)slot.transform).sizeDelta = new Vector2(620, 300);
            ConfigureLabel(slot.Label, font);
            EditorUtility.SetDirty(slot.transform); PrefabUtility.RecordPrefabInstancePropertyModifications(slot.transform);
        }
        foreach (string name in new[] { "PreviousSkills", "NextSkills", "SkillPage", "CloseSkills" })
        {
            Transform control = pager.transform.Find(name);
            foreach (TMP_Text label in control.GetComponentsInChildren<TMP_Text>(true)) ConfigureLabel(label, font);
        }
        pager.RefreshLayout();
        EditorSceneManager.MarkSceneDirty(pager.gameObject.scene); EditorSceneManager.SaveScene(pager.gameObject.scene);
        Debug.Log("SKILL_UI_LARGE_SCENE_UPDATED");
    }

    private static void ConfigureLabel(TMP_Text label, TMP_FontAsset font)
    {
        label.font = font; label.fontSharedMaterial = font.material;
        label.enableAutoSizing = false; label.fontSize = 30; label.fontSizeMin = 30; label.fontSizeMax = 30;
        EditorUtility.SetDirty(label); PrefabUtility.RecordPrefabInstancePropertyModifications(label);
    }

    public static void RecordBefore()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Quest.unity");
        var pager = UnityEngine.Object.FindObjectsByType<SkillPanelPager>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single();
        var serialized = new SerializedObject(pager);
        var report = new StringBuilder();
        foreach (string field in new[] { "previousButton", "nextButton", "pageLabel" })
        {
            var component = (Component)serialized.FindProperty(field).objectReferenceValue;
            report.AppendLine(field + "\tsize=" + ((RectTransform)component.transform).rect.size);
        }
        var panel = (RectTransform)pager.transform;
        report.AppendLine("Panel anchors=" + panel.anchorMin + ".." + panel.anchorMax + " scale=" + panel.localScale);
        foreach (Canvas canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
            if (scaler != null) report.AppendLine($"Canvas {canvas.name} mode={canvas.renderMode} scaler={scaler.uiScaleMode} factor={scaler.scaleFactor} reference={scaler.referenceResolution}");
        }
        foreach (var size in new[] { new Vector2(1920, 1080), new Vector2(1280, 720), new Vector2(960, 600), new Vector2(800, 600), new Vector2(900, 1200) })
        {
            float width = Mathf.Min(400f, Mathf.Max(140f, (size.x - 70f) * .5f));
            float height = Mathf.Min(195f, Mathf.Max(95f, (size.y - 170f) * .5f));
            report.AppendLine($"Before runtime {size}: slot={width}x{height}, from current SkillPanelPager.RefreshLayout");
        }
        var slots = pager.GetComponentsInChildren<SkillSlotUI>(true);
        foreach (SkillSlotUI slot in slots)
            report.AppendLine($"Serialized {slot.name}: size={((RectTransform)slot.transform).rect.size} font={slot.Label.fontSize} min={slot.Label.fontSizeMin} auto={slot.Label.enableAutoSizing}");
        TMP_Text label = slots[0].Label; label.fontSize = 30; label.enableAutoSizing = false;
        foreach (SkillData skill in Enum.GetValues(typeof(PlayerRole)).Cast<PlayerRole>().SelectMany(SkillPresentationAssets.Loadout).Distinct())
            report.AppendLine($"Text30 {skill.name}: unwrappedWidth={label.GetPreferredValues(skill.skillName + "\n" + skill.description, float.PositiveInfinity, float.PositiveInfinity).x}");
        Directory.CreateDirectory(Report); File.WriteAllText(Report + "/before-ui.txt", report.ToString());
        Debug.Log("SKILL_UI_LARGE_BASELINE_RECORDED");
    }
}
