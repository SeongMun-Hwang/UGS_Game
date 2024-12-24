using HelloWorld;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;
using Unity.Services.CloudSave.Model;

namespace Project
{
    internal class GachaManager
    {
        ILogger<MyModule> logger;
        IGameApiClient apiClient;

        [Serializable]
        internal class GachaItem
        {
            public string Name { get; set; }
            public int Factor { get; set; }
        }
        internal class CharacterOwned
        {
            public string Name { get; set; }
            public int Level { get; set; }
        }
        public GachaManager(ILogger<MyModule> logger, IGameApiClient apiClient)
        {
            this.logger = logger;
            this.apiClient = apiClient;
        }
        public async Task<string> DoGacha(IExecutionContext context)
        {
            var result = await apiClient.RemoteConfigSettings.AssignSettingsGetAsync(
                context, context.AccessToken,
                context.ProjectId,
                context.EnvironmentId,
                null,
                new List<string> { "GachaProbabilityTable" });

            List<GachaItem> items = JsonConvert.DeserializeObject<List<GachaItem>>(result.Data.Configs.Settings["GachaProbabilityTable"].ToString());

            int totalFactor = items.Sum(item => item.Factor);
            string selectedName = "";
            Random random = new Random();
            int randomValue = random.Next(totalFactor);
            int countWeight = 0;


            foreach (GachaItem item in items)
            {
                countWeight += item.Factor;
                if (randomValue < countWeight)
                {
                    selectedName = item.Name;
                    break;
                }
            }
            var charcaterSaved = await apiClient.CloudSaveData.GetItemsAsync(
                context, context.AccessToken, context.ProjectId, context.PlayerId, new List<string>
                {
                    "Character"
                });
            List<CharacterOwned> characters = new List<CharacterOwned>();
            if (charcaterSaved.Data.Results.Count == 0)
            {
                characters.Add(new CharacterOwned
                {
                    Name = selectedName,
                    Level = 1,
                });
            }
            else
            {
                var savedData = charcaterSaved.Data.Results.FirstOrDefault(item => item.Key == "Character");
                if (savedData != null)
                {
                    characters = JsonConvert.DeserializeObject<List<CharacterOwned>>(savedData.Value.ToString());
                    var exisitingCharacter = characters.FirstOrDefault(c => c.Name == selectedName);
                    if (exisitingCharacter != null)
                    {
                        exisitingCharacter.Level += 1;
                    }
                    else
                    {
                        characters.Add(new CharacterOwned
                        {
                            Name = selectedName,
                            Level = 1,
                        });
                    }
                }
            }

            await apiClient.CloudSaveData.SetItemAsync(
                context, context.AccessToken, context.ProjectId, context.PlayerId,
                new SetItemBody("Character", JsonConvert.SerializeObject(characters)));
            return selectedName;
        }
    }
}
