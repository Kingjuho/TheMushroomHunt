using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// Application.persistentDataPath 아래 단일 JSON 파일을 읽고 쓰는 로컬 저장 서비스
/// 이번 버전은 단일 슬롯, 평문 JSON, 고정 스키마만 지원
/// </summary>
public sealed class LocalJsonSaveService
{
    private const string IntegerPattern = @"-?(?:0|[1-9]\d*)";
    private const string FloatPattern = @"-?(?:(?:0|[1-9]\d*)(?:\.\d+)?|\.\d+)(?:[eE][+-]?\d+)?";

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
    /// 필수 필드가 누락되었거나 타입이 잘못되었거나 JSON이 손상된 경우에는
    /// fail-closed로 false를 반환하고 scene/Inspector 기본값을 유지
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

            if (!TryValidateSaveJson(json, out string validationError))
            {
                Debug.LogWarning($"{nameof(LocalJsonSaveService)}: {validationError} Path: {_saveFilePath}");
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
    /// JsonUtility는 누락 필드를 타입 기본값으로 채워 넣을 수 있으므로,
    /// 역직렬화 전에 원본 JSON에서 필수 루트 필드와 playerPosition 하위 좌표를 직접 검사
    /// </summary>
    private bool TryValidateSaveJson(string json, out string validationError)
    {
        validationError = string.Empty;

        if (!TryReadIntField(json, "gold", out _))
        {
            validationError = "save file is missing or has invalid field 'gold'.";
            return false;
        }

        if (!TryReadIntField(json, "attackPower", out _))
        {
            validationError = "save file is missing or has invalid field 'attackPower'.";
            return false;
        }

        if (!TryReadFloatField(json, "attacksPerSecond", out _))
        {
            validationError = "save file is missing or has invalid field 'attacksPerSecond'.";
            return false;
        }

        if (!TryReadFloatField(json, "moveSpeed", out _))
        {
            validationError = "save file is missing or has invalid field 'moveSpeed'.";
            return false;
        }

        if (!TryExtractObjectBody(json, "playerPosition", out string playerPositionBody))
        {
            validationError = "save file is missing required object 'playerPosition'.";
            return false;
        }

        if (!TryReadFloatField(playerPositionBody, "x", out _)
            || !TryReadFloatField(playerPositionBody, "y", out _)
            || !TryReadFloatField(playerPositionBody, "z", out _))
        {
            validationError = "save file is missing or has invalid field 'playerPosition.x/y/z'.";
            return false;
        }

        return true;
    }

    private bool TryReadIntField(string json, string fieldName, out int value)
    {
        value = default;

        if (!TryExtractNumericField(json, fieldName, IntegerPattern, out string rawValue))
        {
            return false;
        }

        return int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    private bool TryReadFloatField(string json, string fieldName, out float value)
    {
        value = default;

        if (!TryExtractNumericField(json, fieldName, FloatPattern, out string rawValue))
        {
            return false;
        }

        if (!float.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
        {
            return false;
        }

        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    private bool TryExtractNumericField(string json, string fieldName, string numericPattern, out string rawValue)
    {
        rawValue = string.Empty;

        string pattern = "\"" + Regex.Escape(fieldName) + "\"\\s*:\\s*(?<value>" + numericPattern + ")(?=\\s*(,|\\}|\\]|$))";

        Match match = Regex.Match(json, pattern, RegexOptions.CultureInvariant);

        if (!match.Success)
        {
            return false;
        }

        rawValue = match.Groups["value"].Value;
        return !string.IsNullOrWhiteSpace(rawValue);
    }

    private bool TryExtractObjectBody(string json, string fieldName, out string objectBody)
    {
        objectBody = string.Empty;

        string pattern =
            "\"" + Regex.Escape(fieldName) + "\"\\s*:\\s*\\{(?<body>[\\s\\S]*?)\\}";

        Match match = Regex.Match(json, pattern, RegexOptions.CultureInvariant);

        if (!match.Success)
        {
            return false;
        }

        objectBody = match.Groups["body"].Value;
        return !string.IsNullOrWhiteSpace(objectBody);
    }
}
