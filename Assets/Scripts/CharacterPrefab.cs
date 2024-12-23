using UnityEngine;

[CreateAssetMenu(fileName = "CharacterPrefab", menuName = "Scriptable Objects/CharacterPrefab")]
public class CharacterPrefab : ScriptableObject
{
    public string characterName;
    public GameObject prefab;
}
