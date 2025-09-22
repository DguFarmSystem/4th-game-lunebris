// Unity
using UnityEngine;

[DisallowMultipleComponent]
public class MainScene : MonoBehaviour
{
    [SerializeField] private string mainBGMTrackName = "Main_Scene_BGM";

    private void Start()
    {
        GameManager.Sound.BGMPlay(mainBGMTrackName);
    }


}
