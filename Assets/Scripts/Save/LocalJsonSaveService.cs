using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Application.persistentDataPath 아래 단일 JSON 파일을 읽고 쓰는 로컬 저장 서비스
/// </summary>
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

    /// <summary>
    /// 현재 슬롯 내용을 JSON 파일로 덮어씀
    /// 저장 경로가 아직 없으면 폴더를 먼저 만들고, 실패 시 false 반환
    /// </summary>
    public bool TrySave(SaveData data)
    {
        if (data == null)
        {
            Debug.LogWarning($"{nameof(LocalJsonSaveService)}: save data is null.");
            return false;
        }

        try
        {
            string directoryPath = Path.GetDirectoryName(_saveFilePath);

            if (!string.IsNullOrWhiteSpace(directoryPath) && !Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(_saveFilePath, json);
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"{nameof(LocalJsonSaveService)}: failed to write save file. Path: {_saveFilePath}\n{exception}");
            return false;
        }
    }

    /// <summary>
    /// 저장 파일이 존재하면 읽어서 SaveData로 복원
    /// 필수 필드가 누락되었거나 JSON이 손상된 경우에는 경고만 남기고 false 반환
    /// </summary>
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

            if (!HasRequiredCoreFields(json))
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

    /// <summary>
    /// 핵심 필드 유효성 체크
    /// 필수 필드가 하나라도 빠져 있으면 손상된 세이브로 판단
    /// </summary>
    private bool HasRequiredCoreFields(string json)
    {
        return json.Contains("\"gold\"")
            && json.Contains("\"playerPosition\"")
            && json.Contains("\"attackPower\"")
            && json.Contains("\"attacksPerSecond\"")
            && json.Contains("\"moveSpeed\"");
    }
}
