using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.CloudCode;
using Unity.Services.CloudCode.GeneratedBindings;
using Unity.Services.Core;
using Unity.Services.Leaderboards;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.SocialPlatforms.Impl;

public class BattleManager : MonoBehaviour
{
    struct Oponent
    {
        public int rank;
        public string playerId;
        public string playerName;
    }
    [SerializeField] Transform scrollViewContent;
    [SerializeField] GameObject batlleButtonPrefab;
    [SerializeField] TextMeshProUGUI rankTmp;

    List<Oponent> opponents;

    async void Start()
    {
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        opponents = new List<Oponent>();
        await FetchOpponent();
        UpdateOpponent();
    }
    async Task FetchOpponent()
    {
        var result = await LeaderboardsService.Instance.GetScoresAsync("BattleRank");
        foreach (var item in result.Results)
        {
            int rank = item.Rank;
            string playerId = item.PlayerId;
            string playerName = item.PlayerName;

            if (playerId == AuthenticationService.Instance.PlayerId)
            {
                rankTmp.text = "#" + (rank + 1);
            }
            else
            {
                opponents.Add(new Oponent
                {
                    rank = rank,
                    playerId = playerId,
                    playerName = playerName,
                });
            }
        }
    }
    void UpdateOpponent()
    {
        foreach (var opponent in opponents)
        {
            GameObject go = Instantiate(batlleButtonPrefab, scrollViewContent);
            go.GetComponent<BattleButton>().onBattleStart = BattleStart;
            go.GetComponent<BattleButton>().SetPlayerId(opponent.rank + 1, opponent.playerId, opponent.playerName);
        }
    }
    public async void BattleStart(string opponentId)
    {
        var module = new ProjectBindings(CloudCodeService.Instance);
        await module.Battle(opponentId);   
    }
    public void BackPressed()
    {
        SceneManager.LoadScene("MainScene");
    }
}
