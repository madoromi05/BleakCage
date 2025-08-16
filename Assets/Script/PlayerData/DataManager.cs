using UnityEngine;
using System.IO;

/// <summary>
/// <see cref="PlayerProfile/> の保存と読み込みを管理するクラス
/// </summary>
public static class DataManager
{
    private static string GetFilePath(string fileName)
    {
        return Path.Combine(Application.persistentDataPath, fileName);
    }

    public static void SaveData<T>(T data, string fileName)
    {
        string filePath = GetFilePath(fileName);
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(filePath, json);
        Debug.Log($"データが {filePath} に保存されました");
    }

    public static T LoadData<T>(string fileName) where T : new()
    {
        string filePath = GetFilePath(fileName);
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            T data = JsonUtility.FromJson<T>(json);
            Debug.Log($"データが {filePath} から読み込まれました");
            return data;
        }
        else
        {
            Debug.LogWarning($"ファイルが見つかりません: {filePath}。新しいデータオブジェクトを返します。");
            return new T();
        }
    }
}