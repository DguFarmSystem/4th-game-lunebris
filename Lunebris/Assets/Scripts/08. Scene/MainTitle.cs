// Unity
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MainTitle : MonoBehaviour
{
    [Header("BGM")]
    [SerializeField] private string bgmTrackName;

    [Header("UI")]
    [SerializeField] private string nextSceneName;
    [SerializeField] private Button startButton;

    private void Start()
    {
        // Play BGM
        GameManager.Sound.BGMPlay(bgmTrackName);
        
        // Add Button Litsener
        startButton.onClick.AddListener(ConvertScene);
    }

    private void ConvertScene()
    {
        GameManager.Scene.ConvertScene(nextSceneName);
    }
}
