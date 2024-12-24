using HelloWorld;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;
using Unity.Services.CloudSave.Model;

namespace Project
{
    internal class BattleSimulator
    {
        ILogger<MyModule> logger;
        IGameApiClient apiClient;

        public BattleSimulator(ILogger<MyModule> logger, IGameApiClient apiClient)
        {
            this.apiClient = apiClient;
            this.logger = logger;
        }
        public async Task RegisterPlayerDeck(IExecutionContext context)
        {
            Guid porjectId = Guid.Parse(context.ProjectId);
            var result = await apiClient.Leaderboards.GetLeaderboardScoresAsync(
                context,
                context.ServiceToken,
                porjectId,
                "BattleRank");

            var entry = result.Data.Results.FirstOrDefault(data => data.PlayerId == context.PlayerId);
            if (entry == null)
            {
                await apiClient.Leaderboards.AddLeaderboardPlayerScoreAsync(
                    context,
                    context.ServiceToken,
                    porjectId,
                    "BattleRank",
                    context.PlayerId,
                    new Unity.Services.Leaderboards.Model.LeaderboardScore(result.Data.Total + 1)
                    );
            }
            else
            {

            }
        }
        //1이면 공격자 승 -1이면 수비자 승, 0이면 드로우
        public async Task<int> Battle(IExecutionContext context, string opponentPlayer)
        {
            logger.LogDebug("Battle called");
            Guid porjectId = Guid.Parse(context.ProjectId);

            int result = 1;

            if (result == 1) //when attacker win
            {
                var result1 = await apiClient.Leaderboards.GetLeaderboardPlayerScoreAsync(
                    context,
                    context.ServiceToken,
                    porjectId,
                    "BattleRank",
                    context.PlayerId
                    );
                var result2 = await apiClient.Leaderboards.GetLeaderboardPlayerScoreAsync(
                    context,
                    context.ServiceToken,
                    porjectId,
                    "BattleRank",
                    opponentPlayer
                    );
                int score1 = (int)result1.Data.Score;
                int score2 = (int)result2.Data.Score;

                if (score1 > score2)
                {
                    await apiClient.Leaderboards.AddLeaderboardPlayerScoreAsync(
                    context,
                    context.ServiceToken,
                    porjectId,
                    "BattleRank",
                    context.PlayerId,
                    new Unity.Services.Leaderboards.Model.LeaderboardScore(score2)
                    );
                    await apiClient.Leaderboards.AddLeaderboardPlayerScoreAsync(
                        context,
                        context.ServiceToken,
                        porjectId,
                        "BattleRank",
                        opponentPlayer,
                        new Unity.Services.Leaderboards.Model.LeaderboardScore(score1)
                    );
                }
            }
            return result;
        }
    }
}
