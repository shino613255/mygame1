using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TitleManager : MonoBehaviour
{
    public void OnToTownButton() 
    {
        SoundManager.instance.PlayButtonSE(0); 
    }
}
