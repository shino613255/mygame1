using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TownManager : MonoBehaviour
{    
    private void Start()
    {
        DialogTextManager.instance.SetScenarios(new string[]
        {
            "ŠX‚É“’…‚µ‚½B",           
        });
    }
    public void OnToQuestButton() // ƒ{ƒ^ƒ“‚ª‰Ÿ‚³‚ê‚½‚Æ‚«
    {
        SoundManager.instance.PlayButtonSE(0); // SEÄ¶
    }
}
