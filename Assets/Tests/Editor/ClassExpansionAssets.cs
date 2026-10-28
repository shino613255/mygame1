
using System;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static partial class ClassExpansionAssets
{
    public const string SkillFolder = "Assets/Skill Data/Phase3";
    public const string EnemyFolder = "Assets/Script/Enemy/Expansion";
    public const string ReportFolder = "Logs/ClassExpansion";

    private const int RequiredExpansionSkillCount = 23;
    private const int MaxClassSkillCount = 6;

    private static readonly string[] AuditScenePaths =
    {
        "Assets/Scenes/Town.unity",
        "Assets/Scenes/Quest.unity"
    };

    private static readonly string[] CharacterFolders =
    {
        "Assets/Script/Charcter",
        "Assets/Images/Charcter"
    };

    private static readonly string[] PlayerNames =
    {
        "Warrior",
        "Mage",
        "Berserker"
    };

    public static void Audit()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Play Mode中は監査を実行できません。");
            return;
        }

        SceneSetup[] sceneSetup = EditorSceneManager.GetSceneManagerSetup();

        foreach (Scene scene in Enumerable.Range(0, SceneManager.sceneCount)
                     .Select(SceneManager.GetSceneAt))
        {
            if (scene.isDirty || string.IsNullOrEmpty(scene.path))
            {
                Debug.LogWarning(
                    "未保存のSceneがあります。保存してから監査してください。"
                );

                return;
            }
        }

        Directory.CreateDirectory(ReportFolder);

        var report = new StringBuilder();

        report.AppendLine(
            $"Unity {Application.unityVersion}; project {Application.dataPath}"
        );

        try
        {
            foreach (string path in AuditScenePaths)
            {
                Scene scene = EditorSceneManager.OpenScene(
                    path,
                    OpenSceneMode.Single
                );

                report.AppendLine(
                    $"SCENE {path} dirty={scene.isDirty}"
                );

                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    Dump(root.transform, report, "");
                }

                foreach (QuestManager quest in Find<QuestManager>())
                {
                    var serialized = new SerializedObject(quest);
                    var floors = serialized.FindProperty("floors");

                    if (floors == null)
                    {
                        report.AppendLine("FLOOR DATA MISSING");
                        continue;
                    }

                    for (int i = 0; i < floors.arraySize; i++)
                    {
                        var floor = floors.GetArrayElementAtIndex(i);

                        bool isBossFloor = floor
                            .FindPropertyRelative("isBossFloor")
                            .boolValue;

                        int candidates = floor
                            .FindPropertyRelative("enemyDatas")
                            .arraySize;

                        report.AppendLine(
                            $"FLOOR {i + 1} " +
                            $"boss={isBossFloor} " +
                            $"candidates={candidates}"
                        );
                    }
                }
            }

            string[] allPaths = AssetDatabase.GetAllAssetPaths();

            foreach (string guid in AssetDatabase.FindAssets(
                         "t:Texture2D",
                         CharacterFolders))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                var references = allPaths.Where(assetPath =>
                    (assetPath.EndsWith(".unity") ||
                     assetPath.EndsWith(".prefab") ||
                     assetPath.EndsWith(".asset")) &&
                    AssetDatabase.GetDependencies(assetPath, true)
                        .Contains(path)
                );

                report.AppendLine(
                    $"IMAGE {path} " +
                    $"GUID={guid} " +
                    $"REFERENCES={string.Join(";", references)}"
                );
            }

            File.WriteAllText(
                Path.Combine(ReportFolder, "unity-audit.txt"),
                report.ToString()
            );

            Debug.Log("CLASS_EXPANSION_AUDIT_OK");
        }
        finally
        {
            EditorSceneManager.RestoreSceneManagerSetup(sceneSetup);
        }
    }

    private static void Dump(
        Transform target,
        StringBuilder report,
        string indent)
    {
        var rect = target as RectTransform;

        string components = string.Join(
            ",",
            target.GetComponents<Component>()
                .Select(component =>
                    component != null
                        ? component.GetType().Name
                        : "MISSING")
        );

        string position = rect != null
            ? rect.rect.ToString()
            : target.localPosition.ToString();

        report.AppendLine(
            $"{indent}{target.name} " +
            $"active={target.gameObject.activeSelf} " +
            $"{components} {position}"
        );

        foreach (Transform child in target)
        {
            Dump(child, report, indent + "  ");
        }
    }

    public static void ValidateClassesAndSkills()
    {
        SkillData[] skills = AssetDatabase.FindAssets(
                "t:SkillData",
                new[] { SkillFolder })
            .Select(guid =>
                AssetDatabase.LoadAssetAtPath<SkillData>(
                    AssetDatabase.GUIDToAssetPath(guid)))
            .ToArray();

        if (skills.Any(skill => skill == null))
        {
            throw new InvalidOperationException(
                "Missing expansion SkillData asset."
            );
        }

        if (skills.Length != RequiredExpansionSkillCount ||
            skills.Select(skill => skill.id).Distinct().Count() !=
            RequiredExpansionSkillCount)
        {
            throw new InvalidOperationException(
                $"Expected {RequiredExpansionSkillCount} unique expansion skills."
            );
        }

        foreach (string playerName in PlayerNames)
        {
            PlayerData data = Player(playerName);

            if (data == null)
            {
                throw new InvalidOperationException(
                    $"PlayerData not found: {playerName}"
                );
            }

            if (data.portrait == null || data.startSkills == null)
            {
                throw new InvalidOperationException(
                    "Invalid class asset: " + data.name
                );
            }

            bool hasInvalidSkill = data.startSkills.Any(skill =>
                skill == null ||
                !SkillUseRules.IsAllowedForRole(skill, data.role)
            );

            if (hasInvalidSkill)
            {
                throw new InvalidOperationException(
                    "Invalid class skill: " + data.name
                );
            }

            if (data.startSkills.Count > MaxClassSkillCount ||
                !data.startSkills.SequenceEqual(
                    SkillPresentationAssets.Loadout(data.role)))
            {
                throw new InvalidOperationException(
                    $"Class loadout must match the current " +
                    $"{MaxClassSkillCount}-skill selection: {data.name}"
                );
            }
        }

        Directory.CreateDirectory(ReportFolder);

        File.WriteAllLines(
            Path.Combine(ReportFolder, "created-skills.tsv"),
            skills.OrderBy(skill => skill.id)
                .Select(skill =>
                    $"{skill.id}\t" +
                    $"{skill.skillName}\t" +
                    $"{AssetDatabase.GetAssetPath(skill)}\t" +
                    $"P={skill.power}\t" +
                    $"MP={skill.mpCost}\t" +
                    $"CD={skill.cooldown}\t" +
                    $"MAIN={skill.mainDamageRate}\t" +
                    $"PART={skill.partDamageRate}\t" +
                    $"CRIT={skill.canCrit}")
        );

        Debug.Log("CLASS_EXPANSION_VALIDATION_OK");
    }

    public static SkillData LoadSkill(SkillId id)
    {
        if (id == SkillId.None)
        {
            return null;
        }

        string path = $"{SkillFolder}/{id}.asset";

        return AssetDatabase.LoadAssetAtPath<SkillData>(path);
    }

    private static PlayerData Player(string name)
    {
        string path = $"Assets/Script/Player Data/{name}.asset";

        return AssetDatabase.LoadAssetAtPath<PlayerData>(path);
    }

    private static T[] Find<T>() where T : UnityEngine.Object
    {
        return UnityEngine.Object.FindObjectsByType<T>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );
    }

    private static Sprite Sprite(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .First();
    }

    private static void Folder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        int separatorIndex = path.LastIndexOf('/');

        string parent = path.Substring(0, separatorIndex);
        string folderName = path.Substring(separatorIndex + 1);

        Folder(parent);

        AssetDatabase.CreateFolder(parent, folderName);
    }

    private static void Save(UnityEngine.Object asset)
    {
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssetIfDirty(asset);
    }

    private static void Set(
        UnityEngine.Object owner,
        string field,
        UnityEngine.Object value)
    {
        var serialized = new SerializedObject(owner);

        serialized.FindProperty(field).objectReferenceValue = value;

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Set(
        UnityEngine.Object owner,
        string field,
        UnityEngine.Object[] values)
    {
        var serialized = new SerializedObject(owner);
        var array = serialized.FindProperty(field);

        array.arraySize = values.Length;

        for (int i = 0; i < values.Length; i++)
        {
            array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
