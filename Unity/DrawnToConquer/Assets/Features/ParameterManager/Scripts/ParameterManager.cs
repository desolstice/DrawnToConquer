using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;

public class ParameterManager : MonoBehaviour
{
    // Public static dictionaries for easy global access.
    public static Lazy<Dictionary<DoubleParameters, double>> DoubleParametersValues { get; private set; } = new Lazy<Dictionary<DoubleParameters, double>>(() => LoadEnumDictionary<DoubleParameters, double>("DoubleParameters", double.Parse));
    //public static Dictionary<StringParameters, string> StringParamValues { get; private set; }

    private void Awake()
    {
        //Force initialize dictionaries
        var _ = DoubleParametersValues.Value;

        //StringParamValues = LoadEnumDictionary<StringParameters, string>(
        //    Path.Combine(Application.dataPath, parametersDirectory, "StringParameters.csv"), s => s);
    }

    //Read lines of file from resources folder
    public static string[] ReadLines(string fileName)
    {
        TextAsset textAsset = Resources.Load<TextAsset>(fileName);
        if (textAsset == null)
        {
            Debug.LogError($"File not found: {fileName}");
            return new string[0];
        }
        return textAsset.text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
    }

    /// <summary>
    /// A generic method that loads key-value pairs from a CSV file into a dictionary.
    /// The keys are converted from strings into enum values of type TEnum.
    /// The values are parsed using the provided valueParser function.
    /// </summary>
    private static Dictionary<TEnum, TValue> LoadEnumDictionary<TEnum, TValue>(string fileName, Func<string, TValue> valueParser) where TEnum : struct, Enum
    {
        Dictionary<TEnum, TValue> dict = new Dictionary<TEnum, TValue>();

        string[] lines = ReadLines(fileName);
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            string[] parts = line.Split(',');
            if (parts.Length < 2)
            {
                Debug.LogWarning($"Invalid line in {fileName}: {line}");
                continue;
            }

            string keyString = parts[0].Trim();
            string valueString = parts[1].Trim();

            if (!Enum.TryParse(keyString, out TEnum enumKey))
            {
                Debug.LogWarning($"Failed to parse enum key {keyString} in {fileName}");
                continue;
            }

            try
            {
                TValue parsedValue = valueParser(valueString);
                dict[enumKey] = parsedValue;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to parse value '{valueString}' for key '{keyString}' in {fileName}: {e.Message}");
            }
        }

        return dict;
    }
}