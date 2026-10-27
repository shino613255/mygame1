using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class AutoplayAssetAudit
{
    public static void FitHud()
    {
        foreach(string scene in new[]{"Title","Town","Quest"}){
            EditorSceneManager.OpenScene("Assets/Scenes/"+scene+".unity");
            foreach(Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
                string name=canvas.name.Trim();
                if(name!="DialogUICanvas" && !(scene=="Quest" &&
                    (name=="PlayerUICanvas" || name=="EnemyUICanvas" || name=="StageUICanvas")))continue;
                var scaler=canvas.GetComponent<CanvasScaler>();
                scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution=new Vector2(1920,1080);
                scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
                EditorUtility.SetDirty(scaler);PrefabUtility.RecordPrefabInstancePropertyModifications(scaler);
            }
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }
        Debug.Log("AUTOPLAY_HUD_SCALERS_FIXED");
    }

    public static void InspectUi()
    {
        var report=new StringBuilder();
        foreach(string scene in new[]{"Town","Quest"}){
            EditorSceneManager.OpenScene("Assets/Scenes/"+scene+".unity");report.AppendLine("SCENE "+scene);
            foreach(Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
                if(canvas.name.Contains("Skill") || canvas.name.Contains("Fade"))continue;
                report.AppendLine("CANVAS "+canvas.name+" path="+PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(canvas));
                foreach(RectTransform rect in canvas.GetComponentsInChildren<RectTransform>(true))
                    report.AppendLine($"  {rect.name} parent={rect.parent?.name} size={rect.rect.size} pos={rect.anchoredPosition} anchors={rect.anchorMin}..{rect.anchorMax} pivot={rect.pivot} scale={rect.localScale}");
            }
        }
        File.WriteAllText("Logs/Autoplay/ui-hierarchy-before.txt",report.ToString());
    }
}
