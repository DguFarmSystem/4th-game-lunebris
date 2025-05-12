// System
using System.IO;

// Unity
using UnityEngine;

public class DataManager
{
    // --- ���� ������ �����̸� ���� ("���ϴ� �̸�.json) --- //
    string gameDataFileName = "GameData.json";

    // --- ����� Ŭ���� ���� --- //
    public Data data = new Data();

    // --- ������ �ε� --- //
    public void LoadGameData()
    {
        string filePath = Application.persistentDataPath + "/" + gameDataFileName;

        // ����� ������ �̹� ���� ���
        if (File.Exists(filePath))
        {
            string FromJsonData = File.ReadAllText(filePath);
            data = JsonUtility.FromJson<Data>(FromJsonData);
        }
    }

    // --- ������ ���� --- //
    public void SaveGameData()
    {
        // Ŭ���� -> Json (true = ������ ���� �ۼ�)
        string ToJsonData = JsonUtility.ToJson(data, true);
        string filePath = Application.persistentDataPath + "/" + gameDataFileName;

        //�̹� ����� ���� �ִٸ� �����, ���ٸ� ���� ���� ����
        File.WriteAllText(filePath, ToJsonData);
    }

}
