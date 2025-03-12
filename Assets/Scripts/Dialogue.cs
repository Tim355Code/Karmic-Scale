using UnityEngine;

// Basic dialogue container SO
[CreateAssetMenu(fileName = "New Dialogue", menuName = "ScriptableObjects/Dialogue", order = 1)]
public class Dialogue : ScriptableObject
{
    public DialogueData[] Data;
}

[System.Serializable]
public class DialogueData
{
    public string Name;
    [TextArea]
    public string Text;
}
