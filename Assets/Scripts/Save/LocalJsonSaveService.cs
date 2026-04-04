using System;
using System.IO;
using UnityEngine;

public sealed class LocalJsonSaveService
{
    private readonly string _saveFilePath;

    public string SaveFilePath => _saveFilePath;

    public LocalJsonSaveService(string fileName)
    {
        string safeFileName = string.IsNullOrWhiteSpace(fileName)
            ? "save-slot.json"
            : fileName;

        _saveFilePath = Path.Combine(Application.persistentDataPath, safeFileName);
    }

    public void Save(SaveData data)
    {
        string directoryPath = Path.GetDirectoryName(_saveFilePath);

        if (!string.IsNullOrWhiteSpace(directoryPath) && !Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(_saveFilePath, json);
    }

    public bool TryLoad(out SaveData data)
    {
        data = null;

        if (!File.Exists(_saveFilePath))
        {
            return false;
        }

        try
        {
            string json = File.ReadAllText(_saveFilePath);

            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogWarning($"{nameof(LocalJsonSaveService)}: save file is empty. Path: {_saveFilePath}");
                return false;
            }

            if (!HasRequiredFields(json))
            {
                Debug.LogWarning($"{nameof(LocalJsonSaveService)}: save file is missing required fields. Path: {_saveFilePath}");
                return false;
            }

            SaveData loadedData = JsonUtility.FromJson<SaveData>(json);

            if (loadedData == null)
            {
                Debug.LogWarning($"{nameof(LocalJsonSaveService)}: failed to deserialize save file. Path: {_saveFilePath}");
                return false;
            }

            data = loadedData;
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"{nameof(LocalJsonSaveService)}: failed to read save file. Path: {_saveFilePath}\n{exception}");
            return false;
        }
    }

    private bool HasRequiredFields(string json)
    {
        return json.Contains("\"gold\"")
            && json.Contains("\"playerPosition\"")
            && json.Contains("\"attackPower\"")
            && json.Contains("\"attacksPerSecond\"");
    }
}
