#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public partial class ClassExpansionPlayModeTests
{
    private const string AutoFolder = "Logs/Autoplay";
    private static readonly string[][] AutoRoutes = {
        new[] { "00_Attacker", "Auditor", "Raider", "04_Tank Boss" },
        new[] { "01_Tank", "Pilgrim", "02_Ninja", "04_Tank Boss" },
        new[] { "02_Ninja", "03_Elite Attacker", "Pilgrim", "04_Tank Boss" }
    };

    [UnityTest] public IEnumerator AutoplayWarrior() => AutoRole("Warrior");
    [UnityTest] public IEnumerator AutoplayMage() => AutoRole("Mage");
    [UnityTest] public IEnumerator AutoplayBerserker() => AutoRole("Berserker");

    private static IEnumerator AutoRole(string role)
    {
        for (int route = 0; route < AutoRoutes.Length; route++) yield return AutoQuest(role, route, false);
        yield return AutoQuest(role, 0, true);
        yield return AutoEnter(role);
        var player = Find("PlayerManager");
        Assert.That((int)Get(player, "hp"), Is.EqualTo((int)Get(player, "maxHp")));
        Assert.That(((IEnumerable)Get(player, "Skills")).Cast<object>().Count(), Is.EqualTo(6));
        Assert.That((bool)Get(player, "HasLifeDrainReward"), Is.False);
        Assert.That((bool)Get(player, "HasFocusedAttackReward"), Is.False);
        AutoWrite("retry", role, "Fresh HP/MP, six skills and cleared growth rewards");
    }

    private static IEnumerator AutoEnter(string role)
    {
        Directory.CreateDirectory(AutoFolder);
        Time.timeScale = 20; Application.runInBackground = true;
        SceneManager.LoadScene("Town"); yield return null; yield return null;
        Set(Find("DialogTextManager"), "intervalForCharacterDisplay", .001f);
        Set(Find("FadeIOManager"), "fadeDuration", .05f);
        var button = FindAll("PlayerSelectButton").Single(b => Get(Get(b, "playerData"), "role").ToString() == role);
        AutoPointer(button.GetComponent<Button>());
        yield return AutoWait(() => SceneManager.GetActiveScene().name == "Quest", "Town to Quest");
        yield return null; yield return null;
        yield return AutoWait(() => !(bool)Get(Find("DialogTextManager"), "HasPendingLines") &&
            (bool)Get(Find("DialogTextManager"), "IsCompleteDisplayText"), "Quest introduction");
        AutoWrite("start", role, "Town button -> Quest; no runtime stat overrides");
    }

    private static IEnumerator AutoQuest(string role, int route, bool defeat)
    {
        yield return AutoEnter(role);
        string run = role + "-" + (defeat ? "defeat" : "route-" + route);
        Component quest = Find("QuestManager"), player = Find("PlayerManager"), battle = Find("BattleManager");
        var knownEnemies = new HashSet<int>(); var rejected = new HashSet<string>();
        int actions = 0, maxFloor = 0; float progress = Time.realtimeSinceStartup;
        string lastMessage = null;
        using (var view = new AutoView(1920, 1080))
        {
            AutoEncounterSeed(quest, AutoRoutes[route][0]);
            AutoPointer(((GameObject)Get(Get(quest, "stageUI"), "nextButton")).GetComponent<Button>());
            while (SceneManager.GetActiveScene().name == "Quest" && !(bool)Get(quest, "isQuestCleared"))
            {
                var dialog = Find("DialogTextManager");
                string message = (string)Get(dialog, "currentText");
                if (message != lastMessage) { AutoWrite("message", run, message); lastMessage = message; }
                if (AutoAdvanceDialog()) { yield return null; continue; }
                int floor = (int)Get(quest, "currentFloorIndex"); maxFloor = Mathf.Max(maxFloor, floor + 1);
                var growth = Get(quest, "growthSelectionManager");
                var growthPanel = (GameObject)Get(growth, "growthPanel");
                if (growthPanel.activeInHierarchy)
                {
                    Assert.That((int)Get(Get(player, "BattleEffects"), "TechniqueCount"), Is.Zero);
                    Assert.That((int)Get(Get(battle, "SkillVfx"), "ActiveEffectCount"), Is.Zero);
                    AutoFrame(run + "-floor-" + (floor + 1) + "-growth");
                    var offers = ((IEnumerable)Get(growth, "CurrentOffers")).Cast<object>().ToArray();
                    string[] preferred = { "LifeDrain", role == "Mage" ? "MagicAttackUp" : "AttackUp", "FocusedAttack", "MaxHpUp", "PrecisionShot" };
                    int index = preferred.Select(id => Array.FindIndex(offers, x => x.ToString() == id)).FirstOrDefault(i => i >= 0);
                    AutoWrite("reward", run, "floor=" + (floor + 1) + " " + offers[index]);
                    AutoEncounterSeed(quest, AutoRoutes[route][Mathf.Min(floor + 1, 3)]);
                    string[] names = { "HpUpButton", "MagicAttackUpButton", "DefenceUpButton" };
                    var choice = growthPanel.transform.Find(names[index]).GetComponent<Button>();
                    AutoPointer(choice); AutoPointer(choice);
                    progress = Time.realtimeSinceStartup; yield return null; continue;
                }
                Component enemy = Get(battle, "enemy") as Component;
                if (enemy != null && knownEnemies.Add(enemy.GetInstanceID()))
                {
                    AutoWrite("encounter", run, $"floor={floor+1} enemy={((UnityEngine.Object)Get(enemy,"data")).name} playerHP={Get(player,"hp")}/{Get(player,"maxHp")} MP={Get(player,"mp")}/{Get(player,"maxMp")}");
                    AutoFrame(run + "-floor-" + (floor + 1) + "-encounter");
                    progress = Time.realtimeSinceStartup;
                }
                if (enemy != null && (bool)Call(battle, "CanReceivePlayerInput"))
                {
                    var skills = ((IEnumerable)Get(player, "Skills")).Cast<UnityEngine.Object>().ToArray();
                    var unavailable = skills.FirstOrDefault(s => (int)Get(player, "mp") < (int)Call(player, "GetSkillMpCost", s) && !rejected.Contains(s.name));
                    if (unavailable == null && role == "Berserker")
                        unavailable = skills.FirstOrDefault(s => Get(s,"id").ToString() == "Berserker_BloodPrice" && (int)Get(player,"hp") <= Mathf.CeilToInt((int)Get(player,"maxHp")*.1f) && !rejected.Contains(s.name));
                    if (unavailable != null)
                    {
                        int hp = (int)Get(player,"hp"), mp = (int)Get(player,"mp");
                        yield return AutoSelect(battle, player, unavailable);
                        Assert.That((int)Get(player,"hp"), Is.EqualTo(hp)); Assert.That((int)Get(player,"mp"), Is.EqualTo(mp));
                        rejected.Add(unavailable.name); AutoWrite("rejected", run, unavailable.name + " HP=" + hp + " MP=" + mp);
                        yield return AutoWait(() => (bool)Call(battle,"CanReceivePlayerInput"), "rejected choice message");
                    }
                    var part = AutoPart(enemy, battle);
                    var skill = AutoChoose(player, enemy, part, skills, defeat);
                    int hpBefore = (int)Get(player,"hp"), mpBefore = (int)Get(player,"mp"), enemyBefore = (int)Get(enemy,"hp");
                    int cost = skill != null ? (int)Call(player,"GetSkillMpCost",skill) : 0;
                    if (skill != null) yield return AutoSelect(battle, player, skill);
                    if (skill == null || Get(skill,"targetType").ToString() != "Self")
                    {
                        yield return AutoWait(() => (bool)Call(battle,"CanReceivePlayerInput"), "attack selection");
                        var panel = (GameObject)Get(battle,"skillSelectionPanel");
                        if (panel.activeSelf) { AutoPointer(panel.transform.Find("CloseSkills").GetComponent<Button>()); yield return null; }
                        Physics2D.SyncTransforms();
                        Vector2 point = AutoPartPoint(part);
                        Call(battle, "TryPickBodyPart", point);
                        Assert.That((bool)Get(battle,"waitingTap"), Is.False, "Part click must be accepted: " + part.name);
                    }
                    int expected = mpBefore - cost;
                    if (skill != null && Get(skill,"skillType").ToString() == "RecoverMp") expected = Mathf.Min((int)Get(player,"maxMp"), expected + (int)Get(skill,"power"));
                    Assert.That((int)Get(player,"mp"), Is.EqualTo(expected), "One MP cost per action");
                    Assert.That((int)Get(player,"hp"), Is.InRange(0,(int)Get(player,"maxHp")));
                    AutoWrite("action", run, $"floor={floor+1} #{++actions} {(skill != null ? skill.name : "Normal")}->{Get(part,"partType")} HP {hpBefore}->{Get(player,"hp")} MP {mpBefore}->{Get(player,"mp")} enemy {enemyBefore}->{Get(enemy,"hp")} broken={Get(part,"IsBroken")} techniques={Get(Get(player,"BattleEffects"),"TechniqueCount")}");
                    if (actions % 20 == 0) AutoFrame(run + "-action-" + actions);
                    progress = Time.realtimeSinceStartup;
                    Assert.That(actions, Is.LessThan(700), "Autoplay policy exhausted its action budget");
                }
                Assert.That(Time.realtimeSinceStartup - progress, Is.LessThan(20), "No gameplay progress; " + run + " " + lastMessage);
                yield return null;
            }
            bool cleared = SceneManager.GetActiveScene().name == "Quest" && (bool)Get(quest,"isQuestCleared");
            AutoWrite("result", run, $"{(cleared ? "CLEAR" : "DEFEAT")} reached={maxFloor}/4 actions={actions} encounters={knownEnemies.Count}");
            if (cleared)
            {
                AutoFrame(run + "-clear");
                AutoPointer(((GameObject)Get(Get(quest,"stageUI"),"toTownButton")).GetComponent<Button>());
                yield return AutoWait(() => SceneManager.GetActiveScene().name == "Town", "clear return to Town");
            }
            else Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Town"));
        }
    }

    private static UnityEngine.Object AutoChoose(Component player, Component enemy, Component part, UnityEngine.Object[] skills, bool defeat)
    {
        var rules = Resolve("SkillUseRules");
        bool Ready(UnityEngine.Object s) {
            object[] args = { player, Get(s,"targetType").ToString()=="Self" ? (object)player : enemy, s, null };
            return (bool)rules.GetMethod("CanUse").Invoke(null,args);
        }
        UnityEngine.Object FindSkill(string id) => skills.FirstOrDefault(s => (Get(s,"id").ToString()==id || s.name==id) && Ready(s));
        var role = Get(player,"Role").ToString();
        float hp = (int)Get(player,"hp") / (float)(int)Get(player,"maxHp");
        int mp = (int)Get(player,"mp");
        if (defeat) return role == "Mage" ? FindSkill("Skill_MpHeal") ?? FindSkill("Skill_Heal") : FindSkill("Common_Guard");
        if ((bool)Get(player,"IsPoisoned")) { var antidote=FindSkill("Common_Antidote"); if(antidote!=null) return antidote; }
        if ((bool)Get(enemy,"IsCounterActive")) {
            var safe = FindSkill("Common_Guard") ?? FindSkill("Skill_MpHeal") ?? FindSkill("Skill_Heal") ?? FindSkill("Common_Aim") ?? FindSkill("Berserker_WarCry");
            if(safe!=null) return safe;
        }
        if (role=="Mage") {
            if (hp<.65f && mp>=10) { var heal=FindSkill("Skill_Heal"); if(heal!=null) return heal; }
            if (mp<40) { var mana=FindSkill("Skill_MpHeal"); if(mana!=null) return mana; }
            if (!(bool)Get(enemy,"IsFrozen") && !(bool)Get(enemy,"IsBurning")) { var frost=FindSkill("Mage_FrostBind"); if(frost!=null) return frost; }
            if (!(bool)Get(part,"IsBroken")) { var piercer=FindSkill("Mage_FlamePiercer"); if(piercer!=null) return piercer; }
            return FindSkill("Skill_fire") ?? FindSkill("Mage_ArcaneBurst");
        }
        if (role=="Warrior") {
            if(hp<.65f) {var heal=FindSkill("Common_FirstAid"); if(heal!=null) return heal;}
            if (!(bool)Get(part,"IsBroken") && mp>16) return FindSkill("Warrior_BreakSlash");
            return mp>25 ? FindSkill("Warrior_TacticalSlash") : null;
        }
        if(hp<.8f) { var cry=FindSkill("Berserker_WarCry"); if(cry!=null) return cry; }
        if(hp>.55f && (bool)Get(part,"IsBroken")) { var blood=FindSkill("Berserker_BloodPrice"); if(blood!=null) return blood; }
        return FindSkill("Berserker_Rend");
    }

    private static Component AutoPart(Component enemy, Component battle)
    {
        var parts = enemy.GetComponentsInChildren(Resolve("BodyPart"));
        string[] order = (bool)Get(Get(battle,"enemyActionPlan"),"isTelegraphed") ? new[] {"Hand","Face","Leg"} : new[] {"Face","Leg","Hand"};
        foreach(string type in order) {var part=parts.FirstOrDefault(p=>Get(p,"partType").ToString()==type && !(bool)Get(p,"IsBroken"));if(part!=null)return part;}
        return parts.First(p=>Get(p,"partType").ToString()=="Face");
    }

    private static Vector2 AutoPartPoint(Component part)
    {
        var collider=part.GetComponent<Collider2D>(); var b=collider.bounds;
        for(int x=1;x<10;x++) for(int y=1;y<10;y++) {
            Vector2 world=new Vector2(Mathf.Lerp(b.min.x,b.max.x,x/10f),Mathf.Lerp(b.min.y,b.max.y,y/10f));
            if(Physics2D.OverlapPoint(world)!=collider)continue;
            Vector2 screen=Camera.main.WorldToScreenPoint(world);
            var hits=new List<RaycastResult>(); EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=screen},hits);
            if(!hits.Any(h=>h.module is GraphicRaycaster))return screen;
        }
        Assert.Fail("No unobstructed pointer point for "+part.name);return Vector2.zero;
    }

    private static IEnumerator AutoSelect(Component battle, Component player, UnityEngine.Object skill)
    {
        var panel=(GameObject)Get(battle,"skillSelectionPanel");
        if(!panel.activeSelf){ AutoPointer(((GameObject)Get(battle,"skillButton")).GetComponent<Button>()); yield return null; }
        Component pager=Find("SkillPanelPager"), slot=null;
        for(int i=0;i<(int)Get(pager,"PageCount") && slot==null;i++) {
            slot=FindAll("SkillSlotUI").FirstOrDefault(s=>s.gameObject.activeInHierarchy && (UnityEngine.Object)Get(s,"Skill")==skill);
            if(slot==null){ if((int)Get(pager,"Page")+1<(int)Get(pager,"PageCount"))AutoPointer((Button)Get(pager,"nextButton"));else AutoPointer((Button)Get(pager,"previousButton")); yield return null; }
        }
        Assert.That(slot,Is.Not.Null,skill.name);ScrollToCard(pager,slot);yield return null;
        ClickLargeButton(slot.GetComponent<Button>(),true);
    }

    private static void AutoEncounterSeed(Component quest,string target)
    {
        var floors=(IList)Get(quest,"floors"); int floor=(int)Get(quest,"currentFloorIndex");
        if((bool)Get(quest,"isWaitingGrowthSelection"))floor++;
        if(floor>=floors.Count)return;
        var pool=((IEnumerable)Get(floors[floor],"enemyDatas")).Cast<UnityEngine.Object>().ToArray();
        int index=Array.FindIndex(pool,p=>p.name==target);Assert.That(index,Is.GreaterThanOrEqualTo(0),target+" must be an existing floor candidate");
        int seed=0;for(;seed<1000;seed++){UnityEngine.Random.InitState(seed);if(UnityEngine.Random.Range(0,pool.Length)==index)break;}
        UnityEngine.Random.InitState(seed);AutoWrite("seed",target,"floor="+(floor+1)+" seed="+seed);
    }

    private static void AutoPointer(Button button) => ExecuteEvents.Execute(button.gameObject,
        new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
    private static bool AutoAdvanceDialog()
    {
        var dialog=Find("DialogTextManager");
        if((bool)Get(dialog,"IsCompleteDisplayText") && (bool)Get(dialog,"HasPendingLines")){Call(dialog,"SetNextLine");return true;}return false;
    }
    private static IEnumerator AutoWait(Func<bool> predicate,string label)
    {
        float limit=Time.realtimeSinceStartup+20;
        while(!predicate() && Time.realtimeSinceStartup<limit){AutoAdvanceDialog();yield return null;}
        Assert.That(predicate(),Is.True,label);
    }
    private static void AutoWrite(string kind,string run,string value)
    {
        Directory.CreateDirectory(AutoFolder);
        File.AppendAllText(AutoFolder+"/trace.tsv",$"{Time.frameCount}\t{kind}\t{run}\t{value.Replace("\n"," / ").Replace("\r","")}\n");
    }
    private static void AutoFrame(string name)
    {
        int width=Camera.main.targetTexture.width,height=Camera.main.targetTexture.height;
        var image=new Texture2D(width,height,TextureFormat.RGB24,false);var active=RenderTexture.active;
        Camera.main.Render();RenderTexture.active=Camera.main.targetTexture;
        image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();
        File.WriteAllBytes(AutoFolder+"/"+name+".png",image.EncodeToPNG());
        RenderTexture.active=active;UnityEngine.Object.Destroy(image);
    }

    [UnityTest]
    public IEnumerator AutoplayFirstEncounterDisplaysFloorOne()
    {
        yield return AutoEnter("Mage");
        var quest=Find("QuestManager");var battle=Find("BattleManager");
        using(var view=new AutoView(1920,1080)){
            AutoEncounterSeed(quest,"00_Attacker");
            AutoPointer(((GameObject)Get(Get(quest,"stageUI"),"nextButton")).GetComponent<Button>());
            yield return AutoWait(()=>Get(battle,"enemy") is Component,"first encounter");
            string text=((Text)Get(Get(quest,"stageUI"),"stageText")).text;
            AutoFrame("first-encounter-stage");
            Assert.That(int.Parse(Regex.Match(text,"[0-9]+").Value),Is.EqualTo(1),"First dungeon encounter must display stage one");
        }
    }
    [UnityTest]
    public IEnumerator AutoplayHudRemainsInsideTheGameView()
    {
        yield return AutoEnter("Mage");
        var quest=Find("QuestManager");var battle=Find("BattleManager");
        AutoEncounterSeed(quest,"00_Attacker");
        AutoPointer(((GameObject)Get(Get(quest,"stageUI"),"nextButton")).GetComponent<Button>());
        yield return AutoWait(()=>Get(battle,"enemy") is Component && (bool)Call(battle,"CanReceivePlayerInput"),"battle HUD");
        var playerUi=Get(battle,"playerUI");var enemyUi=Get(battle,"enemyUI");
        Component[] elements={ (Component)Get(playerUi,"hpText"),(Component)Get(playerUi,"mpText"),
            (Component)Get(enemyUi,"hpSlider"),(Component)Get(enemyUi,"nameText"),
            (Component)Get(Get(quest,"stageUI"),"stageText"),(Component)Get(Find("DialogTextManager"),"uiText") };
        var outside=new List<string>();
        foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1366,768),new Vector2Int(800,600)}){
            using(var view=new AutoView(size.x,size.y)){
                yield return null;Canvas.ForceUpdateCanvases();
                foreach(var element in elements){
                    var r=ScreenRect(Camera.main,(RectTransform)element.transform);
                    var canvas=element.GetComponentInParent<Canvas>();var scaler=canvas.GetComponent<CanvasScaler>();
                    string row=$"{size} {element.name} bounds={r} canvas={canvas.name} enabled={scaler.enabled} mode={scaler.uiScaleMode} reference={scaler.referenceResolution}";
                    AutoWrite("hud", "before-or-after",row);
                    if(r.xMin < -1 || r.yMin < -1 || r.xMax>size.x+1 || r.yMax>size.y+1)outside.Add(row);
                }
                AutoFrame("hud-"+size.x+"x"+size.y);
            }
        }
        Assert.That(outside,Is.Empty,string.Join("\n",outside));
    }

    private sealed class AutoView:IDisposable
    {
        private Camera camera;private RenderTexture target,previous;
        private (Canvas canvas,RenderMode mode,Camera camera,float distance)[] states;
        public AutoView(int width,int height){camera=Camera.main;previous=camera.targetTexture;target=new RenderTexture(width,height,24);camera.targetTexture=target;
            states=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(c=>c.isRootCanvas).Select(c=>(c,c.renderMode,c.worldCamera,c.planeDistance)).ToArray();
            foreach(var s in states){s.canvas.renderMode=RenderMode.ScreenSpaceCamera;s.canvas.worldCamera=camera;s.canvas.planeDistance=5;}Canvas.ForceUpdateCanvases();}
        public void Dispose(){foreach(var s in states){if(s.canvas==null)continue;s.canvas.renderMode=s.mode;s.canvas.worldCamera=s.camera;s.canvas.planeDistance=s.distance;}if(camera!=null)camera.targetTexture=previous;target.Release();UnityEngine.Object.Destroy(target);}
    }
}
#endif
