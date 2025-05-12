// System
using System.Collections;
using System.Collections.Generic;

// Unity
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private bool isPause = false;

    private static GameManager instance;
    public static GameManager Instance
    {
        get
        {
            return instance;
        }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private Scene scene = new Scene();
    private ResourceManager resouce = new ResourceManager();
    private SoundManager sound = new SoundManager();
    private DataManager data = new DataManager(); 

    public static Scene Scene { get { return Instance.scene; } }
    public static ResourceManager Resource { get { return Instance.resouce; } }
    public static SoundManager Sound { get { return Instance.sound; } }
    public static DataManager Data { get { return Instance.data; } }

    public void PauseGame()
    {
        isPause = !isPause;
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        isPause = !isPause;
        Time.timeScale = 1f;
    }

    
}
