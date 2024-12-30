using HelloWorld;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;
using Unity.Services.CloudSave.Model;
using Unity.Services.Leaderboards.Model;

namespace Project
{
    internal class BattleSimulator
    {
        ILogger<MyModule> logger;
        IGameApiClient apiClient;

        internal class Character
        {
            public string name;
            public int level;
        }

        internal class BattleCharacter
        {
            public string name;
            public float health;
            public float attack;
            public float defend;
            public int repeat;

            public BattleCharacter(CharacterStats stat, int level, float levelUpFactor)
            {
                float pow = MathF.Pow(levelUpFactor, level - 1);

                name = stat.name;
                health = stat.health * pow;
                attack = stat.attack * pow;
                defend = stat.defend * pow;
                repeat = stat.repeat;
            }
        }

        internal class CharacterStats
        {
            public string name;
            public float health;
            public float attack;
            public float defend;
            public int repeat;
            public string rarity;
        }

        public BattleSimulator(ILogger<MyModule> logger, IGameApiClient apiClient)
        {
            this.apiClient = apiClient;
            this.logger = logger;
        }

        public async Task RegisterPlayerDeck(IExecutionContext context)
        {
            Guid projectId = Guid.Parse(context.ProjectId);

            var result = await apiClient.Leaderboards.GetLeaderboardScoresAsync(
                context,
                context.ServiceToken,
                projectId, "BattleRank");

            LeaderboardEntry entry = result.Data.Results.FirstOrDefault(data => data.PlayerId == context.PlayerId);
            if (entry == null)
            {
                await apiClient.Leaderboards.AddLeaderboardPlayerScoreAsync(
                    context,
                    context.ServiceToken,
                    projectId,
                    "BattleRank",
                    context.PlayerId,
                    new LeaderboardScore(result.Data.Total + 1)
                    );
            }
        }

        // 1이면 공격자 승, -1이면 수비자 승, 0이면 무승부
        public async Task<int> Battle(IExecutionContext context, string opponentPlayer)
        {
            logger.LogDebug("Battle Started");
            Guid projectId = Guid.Parse(context.ProjectId);

            // 전투에 필요한 데이터를 가져와서 전투할 수 있게 가공

            // 두 유저가 가지고 있는 캐릭터, 각 유저의 덱을 가져옴
            var deckQuery1 = await apiClient.CloudSaveData.GetItemsAsync(
                context, context.ServiceToken, context.ProjectId, context.PlayerId,
                new List<string> { "Character", "Deck" }
                );
            var deckQuery2 = await apiClient.CloudSaveData.GetItemsAsync(
                context, context.ServiceToken, context.ProjectId, opponentPlayer,
                new List<string> { "Character", "Deck" }
                );

            var deck1 = JsonConvert.DeserializeObject<List<string>>(
                deckQuery1.Data.Results.FirstOrDefault(item => item.Key == "Deck").Value.ToString());
            var deck2 = JsonConvert.DeserializeObject<List<string>>(
                deckQuery2.Data.Results.FirstOrDefault(item => item.Key == "Deck").Value.ToString());

            var char1 = JsonConvert.DeserializeObject<List<Character>>(
                deckQuery1.Data.Results.FirstOrDefault(item => item.Key == "Character").Value.ToString());
            var char2 = JsonConvert.DeserializeObject<List<Character>>(
                deckQuery2.Data.Results.FirstOrDefault(item => item.Key == "Character").Value.ToString());

            //logger.LogDebug(JsonConvert.SerializeObject(deck1));
            //logger.LogDebug(JsonConvert.SerializeObject(deck2));
            //logger.LogDebug(JsonConvert.SerializeObject(char1));
            //logger.LogDebug(JsonConvert.SerializeObject(char2));

            var statResult = await apiClient.RemoteConfigSettings.AssignSettingsGetAsync(
                context, context.AccessToken, context.ProjectId, context.EnvironmentId, null,
                new List<string> { "CharacterStats", "LevelUpFactor" });
            List<CharacterStats> stats = JsonConvert.DeserializeObject<List<CharacterStats>>(
                statResult.Data.Configs.Settings["CharacterStats"].ToString());
            float levelUpFactor = Convert.ToSingle(statResult.Data.Configs.Settings["LevelUpFactor"]);

            List<BattleCharacter> list1 = new List<BattleCharacter>();
            List<BattleCharacter> list2 = new List<BattleCharacter>();

            foreach (var c in deck1)
            {
                BattleCharacter battleCharacter = new BattleCharacter(
                    stats.FirstOrDefault(ch => ch.name == c),
                    char1.FirstOrDefault(ch => ch.name == c).level,
                    levelUpFactor);
                list1.Add(battleCharacter);
            }
            foreach (var c in deck2)
            {
                BattleCharacter battleCharacter = new BattleCharacter(
                    stats.FirstOrDefault(ch => ch.name == c),
                    char2.FirstOrDefault(ch => ch.name == c).level,
                    levelUpFactor);
                list2.Add(battleCharacter);
            }

            // 전투

            int battleResult = RealBattle(list1, list2);

            // 전투 결과에 따라 랭킹 변경
            if (battleResult == 1)      // 공격자가 이겼을때
            {
                var result1 = await apiClient.Leaderboards.GetLeaderboardPlayerScoreAsync(
                    context, context.ServiceToken, projectId, "BattleRank", context.PlayerId);
                var result2 = await apiClient.Leaderboards.GetLeaderboardPlayerScoreAsync(
                    context, context.ServiceToken, projectId, "BattleRank", opponentPlayer);

                int score1 = (int)result1.Data.Score;
                int score2 = (int)result2.Data.Score;

                if (score1 > score2)
                {
                    await apiClient.Leaderboards.AddLeaderboardPlayerScoreAsync(
                        context, context.ServiceToken, projectId, "BattleRank", context.PlayerId,
                        new LeaderboardScore(score2));
                    await apiClient.Leaderboards.AddLeaderboardPlayerScoreAsync(
                        context, context.ServiceToken, projectId, "BattleRank", opponentPlayer,
                        new LeaderboardScore(score1));
                }
            }

            return battleResult;
        }

        int RealBattle(List<BattleCharacter> list1, List<BattleCharacter> list2)
        {
            int index1 = 0;
            int index2 = 0;

            while (index1 < list1.Count && index2 < list2.Count)
            {
                BattleCharacter bc1 = list1[index1];
                BattleCharacter bc2 = list2[index2];

                float hp1 = bc1.health;
                float hp2 = bc2.health;

                float at1 = MathF.Max(0.1f, (bc1.attack - bc2.defend) * bc1.repeat);
                float at2 = MathF.Max(0.1f, (bc2.attack - bc1.defend) * bc2.repeat);

                hp1 -= at2;
                hp2 -= at1;

                if (hp1 <= float.Epsilon)
                {
                    index1++;
                }
                else
                {
                    bc1.health = hp1;
                }
                if (hp2 <= float.Epsilon)
                {
                    index2++;
                }
                else
                {
                    bc2.health = hp2;
                }
            }

            if (index1 == list1.Count && index2 == list2.Count)
            {
                return 0;
            }
            if (index1 == list1.Count)
            {
                return -1;
            }

            return 1;
        }
    }
}
