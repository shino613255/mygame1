using System;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

[InitializeOnLoad]
public static class SkillUiLargeAssets
{
    public const string Report = "Logs/SkillUiResize20261027";

    private const string Pending = "BreakTactics.SkillUiResize.Pending";

    private static TestRunnerApi api;

    static SkillUiLargeAssets()
    {
        EditorApplication.delayCall += () =>
        {
            if (!string.IsNullOrEmpty(SessionState.GetString(Pending, "")))
            {
                Register();
            }
        };
    }

    [MenuItem("Tools/BREAK TACTICS/Skill UI/Record starting dimensions %&F5")]
    public static void RecordBefore()
    {
        if (!Ready())
        {
            return;
        }

        if (File.Exists(Report + "/before-ui.txt"))
        {
            Debug.Log(
                "The starting Skill UI dimensions are already recorded; preserving that snapshot."
            );

            return;
        }

        EditorSceneManager.OpenScene("Assets/Scenes/Quest.unity");

        var pager = UnityEngine.Object.FindObjectsByType<SkillPanelPager>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        ).Single();

        var serialized = new SerializedObject(pager);
        var report = new StringBuilder();

        foreach (string field in new[] { "previousButton", "nextButton", "pageLabel" })
        {
            var component = (Component)serialized.FindProperty(field).objectReferenceValue;

            report.AppendLine(
                field + "\tsize=" + ((RectTransform)component.transform).rect.size
            );
        }

        var panel = (RectTransform)pager.transform;

        report.AppendLine(
            "Panel anchors=" + panel.anchorMin +
            ".." + panel.anchorMax +
            " scale=" + panel.localScale
        );

