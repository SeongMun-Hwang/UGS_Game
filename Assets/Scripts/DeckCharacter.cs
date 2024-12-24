using System;
using TMPro;
using UnityEngine;

public class DeckCharacter : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI characterNameTmp;
    public Action<string> onDelete;
    string characterName;

    public void SetCharacterName(string name)
    {
        characterName = name;
        characterNameTmp.text = name;
    }

    public void DeleteThis()
    {
        if(onDelete != null)
        {
            onDelete(characterName);
            Destroy(gameObject);
        }
    }
}
