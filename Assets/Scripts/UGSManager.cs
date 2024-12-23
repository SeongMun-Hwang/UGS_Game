using TMPro;
using UnityEngine;
using Unity.Services;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Economy;
using System.Linq;
using NUnit.Framework;
using Unity.Services.Economy.Model;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudSave;
using Unity.Services.Leaderboards;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.SocialPlatforms;
using Newtonsoft.Json;
using Unity.Services.CloudCode;
using Unity.Services.CloudCode.GeneratedBindings;
using UnityEngine.SceneManagement;

public class UGSManager : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI userIdText;
    [SerializeField]
    TextMeshProUGUI goldText;
    [SerializeField]
    TextMeshProUGUI ticketText;
    [SerializeField]
    TextMeshProUGUI playerDataText;

    List<PlayersInventoryItem> items = new List<PlayersInventoryItem>();
    LevelData levelData;
    async void Start()
    {
        await UnityServices.InitializeAsync();
        if (AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        //SignInAnonymouslyAsync() - ������ ���� �� ����� �α���. ���� �ƴ� �Խ�Ʈ �α���
        await AuthenticationService.Instance.SignInAnonymouslyAsync();

        if (AuthenticationService.Instance.IsSignedIn) //�α��� ������
        {
            userIdText.text = "Player ID : " + AuthenticationService.Instance.PlayerId;

            await AuthenticationService.Instance.UpdatePlayerNameAsync("Delirium");

            UpdatePlayerInfo();
            LoadFromCloud();
            CallCloudFunction();
        }
        else
        {
            Debug.Log("Login Failed");
        }
    }
    async void UpdatePlayerInfo()
    {
        var result = await EconomyService.Instance.PlayerBalances.GetBalancesAsync(); //���� ����ȭ

        foreach (var balance in result.Balances)
        {
            //Debug.Log(balance.CurrencyId + " : " + balance.Balance);
        }

        long ssal = result.Balances.Single(balance => balance.CurrencyId == "GOLD").Balance;
        goldText.text = ssal.ToString(); //GOLD�� �ؽ�Ʈ �Է�

        AddSCore(ssal);

        await FetchAllInventoryItems();

        int counter = 0;
        foreach (var item in items)
        {
            if (item.InventoryItemId == "GACHATICKET")
            {
                counter++;
            }
        }
        ticketText.text = counter.ToString();
    }
    public void GetGold()
    {
        AddGold(100);
    }
    /*��ư �׼� : �� �߰�*/
    async void AddGold(int amount)
    {
        var result = await EconomyService.Instance.PlayerBalances.GetBalancesAsync();
        long currentGold = result.Balances.Single(balance => balance.CurrencyId == "GOLD").Balance;

        await EconomyService.Instance.PlayerBalances.SetBalanceAsync("GOLD", currentGold + amount);

        UpdatePlayerInfo();
    }
    public async void PurchaseTicket()
    {
        await EconomyService.Instance.Purchases.MakeVirtualPurchaseAsync("SSALMUK");

        UpdatePlayerInfo();
    }
    async Task FetchAllInventoryItems()
    {
        items.Clear();
        GetInventoryOptions options = new GetInventoryOptions
        {
            ItemsPerFetch = 20,
        };

        try
        {
            var itemResult = await EconomyService.Instance.PlayerInventory.GetInventoryAsync(options); //�κ��丮 ��ȸ. ��ȸ �� �������� 20�� ������ ������
            items.AddRange(itemResult.PlayersInventoryItems);

            while (itemResult.HasNext) //������ 20������ �������� �� ������
            {
                itemResult = await itemResult.GetNextAsync(); //20�� ���� �����͸� �����ͼ�
                items.AddRange(itemResult.PlayersInventoryItems); //�� �߰�
            }
        }
        catch (RequestFailedException e)
        {
            Debug.Log(e);
        }
    }
    async void LoadFromCloud()
    {
        try
        {
            //�÷��̾�� ���õ� ������ �ε�. Dictionary������� ������
            var data = await CloudSaveService.Instance.Data.Player.LoadAllAsync();

            //������ ������ ����
            if (data.Count == 0)
            {
                CloudSave();
            }
            //������ �ε�
            else
            {
                data.TryGetValue("Player", out var playerDataJson);

                levelData = playerDataJson.Value.GetAs<LevelData>();
                UpdateLevelData();
            }
        }
        catch
        {

        }
    }
    async void CloudSave()
    {
        if (levelData == null)
        {
            levelData = new LevelData
            {
                playerLevel = 1,
                exp = 0,
            };
        }
        var data = new Dictionary<string, object>
        {
            { "Player",levelData }
        };
        try
        {
            await CloudSaveService.Instance.Data.Player.SaveAsync(data);
        }
        catch (CloudSaveException e)
        {
            Debug.Log(e);
        }
    }
    void UpdateLevelData()
    {
        playerDataText.text = "Player Level : " + levelData.playerLevel + "\nEXP : " + levelData.exp;
    }
    public async void AddSCore(long ssal)
    {
        var scoreResponse = await LeaderboardsService.Instance.AddPlayerScoreAsync("SSALMONKEY", ssal);
        GetScores();
    }
    public async void GetScores()
    {
        var scoreResponse = await LeaderboardsService.Instance.GetScoresAsync("SSALMONKEY");
        Debug.Log(JsonConvert.SerializeObject(scoreResponse));
    }
    async void CallCloudFunction()
    {
        try
        {
            //var module = new ProjectBindings(CloudCodeService.Instance);
            //var result = await module.SayHello("World"); 
            var module = new ProjectBindings(CloudCodeService.Instance);
            var result = await module.GetGacha();

            Debug.Log("Cloud code result : " + result);
        }
        catch (CloudCodeException e)
        {
            Debug.Log(e);
        }
    }
    public void MoveToGachaScene()
    {
        SceneManager.LoadScene("GachaScene");
    }
}