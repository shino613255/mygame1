using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;

public class ControlGuideUI : MonoBehaviour
{
    [SerializeField] private GameObject controlPanel;
    private void Start()
    {
        controlPanel.SetActive(false);
    }
    public void ToggleControlPanel()
    {
        bool isOpen = !controlPanel.activeSelf;
        controlPanel.SetActive(isOpen);
    }
}