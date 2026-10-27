using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CartoonFX;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static partial class SkillPresentationAssets
{
    public static readonly Dictionary<string, string> VfxSources = new Dictionary<string, string>
    {
        ["Physical"] = "Assets/Effect Assets/motion/CFXR Prefabs/Sword Trails/Plain/CFXR4 Sword Hit PLAIN (Cross).prefab",
        ["Arcane"] = "Assets/Effect Assets/motion/CFXR Prefabs/Impacts/CFXR Impact Glowing HDR (Blue).prefab",
        ["Guard"] = "Assets/Effect Assets/motion/CFXR Prefabs/Magic Misc/CFXR4 Bouncing Glows Bubble (Blue Purple).prefab",
        ["Aim"] = "Assets/Effect Assets/motion/CFXR Prefabs/Light/CFXR3 LightGlow A (Loop).prefab",
        ["Dispel"] = "Assets/Effect Assets/motion/CFXR Prefabs/Misc/CFXR Magic Poof.prefab",
        ["Fire"] = "Assets/Skill Data/Skill Effect/Fire.prefab",
        ["Ice"] = "Assets/Skill Data/Skill Effect/Ice.prefab",
        ["Heal"] = "Assets/Skill Data/Skill Effect/Heal.prefab",
        ["MpHeal"] = "Assets/Skill Data/Skill Effect/Mp Heal.prefab"
    };

    public static void AuditVfx()
    {
        Directory.CreateDirectory(Report);
        var report = new StringBuilder();
        foreach (var pair in VfxSources)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(pair.Value);
            if (prefab == null) throw new InvalidOperationException("Missing VFX: " + pair.Value);
            report.AppendLine(pair.Key + "\t" + pair.Value + "\tGUID=" + AssetDatabase.AssetPathToGUID(pair.Value));
            foreach (ParticleSystem ps in prefab.GetComponentsInChildren<ParticleSystem>(true))
                report.AppendLine($"  PARTICLES {ps.name} loop={ps.main.loop} duration={ps.main.duration} lifetime={ps.main.startLifetime.constantMax}");
            foreach (Renderer renderer in prefab.GetComponentsInChildren<Renderer>(true))
            foreach (Material material in renderer.sharedMaterials)
            {
                if (material == null || material.shader == null)
                {
                    var psRenderer = renderer as ParticleSystemRenderer;
                    if (psRenderer != null && !psRenderer.GetComponent<ParticleSystem>().trails.enabled)
                        report.AppendLine("  OPTIONAL EMPTY TRAIL MATERIAL " + renderer.name);
                    else report.AppendLine("  MISSING REQUIRED MATERIAL " + renderer.name);
                    continue;
                }
                report.AppendLine("  SHADER " + AssetDatabase.GetAssetPath(material) + " " + material.shader.name + " supported=" + material.shader.isSupported);
            }
            RenderVfx(prefab, "source-" + pair.Key);
        }
        File.WriteAllText(Report + "/vfx-source-audit.txt", report.ToString());
        Debug.Log("SKILL_PRESENTATION_VFX_SOURCE_AUDIT_OK");
    }

    private static void RenderVfx(GameObject prefab, string name)
    {
        Scene scene = EditorSceneManager.NewPreviewScene();
        GameObject root = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        var cameraObject = new GameObject("PreviewCamera"); SceneManager.MoveGameObjectToScene(cameraObject, scene);
        var camera = cameraObject.AddComponent<Camera>(); camera.scene = scene;
        camera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(scene);
        camera.transform.position = new Vector3(0, 0, -10); camera.orthographic = true; camera.orthographicSize = 3.5f;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.04f, .05f, .08f);
        var target = new RenderTexture(400, 400, 24); camera.targetTexture = target;
        var sheet = new Texture2D(1200, 400, TextureFormat.RGB24, false);
        RenderTexture previous = RenderTexture.active;
        try
        {
            for (int i = 0; i < 3; i++)
            {
                foreach (ParticleSystem ps in root.GetComponentsInChildren<ParticleSystem>(true))
                    ps.Simulate(new[] { .12f, .3f, .6f }[i], false, true, true);
                camera.Render(); RenderTexture.active = target;
                sheet.ReadPixels(new Rect(0, 0, 400, 400), i * 400, 0);
            }
            sheet.Apply(); File.WriteAllBytes(Report + "/" + name + ".png", sheet.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous; camera.targetTexture = null; target.Release();
            UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(sheet);
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    public static void AssignVfx()
    {
        var report = new StringBuilder();
        var physical = Wrapper("Physical", 1f); var arcane = Wrapper("Arcane", .65f);
        var guard = Wrapper("Guard", .55f); var aim = Wrapper("Aim", 1f); var dispel = Wrapper("Dispel", .6f);
        var fire = Wrapper("Fire", .75f); var ice = Wrapper("Ice", .75f);
        var map = new Dictionary<SkillId, GameObject>
        {
            [SkillId.Warrior_BreakSlash] = physical, [SkillId.Warrior_TacticalSlash] = physical,
            [SkillId.Berserker_Rend] = physical, [SkillId.Berserker_BloodPrice] = physical,
            [SkillId.Common_Guard] = guard, [SkillId.Common_Aim] = aim, [SkillId.Common_Antidote] = dispel,
            [SkillId.Common_FirstAid] = AssetDatabase.LoadAssetAtPath<GameObject>(VfxSources["Heal"]),
            [SkillId.Berserker_WarCry] = AssetDatabase.LoadAssetAtPath<GameObject>(VfxSources["Heal"]),
            [SkillId.Mage_FlamePiercer] = fire, [SkillId.Mage_FrostBind] = ice, [SkillId.Mage_ArcaneBurst] = arcane
        };
        foreach (var pair in map)
        {
            SkillData skill = Skill(pair.Key); skill.effectPrefab = pair.Value;
            skill.effectDuration = pair.Key == SkillId.Common_Aim ? .8f : 1f;
            skill.effectOffset = Vector2.zero; Save(skill);
        }
        foreach (SkillData skill in Enum.GetValues(typeof(PlayerRole)).Cast<PlayerRole>().SelectMany(Loadout).Distinct())
            report.AppendLine($"{skill.skillName}\t{AssetDatabase.GetAssetPath(skill)}\t{AssetDatabase.GetAssetPath(skill.effectPrefab)}\tduration={skill.effectDuration}\toffset={skill.effectOffset}");
        File.WriteAllText(Report + "/vfx-assignments.tsv", report.ToString());
        Debug.Log("SKILL_PRESENTATION_VFX_ASSIGNMENTS_OK");
    }

    public static void FinalizeVfx() { AuditVfx(); AssignVfx(); }

    private static GameObject Wrapper(string key, float scale)
    {
        string path = Folder + "/" + key + "Vfx.prefab";
        bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
        GameObject root = exists ? PrefabUtility.LoadPrefabContents(path) : new GameObject(key + "Vfx");
        if (root.transform.childCount == 0)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(VfxSources[key]);
            GameObject child = (GameObject)PrefabUtility.InstantiatePrefab(source);
            child.transform.SetParent(root.transform, false);
        }
        root.transform.localScale = Vector3.one * scale;
        foreach (CFXR_Effect effect in root.GetComponentsInChildren<CFXR_Effect>(true))
        {
            if (effect.cameraShake != null) effect.cameraShake.enabled = false;
            effect.clearBehavior = CFXR_Effect.ClearBehavior.None;
            PrefabUtility.RecordPrefabInstancePropertyModifications(effect);
        }
        foreach (Light light in root.GetComponentsInChildren<Light>(true))
        {
            light.enabled = false; PrefabUtility.RecordPrefabInstancePropertyModifications(light);
        }
        foreach (ParticleSystem ps in root.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = ps.main; main.loop = false;
            PrefabUtility.RecordPrefabInstancePropertyModifications(ps);
        }
        PrefabUtility.SaveAsPrefabAsset(root, path);
        if (exists) PrefabUtility.UnloadPrefabContents(root); else UnityEngine.Object.DestroyImmediate(root);
        var result = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        RenderVfx(result, "assigned-" + key); return result;
    }
}
