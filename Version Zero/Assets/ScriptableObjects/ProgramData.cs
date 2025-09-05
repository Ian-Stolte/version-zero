using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ProgramData", menuName = "ScriptableObjects/Program Data")]
public class ProgramData : ScriptableObject
{
    public List<GameObject> baseBlocks;
    public List<GameObject> effectBlocks;
    public List<GameObject> modBlocks;
}