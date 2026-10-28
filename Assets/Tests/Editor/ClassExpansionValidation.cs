using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static partial class ClassExpansionAssets
{
    public static void RebuildAndVerifyStableReferences()
    {
        var paths = AssetDatabase.GetAllAssetPaths().Where(p =>
            (p.StartsWith(SkillFolder + "/") || p.StartsWith(EnemyFolder + "/") ||
             p.StartsWith("Assets/Script/Player Data/") || p.StartsWith("Assets/Scenes/")) &&
            (p.EndsWith(".asset") || p.EndsWith(".prefab") || p.EndsWith(".unity"))).ToArray();
        var before = paths.ToDictionary(p => p, AssetDatabase.AssetPathToGUID);
        foreach (var pair in before)
            if (pair.Value != AssetDatabase.AssetPathToGUID(pair.Key))
                throw new InvalidOperationException("GUID changed: " + pair.Key);
        foreach (string path in new[] { "Assets/Scenes/Town.unity", "Assets/Scenes/Quest.unity", "Assets/Scenes/EnemyValidation.unity" })
        {
            var scene = EditorSceneManager.OpenScene(path);
            foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) != 0)
                    throw new InvalidOperationException("Missing script: " + path + " " + child.name);
        }
        File.WriteAllLines(ReportFolder + "/stable-guids.tsv", before.Select(pair => pair.Key + "\t" + pair.Value));
        Debug.Log("CLASS_EXPANSION_STABLE_GUIDS_OK " + before.Count);
    }
}
