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
        //SignInAnonymouslyAsync() - 계정을 따로 안 만들고 로그인. 흔히 아는 게스트 로그인
        await AuthenticationService.Instance.SignInAnonymouslyAsync();

        if (AuthenticationService.Instance.IsSignedIn) //로그인 됐으면
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
        var result = await EconomyService.Instance.PlayerBalances.GetBalancesAsync(); //정보 동기화

        foreach (var balance in result.Balances)
        {
            //Debug.Log(balance.CurrencyId + " : " + balance.Balance);
        }

        long ssal = result.Balances.Single(balance => balance.CurrencyId == "GOLD").Balance;
        goldText.text = ssal.ToString(); //GOLD면 텍스트 입력

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
    /*버튼 액션 : 돈 추가*/
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
            var itemResult = await EconomyService.Instance.PlayerInventory.GetInventoryAsync(options); //인벤토리 조회. 조회 시 아이템을 20개 단위로 가져옴
            items.AddRange(itemResult.PlayersInventoryItems);

            while (itemResult.HasNext) //가져온 20개보다 아이템이 더 있으면
            {
                itemResult = await itemResult.GetNextAsync(); //20개 다음 데이터를 가져와서
                items.AddRange(itemResult.PlayersInventoryItems); //또 추가
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
            //플레이어와 관련된 데이터 로드. Dictionary방식으로 저장함
            var data = await CloudSaveService.Instance.Data.Player.LoadAllAsync();

            //데이터 없으면 생성
            if (data.Count == 0)
            {
                CloudSave();
            }
            //있으면 로드
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