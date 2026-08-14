using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TownManager : MonoBehaviour
{    
    private void Start()
    {
        DialogTextManager.instance.SetScenarios(new string[]
        {
            "äXÇ…ìûíÖÇµÇΩÅB",           
        });
    }
    public void OnToQuestButton() 
    {
        SoundManager.instance.PlayButtonSE(0); 
    }
}
