using System;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

[InitializeOnLoad]
public static class AutoplayEditorRunner
{
    private const string Pending="BreakTactics.Autoplay.Result";
    private const string OutputVariable="BREAKTACTICS_TEST_OUTPUT";
    private static TestRunnerApi api;
    private static Results callbacks;

    static AutoplayEditorRunner(){EditorApplication.delayCall+=RegisterPending;}
    private static void RegisterPending(){if(!string.IsNullOrEmpty(SessionState.GetString(Pending,"")))Register();}
    private static void Register(){
        if(api!=null)return;
        api=ScriptableObject.CreateInstance<TestRunnerApi>();
        callbacks=new Results();api.RegisterCallbacks(callbacks);
    }

    [MenuItem("Tools/BREAK TACTICS/Autoplay/1 Fix verified HUD clipping %&F7")]
    public static void FitHud(){
        if(!Ready())return;
        var setup=EditorSceneManager.GetSceneManagerSetup();
        try{AutoplayAssetAudit.FitHud();}finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
    }
    [MenuItem("Tools/BREAK TACTICS/Autoplay/2 Test remaining roles and fixes %&F8")]
    public static void Remaining()=>Run(TestMode.PlayMode,"remaining",new[]{"\\.Autoplay(Mage|Berserker|FirstEncounterDisplaysFloorOne|HudRemainsInsideTheGameView)$"});
    [MenuItem("Tools/BREAK TACTICS/Autoplay/3 Run final PlayMode suite %&F9")]
    public static void AllPlay()=>Run(TestMode.PlayMode,"playmode-final",null);
    [MenuItem("Tools/BREAK TACTICS/Autoplay/4 Run final EditMode suite %&F10")]
    public static void AllEdit()=>Run(TestMode.EditMode,"editmode-final",null);

    private static bool Ready(){
        if(EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer){
            Debug.LogWarning("Autoplay waits for compilation, build and Play Mode to finish.");return false;
        }
        if(EditorSceneManager.GetSceneManagerSetup().Any(s=>string.IsNullOrEmpty(s.path) || UnityEngine.SceneManagement.SceneManager.GetSceneByPath(s.path).isDirty)){
            Debug.LogWarning("Save the current named scenes before running autoplay; unsaved scenes are not replaced.");return false;
        }
        return true;
    }
    private static void Run(TestMode mode,string name,string[] groups){
        if(!Ready())return;
        Directory.CreateDirectory("Logs/Autoplay/Regression");
        SessionState.SetString(Pending,"Logs/Autoplay/"+name+".xml");
        SessionState.SetString(Pending+".PreviousOutput",Environment.GetEnvironmentVariable(OutputVariable)??"");
        SessionState.SetBool(Pending+".Background",Application.runInBackground);Application.runInBackground=true;
        Environment.SetEnvironmentVariable(OutputVariable,"Logs/Autoplay/Regression");
        Register();api.Execute(new ExecutionSettings(new Filter{testMode=mode,groupNames=groups}));
    }
    private sealed class Results:ICallbacks
    {
        public void RunStarted(ITestAdaptor test){File.AppendAllText("Logs/Autoplay/editor-runner.tsv","START\t"+test.Name+"\n");}
        public void TestStarted(ITestAdaptor test){}
        public void TestFinished(ITestResultAdaptor result){
            if(!result.HasChildren)File.AppendAllText("Logs/Autoplay/editor-runner.tsv",result.FullName+"\t"+result.ResultState+"\t"+result.Message+"\n");
        }
        public void RunFinished(ITestResultAdaptor result){
            string path=SessionState.GetString(Pending,"");if(string.IsNullOrEmpty(path))return;
            var document=new XmlDocument();var root=document.CreateElement("test-run");document.AppendChild(root);
            root.SetAttribute("result",result.ResultState);
            root.SetAttribute("total",(result.PassCount+result.FailCount+result.SkipCount+result.InconclusiveCount).ToString());
            root.SetAttribute("passed",result.PassCount.ToString());root.SetAttribute("failed",result.FailCount.ToString());
            root.SetAttribute("skipped",result.SkipCount.ToString());root.InnerXml=result.ToXml().OuterXml;document.Save(path);
            File.AppendAllText("Logs/Autoplay/editor-runner.tsv",$"END\t{path}\tpassed={result.PassCount}\tfailed={result.FailCount}\n");
            Environment.SetEnvironmentVariable(OutputVariable,SessionState.GetString(Pending+".PreviousOutput","") is string value && value.Length>0 ? value : null);
            Application.runInBackground=SessionState.GetBool(Pending+".Background",false);
            SessionState.EraseString(Pending);SessionState.EraseString(Pending+".PreviousOutput");
            Debug.Log("Autoplay result saved: "+path+" passed="+result.PassCount+" failed="+result.FailCount);
        }
    }
}
