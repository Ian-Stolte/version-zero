using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "JSONConfig", menuName = "ScriptableObjects/JSON Downloader Config")]
public class JSONConfig : ScriptableObject
{
    [System.Serializable]
    public class DialogueConfig
    {
        public string name;
        [TextArea(3, 5)]
        public string sheetID;
    }

    public List<DialogueConfig> dialogueConfigs;
}