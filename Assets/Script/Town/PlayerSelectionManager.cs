using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSelectionManager : MonoBehaviour
{
    public static PlayerSelectionManager Instance;

    public PlayerData selectedPlayer;

    void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SelectPlayer(PlayerData data)
    {
        selectedPlayer = data;
        Debug.Log("選択されたプレイヤー：" + data.playerName);
    }
}