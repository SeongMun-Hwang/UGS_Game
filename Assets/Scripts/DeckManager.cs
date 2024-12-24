using Newtonsoft.Json;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.CloudCode;
using Unity.Services.CloudCode.GeneratedBindings;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DeckManager : MonoBehaviour
{
    [SerializeField] Transform scrollViewContentView;
    [SerializeField] CharacterPrefab[] prefabs;
    [SerializeField] GameObject selectCharacterButtonPrefab;

    List<CharacterOwned> myCharacters;
    List<string> charactersInDeck;

    [SerializeField] Transform deckParent;
    [SerializeField] GameObject deckPrefab;

    async void Start()
    {
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        await FetchCharacters();
        UpdateCharacterButtons();
        LoadDeck();
    }
    void LoadDeck()
    {
        string json = PlayerPrefs.GetString("deck");
        charactersInDeck = JsonConvert.DeserializeObject<List<string>>(json);
        if(charactersInDeck == null)
        {
            charactersInDeck = new List<string>();
        }
        else
        {
            foreach(string name in charactersInDeck)
            {
                AddCharacterToView(name);
            }
        }
    }
    void SaveDeck()
    {
        PlayerPrefs.SetString("deck",JsonConvert.SerializeObject(charactersInDeck));
        PlayerPrefs.Save();
    }
    void UpdateCharacterButtons()
    {
        foreach(CharacterOwned character in myCharacters)
        {
            var prefab = prefabs.FirstOrDefault(c => c.characterName == character.Name).prefab;
            if (prefab != null)
            {
                GameObject go = Instantiate(selectCharacterButtonPrefab, scrollViewContentView);
                go.GetComponent<Button>().onClick.AddListener(() =>
                {
                    SelectCharacter(character.Name);
                });
                Instantiate(prefab, go.transform.GetChild(0).transform);
            }
        }
    }
    void SelectCharacter(string name)
    {
        if(charactersInDeck.Count<3&& !charactersInDeck.Contains(name))
        {
            charactersInDeck.Add(name);
            AddCharacterToView(name);
            SaveDeck();
        }
    }
    void AddCharacterToView(string name)
    {
        GameObject go = Instantiate(deckPrefab, deckParent);
        go.GetComponent<DeckCharacter>().SetCharacterName(name);
        go.GetComponent<DeckCharacter>().onDelete = DeleteCharacterFromDeck;
    }
    void DeleteCharacterFromDeck(string name)
    {
        charactersInDeck.Remove(name);
        SaveDeck();
    }
    async Task FetchCharacters()
    {
        try
        {
            var data=await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string>{"Character" });
            if (data.Count == 0)
            {
                
            }
            else
            {
                string json = data["Character"].Value.GetAsString();
                myCharacters=JsonConvert.DeserializeObject<List<CharacterOwned>>(json);

                Debug.Log("Character owned : " + json);
            }
        }
        catch (CloudCodeException e)
        {
            Debug.LogException(e);
        }
    }
    public async void BackPressed()
    {
        await SaveDeckToCloud();
        SceneManager.LoadScene("MainScene");
    }
    async Task SaveDeckToCloud()
    {
        try
        {
            var saveItem = new Dictionary<string, object>
            {
                {"Deck",charactersInDeck }
            };
            await CloudSaveService.Instance.Data.Player.SaveAsync(saveItem);

            var module = new ProjectBindings(CloudCodeService.Instance);
            await module.RegisterDeck();
        }
        catch(CloudCodeException e) 
        {
            Debug.LogException(e);
        }
    }
}