        foreach (Canvas canvas in UnityEngine.Object.FindObjectsByType<Canvas>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None))
        {
            var scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();

            if (scaler != null)
            {
                report.AppendLine(
                    $"Canvas {canvas.name} " +
                    $"mode={canvas.renderMode} " +
                    $"scaler={scaler.uiScaleMode} " +
                    $"factor={scaler.scaleFactor} " +
                    $"reference={scaler.referenceResolution}"
                );
            }
        }

        var slots = pager.GetComponentsInChildren<SkillSlotUI>(true);

        foreach (SkillSlotUI slot in slots)
        {
            report.AppendLine(
                $"Serialized {slot.name}: " +
                $"size={((RectTransform)slot.transform).rect.size} " +
                $"font={slot.Label.fontSize} " +
                $"min={slot.Label.fontSizeMin} " +
                $"auto={slot.Label.enableAutoSizing}"
            );
        }

        TMP_Text label = slots[0].Label;

        foreach (SkillData skill in Enum.GetValues(typeof(PlayerRole))
                     .Cast<PlayerRole>()
                     .SelectMany(SkillPresentationAssets.Loadout)
                     .Distinct())
        {
            float width = label.GetPreferredValues(
                skill.skillName + "\n" + skill.description,
                float.PositiveInfinity,
                float.PositiveInfinity
            ).x;

            report.AppendLine(
                $"Text30 {skill.name}: unwrappedWidth={width}"
            );
        }

        Directory.CreateDirectory(Report);

        File.WriteAllText(
            Report + "/before-ui.txt",
            report.ToString()
        );

        Debug.Log("SKILL_UI_LARGE_BASELINE_RECORDED");
    }

    [MenuItem("Tools/BREAK TACTICS/Skill UI/Test all pages %&F11")]
    public static void TestPages()
    {
        Run(
            TestMode.PlayMode,
            "pages",
            new[] { "\\.TownSelectionAndEverySkillPageWorkForAllClasses$" }
        );
    }

    [MenuItem("Tools/BREAK TACTICS/Skill UI/Test scroll controls %&F2")]
    public static void TestScroll()
    {
        Run(
            TestMode.PlayMode,
            "scroll",
            new[] { "\\.LargeControlsScrollNavigateAndReopenWithoutDuplicatingButtons$" }
        );
    }

    [MenuItem("Tools/BREAK TACTICS/Skill UI/Test PlayMode regression %&F12")]
    public static void TestPlay()
    {
        Run(
            TestMode.PlayMode,
            "playmode",
            new[] { "ClassExpansionPlayModeTests\\.(?!Autoplay)" }
        );
    }

    [MenuItem("Tools/BREAK TACTICS/Skill UI/Test EditMode regression %&F3")]
    public static void TestEdit()
    {
        Run(
            TestMode.EditMode,
            "editmode",
            null
        );
    }

    [MenuItem("Tools/BREAK TACTICS/Skill UI/Open Town for manual check")]
    public static void OpenTown()
    {
        if (Ready())
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Town.unity");
        }
    }

    [MenuItem("Tools/BREAK TACTICS/Skill UI/Capture Game View %&F1")]
    public static void CaptureGameView()
    {
        if (!EditorApplication.isPlaying)
        {
            return;
        }

        Directory.CreateDirectory(Report);

        ScreenCapture.CaptureScreenshot(
            Report + "/gameview-" +
            DateTime.Now.ToString("HHmmss") +
            ".png"
        );
    }

    private static bool Ready()
    {
        if (EditorApplication.isCompiling ||
            EditorApplication.isPlayingOrWillChangePlaymode ||
            EditorSceneManager.GetSceneManagerSetup().Any(
                s => string.IsNullOrEmpty(s.path) ||
                     UnityEngine.SceneManagement.SceneManager
                         .GetSceneByPath(s.path).isDirty))
        {
            Debug.LogWarning(
                "Skill UI verification requires compilation to finish and saved scenes outside Play Mode."
            );

            return false;
        }

        return true;
    }

    private static void Register()
    {
        if (api != null)
        {
            return;
        }

        api = ScriptableObject.CreateInstance<TestRunnerApi>();
        api.RegisterCallbacks(new Results());
    }

    private static void Run(
        TestMode mode,
        string name,
        string[] groups)
    {
        if (!Ready())
        {
            return;
        }

        Directory.CreateDirectory(Report);

        SessionState.SetString(
            Pending,
            Report + "/" + name + ".xml"
        );

        SessionState.SetString(
            Pending + ".Output",
            Environment.GetEnvironmentVariable("BREAKTACTICS_TEST_OUTPUT") ?? ""
        );

        SessionState.SetBool(
            Pending + ".Background",
            Application.runInBackground
        );

        Application.runInBackground = true;

        Environment.SetEnvironmentVariable(
            "BREAKTACTICS_TEST_OUTPUT",
            Report
        );

        Register();

        api.Execute(
            new ExecutionSettings(
                new Filter
                {
                    testMode = mode,
                    groupNames = groups
                }
            )
        );
    }

    private sealed class Results : ICallbacks
    {
        public void RunStarted(ITestAdaptor test)
        {
        }

        public void TestStarted(ITestAdaptor test)
        {
        }

        public void TestFinished(ITestResultAdaptor result)
        {
            if (!result.HasChildren)
            {
                File.AppendAllText(
                    Report + "/tests.tsv",
                    result.FullName + "\t" +
                    result.ResultState + "\t" +
                    result.Message + "\n"
                );
            }
        }

        public void RunFinished(ITestResultAdaptor result)
        {
            string path = SessionState.GetString(Pending, "");

            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            var document = new System.Xml.XmlDocument();
            var root = document.CreateElement("test-run");

            document.AppendChild(root);

            root.SetAttribute(
                "result",
                result.ResultState
            );

            root.SetAttribute(
                "total",
                (result.PassCount +
                 result.FailCount +
                 result.SkipCount +
                 result.InconclusiveCount).ToString()
            );

            root.SetAttribute(
                "passed",
                result.PassCount.ToString()
            );

            root.SetAttribute(
                "failed",
                result.FailCount.ToString()
            );

            root.SetAttribute(
                "skipped",
                result.SkipCount.ToString()
            );

            root.InnerXml = result.ToXml().OuterXml;

            document.Save(path);

            string previous = SessionState.GetString(
                Pending + ".Output",
                ""
            );

            Environment.SetEnvironmentVariable(
                "BREAKTACTICS_TEST_OUTPUT",
                previous.Length == 0 ? null : previous
            );

            Application.runInBackground = SessionState.GetBool(
                Pending + ".Background",
                false
            );

            SessionState.EraseString(Pending);

            Debug.Log(
                $"Skill UI tests saved: {path}; " +
                $"passed={result.PassCount}, " +
                $"failed={result.FailCount}, " +
                $"skipped={result.SkipCount}"
            );
        }
    }
}
