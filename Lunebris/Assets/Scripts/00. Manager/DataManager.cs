// System
using System.IO;

// Unity
using UnityEngine;

public class DataManager
{
    string gameDataFileName = "GameData.json";

    public Data data = new Data();

    public void LoadGameData()
    {
        string filePath = Application.persistentDataPath + "/" + gameDataFileName;

        if (File.Exists(filePath))
        {
            string FromJsonData = File.ReadAllText(filePath);
            data = JsonUtility.FromJson<Data>(FromJsonData);
        }
    }

    public void SaveGameData()
    {
        string ToJsonData = JsonUtility.ToJson(data, true);
        string filePath = Application.persistentDataPath + "/" + gameDataFileName;

        File.WriteAllText(filePath, ToJsonData);
    }

}
