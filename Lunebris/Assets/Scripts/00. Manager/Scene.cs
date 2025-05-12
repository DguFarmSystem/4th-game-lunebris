// Unity
using UnityEngine.SceneManagement;

public class Scene
{
    public void ConvertScene(string _sceneName)
    {
        SceneManager.LoadScene(_sceneName);
    }

    public void ReLoad()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentScene);
    }

    public string GetCurrentScene()
    {
        return SceneManager.GetActiveScene().name;
    }
}
