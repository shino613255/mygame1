using UnityEngine;

public class ControlGuideUI : MonoBehaviour
{
    [SerializeField] private GameObject controlPanel;
    [SerializeField] private GameObject controlCloseButton;

    private void Start()
    {
        controlPanel.SetActive(false);
        controlCloseButton.SetActive(false);
    }
    public void ToggleControlPanel()
    {
        bool isOpen =
            !controlPanel.activeSelf;

        controlPanel.SetActive(isOpen);
        controlCloseButton.SetActive(isOpen);
    }

    public void CloseControlPanel()
    {
        controlPanel.SetActive(false);
        controlCloseButton.SetActive(false);
    }
}