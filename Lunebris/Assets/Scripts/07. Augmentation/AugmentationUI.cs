// Unity
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// System
using System.Collections.Generic;

[DisallowMultipleComponent]
public class AugmentationUI : MonoBehaviour
{
    [SerializeField] private GameObject augmentationUI;

    [SerializeField] private Button[] augmentationSelectButtons;
    [SerializeField] private TextMeshProUGUI augmentationName;

    private void Start()
    {
        // Get Select Buttons
        augmentationSelectButtons = augmentationUI.transform.GetComponentsInChildren<Button>();

        // Add Listener (Close Augementation UI)
        foreach (Button button in augmentationSelectButtons) button.onClick.AddListener(CloseAugmentationUI);
    }

    private void Update()
    {
        // Test Code
        if (Input.GetKeyDown(KeyCode.V)) OpenAugmentationUI();
    }

    public void OpenAugmentationUI()
    {
        GameManager.Instance.PauseGame();   // Pause
        augmentationUI.SetActive(true);
    }

    public void CloseAugmentationUI()
    {
        GameManager.Instance.ResumeGame();  // Resume
        augmentationUI.SetActive(false);
    }
}
