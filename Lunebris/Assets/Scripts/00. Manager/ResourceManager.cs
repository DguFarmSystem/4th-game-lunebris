// Unity
using UnityEngine;

public class ResourceManager
{
    public T Load<T>(string _folder, string _fileName) where T : UnityEngine.Object
    {
        string path = $"{_folder}/{_fileName}";

        T resource = Resources.Load<T>(path);

        if (resource == null)
        {
            Debug.LogError("Error! can't find resource");
        }

        return resource;
    }
}