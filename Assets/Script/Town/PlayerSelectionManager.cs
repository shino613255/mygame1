using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSelectionManager : MonoBehaviour
{
    public static PlayerSelectionManager Instance;

    public PlayerData selectedPlayer;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SelectPlayer(PlayerData data)
    {
        if (data == null) return;
        selectedPlayer = data;
        Debug.Log("選択されたプレイヤー：" + data.playerName);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
