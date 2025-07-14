using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using Newtonsoft.Json.Linq;

#if UNITY_EDITOR
public class EventDownloader : EditorWindow
{
    private JSONConfig config;
    [MenuItem("Tools/Download Script")]
    public static void ShowWindow()
    {
        GetWindow<EventDownloader>("JSON Downloader");
    }

    private void OnEnable()
    {
        minSize = new Vector2(400, 300);
    }

    private void OnGUI()
    {
        GUILayout.Label("Google Sheets Downloader Settings", EditorStyles.boldLabel);

        var newConfig = (JSONConfig)EditorGUILayout.ObjectField("Config File", config, typeof(JSONConfig), false);
        if (newConfig != config)
        {
            config = newConfig;
        }
        if (GUILayout.Button("Download JSON Files"))
        {
            if (config != null)
            {
                DownloadJSONFiles();
            }
            else
            {
                Debug.LogError("Please assign a JSONConfig file.");
            }
        }
    }

    private void DownloadJSONFiles()
    {
        string urlTemplate = "https://script.google.com/macros/s/{0}/exec";
        Debug.Log("Downloading JSON Files!");

        foreach (var configEntry in config.dialogueConfigs)
        {
            string url = string.Format(urlTemplate, configEntry.sheetID, name);
            string jsonContent = DownloadFile(url);

            if (!string.IsNullOrEmpty(jsonContent))
            {
                string folderPath = "Assets/Resources/Script";
                if (Directory.Exists(folderPath))
                    Directory.Delete(folderPath, true);
                
                Directory.CreateDirectory(folderPath);

                SaveSheetJSON(jsonContent, folderPath); 
            }
            else
            {
                Debug.LogError($"Failed to download the dialog file for {name}.");
            }
        }
    }

    private void SaveSheetJSON(string JSON, string folderPath)
    {
        Debug.Log(JSON);
        Debug.Log("Path: " + folderPath);
        try
        {
            JObject allSheets = JObject.Parse(JSON);
            foreach (var sheet in allSheets)
            {
                string filePath = Path.Combine(folderPath, sheet.Key + ".json");
                File.WriteAllText(filePath, sheet.Value.ToString());
                AssetDatabase.Refresh();
                Debug.Log($"Dialogue for {sheet.Key} saved to {filePath} successfully.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Error saving sheet JSON: " + ex.Message);
        }
    }

    private string DownloadFile(string url)
    {
        try
        {
            using (System.Net.WebClient client = new System.Net.WebClient())
            {
                return client.DownloadString(url);
            }
        }
        catch (System.Net.WebException ex)
        {
            Debug.LogError("Error downloading dialogue file: " + ex.Message);
            return null;
        }
    }
}
#endif