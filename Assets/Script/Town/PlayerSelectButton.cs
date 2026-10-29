using UnityEngine;

public class PlayerSelectButton : MonoBehaviour
{
    public PlayerData playerData;

    public void OnClick()
    {
        if (PlayerSelectionManager.Instance != null)
        {
            PlayerSelectionManager.Instance.SelectPlayer(playerData);
        }
    }
}
