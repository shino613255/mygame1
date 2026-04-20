using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSelectButton : MonoBehaviour
{
    public PlayerData playerData;

    public void OnClick()
    {
        PlayerSelectionManager.Instance.SelectPlayer(playerData);
    }
}