using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static partial class ClassExpansionAssets
{
    private static readonly string[] PreviewEnemyIds =
    {
        "Auditor",
        "Raider",
        "Pilgrim"
    };

    public static void RenderEnemyPreviews()
    {
        Directory.CreateDirectory(ReportFolder);

        foreach (string id in PreviewEnemyIds)
        {
            EnemyData data = AssetDatabase.LoadAssetAtPath<EnemyData>(
                $"{EnemyFolder}/{id}.asset"
            );

            if (data == null || data.prefab == null)
            {
                Debug.LogWarning($"プレビュー対象のEnemyDataまたはPrefabがありません: {id}");
                continue;
            }

            Scene preview = EditorSceneManager.NewPreviewScene();
            RenderTexture renderTexture = null;
            Texture2D capturedImage = null;
            RenderTexture previousActive = RenderTexture.active;

            try
            {
                GameObject root = (GameObject)PrefabUtility.InstantiatePrefab(
                    data.prefab,
                    preview
                );

                foreach (BodyPart part in root.GetComponentsInChildren<BodyPart>(true))
                {
                    PolygonCollider2D collider = part.GetComponent<PolygonCollider2D>();

                    if (collider == null || collider.pathCount == 0)
                    {
                        continue;
                    }

                    LineRenderer line = part.gameObject.AddComponent<LineRenderer>();
                    line.useWorldSpace = false;
                    line.loop = true;
                    line.sortingOrder = 100;
                    line.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
                    line.startWidth = 0.035f;
                    line.endWidth = 0.035f;

                    Color color = part.partType == PartType.Face
                        ? Color.red
                        : part.partType == PartType.Hand
                            ? Color.cyan
                            : part.partType == PartType.Leg
                                ? Color.green
                                : Color.yellow;

                    line.startColor = color;
                    line.endColor = color;

                    Vector2[] points = collider.GetPath(0);
                    line.positionCount = points.Length;
                    line.SetPositions(points.Select(point => (Vector3)point).ToArray());
                }

                GameObject cameraObject = new GameObject("PreviewCamera", typeof(Camera));
                SceneManager.MoveGameObjectToScene(cameraObject, preview);

                Camera camera = cameraObject.GetComponent<Camera>();
                camera.scene = preview;
                camera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(preview);
                camera.transform.position = new Vector3(0f, 0f, -10f);
                camera.orthographic = true;
                camera.orthographicSize = 3.3f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.12f, 0.14f, 0.18f);

                renderTexture = new RenderTexture(900, 900, 24);
                camera.targetTexture = renderTexture;
                camera.Render();

                RenderTexture.active = renderTexture;
                capturedImage = new Texture2D(900, 900, TextureFormat.RGB24, false);
                capturedImage.ReadPixels(new Rect(0, 0, 900, 900), 0, 0);
                capturedImage.Apply();

                string filePath = Path.Combine(ReportFolder, $"{id}-colliders.png");
                File.WriteAllBytes(filePath, capturedImage.EncodeToPNG());
                Debug.Log($"敵部位のプレビューを保存しました: {filePath}");
            }
            finally
            {
                RenderTexture.active = previousActive;

                if (capturedImage != null)
                {
                    UnityEngine.Object.DestroyImmediate(capturedImage);
                }

                if (renderTexture != null)
                {
                    renderTexture.Release();
                    UnityEngine.Object.DestroyImmediate(renderTexture);
                }

                foreach (GameObject rootObject in preview.GetRootGameObjects())
                {
                    foreach (LineRenderer line in rootObject.GetComponentsInChildren<LineRenderer>(true))
                    {
                        if (line.sharedMaterial != null)
                        {
                            UnityEngine.Object.DestroyImmediate(line.sharedMaterial);
                        }
                    }
                }

                EditorSceneManager.ClosePreviewScene(preview);
            }
        }
    }
}
