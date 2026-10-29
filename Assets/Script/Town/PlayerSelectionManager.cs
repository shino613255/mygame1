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
        if (data == null)
        {
            return;
        }

        selectedPlayer = data;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
