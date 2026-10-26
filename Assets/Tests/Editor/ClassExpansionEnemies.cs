using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static partial class ClassExpansionAssets
{
    public static void BuildEnemyPrefabs()
    {
        Folder(EnemyFolder);
        var audit = new StringBuilder();
        foreach (string image in new[] { "img139.png", "img250.png", "img133.png" })
            AuditUnusedArtwork("Assets/Script/Charcter/" + image, audit);
        File.WriteAllText(ReportFolder + "/enemy-artwork-audit.txt", audit.ToString());

        BuildEnemy("Auditor", "魔導監察官", EnemyRole.Caster, "img139.png", 440, 80, 10, 26, 12, 28,
            SkillId.Auditor_ArcaneBolt, SkillId.Auditor_ManaLevy, SkillId.Auditor_ChargedLance, 100, 120, 110);
        BuildEnemy("Raider", "血斧の略奪者", EnemyRole.Brute, "img250.png", 700, 45, 24, 0, 8, 8,
            SkillId.Raider_CleavingBlow, SkillId.Raider_BloodFury, SkillId.Raider_ChargedAxe, 150, 180, 150);
        BuildEnemy("Pilgrim", "護符の巡礼者", EnemyRole.Warden, "img133.png", 560, 90, 8, 18, 20, 25,
            SkillId.Pilgrim_TalismanBolt, SkillId.Pilgrim_TalismanWard, SkillId.Pilgrim_Renewal, 130, 150, 120);
        CreateEnemyValidationScene();
        ValidateEnemyPrefabs();
        RenderEnemyPreviews();
        Debug.Log("CLASS_EXPANSION_ENEMY_PREFABS_OK");
    }

    private static void AuditUnusedArtwork(string selected, StringBuilder report)
    {
        using var sha = SHA256.Create();
        string hash = Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(selected)));
        string[] aliases = AssetDatabase.GetAllAssetPaths().Where(p => p.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            .Where(p => Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(p))) == hash).ToArray();
        foreach (string alias in aliases)
        {
            string[] uses = AssetDatabase.GetAllAssetPaths().Where(p =>
                (p.EndsWith(".unity") || p.EndsWith(".prefab") || p.EndsWith(".asset")) &&
                !p.StartsWith(EnemyFolder + "/", StringComparison.Ordinal) && p != "Assets/Scenes/EnemyValidation.unity" &&
                AssetDatabase.GetDependencies(p, true).Contains(alias)).ToArray();
            // Quest references are allowed only on a rerun after these expansion enemies were connected.
            uses = uses.Where(p => p != "Assets/Scenes/Quest.unity" ||
                !AssetDatabase.LoadAssetAtPath<EnemyData>(EnemyFolder + "/Auditor.asset")).ToArray();
            if (uses.Length != 0) throw new InvalidOperationException($"Artwork already used: {alias} => {string.Join(";", uses)}");
            report.AppendLine($"{alias}\tGUID={AssetDatabase.AssetPathToGUID(alias)}\tSHA256={hash}\tpre-existing dependencies: none");
        }
    }

    private static void BuildEnemy(string id, string name, EnemyRole role, string image,
        int hp, int mp, int at, int mag, int def, int mdef, SkillId standard, SkillId second, SkillId third,
        int faceHp, int handHp, int legHp)
    {
        string dataPath = EnemyFolder + "/" + id + ".asset";
        EnemyData data = AssetDatabase.LoadAssetAtPath<EnemyData>(dataPath);
        if (data == null) { data = ScriptableObject.CreateInstance<EnemyData>(); AssetDatabase.CreateAsset(data, dataPath); }
        data.enemyName = name; data.role = role; data.enemyType = EnemyType.Normal;
        data.maxHp = hp; data.maxMp = mp; data.at = at; data.mag = mag; data.def = def; data.mdef = mdef;
        data.evasionRate = 0; data.icon = Sprite("Assets/Script/Charcter/" + image);
        data.attackSkill = LoadSkill(standard); data.skillList = new System.Collections.Generic.List<SkillData> { LoadSkill(second), LoadSkill(third) };
        data.telegraphedSkill = role == EnemyRole.Warden ? null : LoadSkill(third);
        string prefabPath = EnemyFolder + "/" + id + ".prefab";
        bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null;
        GameObject root = exists ? PrefabUtility.LoadPrefabContents(prefabPath) : new GameObject(name);
        root.name = name; root.transform.position = Vector3.zero; root.transform.localScale = Vector3.one * .6f;
        var renderer = root.GetComponent<SpriteRenderer>();
        if (renderer == null) renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = data.icon; renderer.sortingOrder = 5;
        var enemy = root.GetComponent<EnemyManager>();
        if (enemy == null) enemy = root.AddComponent<EnemyManager>();
        enemy.Setup(data);
        if (root.GetComponent<EnemyPartsController>() == null) root.AddComponent<EnemyPartsController>();
        if (root.GetComponent<SkillCooldowns>() == null) root.AddComponent<SkillCooldowns>();
        Vector2[][] polygons = PartPolygons(id);
        AddPart(root, data.icon, "Face", PartType.Face, faceHp, polygons[0]);
        AddPart(root, data.icon, "LeftHand", PartType.Hand, handHp, polygons[1]);
        AddPart(root, data.icon, "RightHand", PartType.Hand, handHp, polygons[2]);
        AddPart(root, data.icon, "Leg", PartType.Leg, legHp, polygons[3]);
        AddPart(root, data.icon, "Belly", PartType.Belly, 999, polygons[4]);
        data.prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Save(data);
        if (exists) PrefabUtility.UnloadPrefabContents(root); else UnityEngine.Object.DestroyImmediate(root);
    }

    private static void AddPart(GameObject root, Sprite sprite, string name, PartType type, int hp, Vector2[] pixels)
    {
        Transform existing = root.transform.Find(name);
        GameObject go = existing != null ? existing.gameObject : new GameObject(name);
        go.transform.SetParent(root.transform, false); go.transform.localPosition = Vector3.zero;
        var collider = go.GetComponent<PolygonCollider2D>();
        if (collider == null) collider = go.AddComponent<PolygonCollider2D>();
        collider.pathCount = 1;
        collider.SetPath(0, pixels.Select(p => new Vector2(
            (p.x / 1000f * sprite.rect.width - sprite.pivot.x) / sprite.pixelsPerUnit,
            ((1f - p.y / 1000f) * sprite.rect.height - sprite.pivot.y) / sprite.pixelsPerUnit)).ToArray());
        var part = go.GetComponent<BodyPart>();
        if (part == null) part = go.AddComponent<BodyPart>();
        part.partType = type; part.canBreak = type != PartType.Belly; part.maxPartHp = hp;
    }

    private static Vector2[] Points(params float[] coordinates)
    {
        var points = new Vector2[coordinates.Length / 2];
        for (int i = 0; i < points.Length; i++) points[i] = new Vector2(coordinates[i * 2], coordinates[i * 2 + 1]);
        return points;
    }

    private static Vector2[][] PartPolygons(string id)
    {
        switch (id)
        {
            case "Auditor": return new[] {
                Points(440,130, 515,95, 600,105, 653,240, 635,320, 452,320, 430,245),
                Points(258,425, 320,425, 348,475, 325,530, 275,540, 250,480),
                Points(690,430, 760,455, 785,510, 720,550, 690,505),
                Points(360,910, 485,900, 540,920, 660,925, 730,975, 720,995, 602,995, 550,965, 475,995, 350,995),
                Points(475,365, 595,365, 625,610, 460,610) };
            case "Raider": return new[] {
                Points(350,105, 455,110, 510,215, 510,285, 435,325, 360,270, 333,195),
                Points(227,163, 295,150, 335,195, 313,250, 235,278, 195,240),
                Points(790,305, 850,320, 895,376, 870,455, 796,469, 753,405),
                Points(280,620, 490,595, 650,680, 745,815, 848,965, 784,995, 690,910, 535,755, 420,720, 322,908, 195,947, 208,835, 265,735),
                Points(370,370, 560,365, 600,520, 538,567, 403,555) };
            default: return new[] {
                Points(390,222, 545,205, 618,280, 625,407, 557,464, 438,440, 376,340),
                Points(220,550, 265,548, 300,590, 277,638, 229,629, 210,590),
                Points(620,535, 752,552, 805,642, 800,801, 695,839, 628,750),
                Points(382,850, 590,850, 651,942, 630,990, 548,984, 450,989, 374,973),
                Points(400,500, 565,512, 597,770, 430,820, 385,666) };
        }
    }

    public static void ValidateEnemyPrefabs()
    {
        var report = new StringBuilder();
        foreach (string id in new[] { "Auditor", "Raider", "Pilgrim" })
        {
            var data = AssetDatabase.LoadAssetAtPath<EnemyData>(EnemyFolder + "/" + id + ".asset");
            if (data == null || data.prefab == null || data.prefab.GetComponent<EnemyManager>().data != data)
                throw new InvalidOperationException("Broken enemy reference: " + id);
            BodyPart[] parts = data.prefab.GetComponentsInChildren<BodyPart>(true);
            if (parts.Length != 5 || parts.Count(p => p.partType == PartType.Hand) != 2 ||
                parts.Any(p => p.GetComponent<PolygonCollider2D>().GetPath(0).Length < 3) ||
                parts.Single(p => p.partType == PartType.Belly).canBreak)
                throw new InvalidOperationException("Invalid enemy parts: " + id);
            report.AppendLine($"{id}\t{AssetDatabase.GetAssetPath(data.icon)}\tGUID={AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(data.icon))}\t{parts.Length} parts");
        }
        File.WriteAllText(ReportFolder + "/enemy-prefabs.tsv", report.ToString());
    }

    public static void RenderEnemyPreviews()
    {
        foreach (string id in new[] { "Auditor", "Raider", "Pilgrim" })
        {
            Scene preview = EditorSceneManager.NewPreviewScene();
            var data = AssetDatabase.LoadAssetAtPath<EnemyData>(EnemyFolder + "/" + id + ".asset");
            var root = (GameObject)PrefabUtility.InstantiatePrefab(data.prefab, preview);
            foreach (BodyPart part in root.GetComponentsInChildren<BodyPart>())
            {
                var line = part.gameObject.AddComponent<LineRenderer>();
                line.useWorldSpace = false; line.loop = true; line.sortingOrder = 100;
                line.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
                line.startWidth = line.endWidth = .035f;
                Color color = part.partType == PartType.Face ? Color.red : part.partType == PartType.Hand ? Color.cyan : part.partType == PartType.Leg ? Color.green : Color.yellow;
                line.startColor = line.endColor = color;
                Vector2[] path = part.GetComponent<PolygonCollider2D>().GetPath(0);
                line.positionCount = path.Length; line.SetPositions(path.Select(p => (Vector3)p).ToArray());
            }
            var cameraObject = new GameObject("PreviewCamera", typeof(Camera));
            SceneManager.MoveGameObjectToScene(cameraObject, preview);
            var camera = cameraObject.GetComponent<Camera>();
            camera.scene = preview;
            camera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(preview);
            camera.transform.position = new Vector3(0, 0, -10); camera.orthographic = true; camera.orthographicSize = 3.3f;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.12f, .14f, .18f);
            var target = new RenderTexture(900, 900, 24); camera.targetTexture = target;
            camera.Render(); RenderTexture.active = target;
            var image = new Texture2D(900, 900, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 900, 900), 0, 0); image.Apply();
            File.WriteAllBytes(ReportFolder + "/" + id + "-colliders.png", image.EncodeToPNG());
            RenderTexture.active = null; camera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(image); target.Release(); UnityEngine.Object.DestroyImmediate(target);
            foreach (LineRenderer line in root.GetComponentsInChildren<LineRenderer>()) UnityEngine.Object.DestroyImmediate(line.sharedMaterial);
            EditorSceneManager.ClosePreviewScene(preview);
        }
    }

    private static void CreateEnemyValidationScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camera = new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>();
        camera.tag = "MainCamera"; camera.transform.position = new Vector3(0, 0, -10);
        camera.orthographic = true; camera.orthographicSize = 4;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.08f, .1f, .14f);
        var harness = new GameObject("EnemyValidation", typeof(EnemyValidationHarness)).GetComponent<EnemyValidationHarness>();
        harness.enemies = new[] { "Auditor", "Raider", "Pilgrim" }.Select(id =>
            AssetDatabase.LoadAssetAtPath<EnemyData>(EnemyFolder + "/" + id + ".asset")).ToArray();
        var canvas = new GameObject("ValidationCanvas", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster)).GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        harness.instructions = Text(canvas.transform, "Instructions", new Vector2(0, 170), new Vector2(600, 100));
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), "Assets/Scenes/EnemyValidation.unity");
    }

    public static void ConnectEnemies()
    {
        ValidateEnemyPrefabs();
        EditorSceneManager.OpenScene("Assets/Scenes/Quest.unity");
        var quest = Find<QuestManager>().Single();
        var serialized = new SerializedObject(quest);
        var floors = serialized.FindProperty("floors");
        var report = new StringBuilder();
        for (int i = 0; i < floors.arraySize; i++)
        {
            var floor = floors.GetArrayElementAtIndex(i);
            var candidates = floor.FindPropertyRelative("enemyDatas");
            bool containsBoss = Enumerable.Range(0, candidates.arraySize).Any(n =>
                candidates.GetArrayElementAtIndex(n).objectReferenceValue is EnemyData d && d.enemyType == EnemyType.Boss);
            if (floor.FindPropertyRelative("isBossFloor").boolValue || containsBoss || i == 0) continue;
            foreach (string id in new[] { "Auditor", "Raider", "Pilgrim" })
            {
                if (id == "Raider" && i < 2) continue;
                if (i > 3 || (id == "Auditor" && i > 2)) continue;
                var data = AssetDatabase.LoadAssetAtPath<EnemyData>(EnemyFolder + "/" + id + ".asset");
                bool already = Enumerable.Range(0, candidates.arraySize).Any(n => candidates.GetArrayElementAtIndex(n).objectReferenceValue == data);
                if (!already) { candidates.InsertArrayElementAtIndex(candidates.arraySize); candidates.GetArrayElementAtIndex(candidates.arraySize - 1).objectReferenceValue = data; }
                report.AppendLine($"Floor {i + 1}: {data.enemyName}");
            }
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        File.WriteAllText(ReportFolder + "/enemy-encounters.txt", report.ToString());
        Debug.Log("CLASS_EXPANSION_ENCOUNTERS_OK");
    }
}
