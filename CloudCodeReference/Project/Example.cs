using Unity.Services.CloudCode.Core;
using Microsoft.Extensions.Logging;
using Unity.Services.CloudCode.Apis;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Project;
namespace HelloWorld;

public class MyModule
{
    ILogger<MyModule> logger; //�α� ��� �� ���
    IGameApiClient apiClient; //UGS ��� ����ϴµ� ���

    public MyModule(IGameApiClient apiClient, ILogger<MyModule> logger)
    {
        this.apiClient = apiClient;
        this.logger = logger;
    }

    //�Լ��̸��� Hello���� Ŭ���� �ڵ忡���� SayHello��� ȣ���� ���̴�
    [CloudCodeFunction("SayHello")]
    public string Hello(string name)
    {
        return $"Hello, {name}!";
    }
    [CloudCodeFunction("GetGacha")]
    public async Task<string> GetGacha(IExecutionContext context, IGameApiClient gameApiClient)
    {
        //var result = await gameApiClient.CloudSaveData.GetItemsAsync(context, context.AccessToken, context.ProjectId, context.PlayerId,
        //    new List<string> { "Player" });
        //string savedData=result.Data.Results.First().Value.ToString();
        //logger.LogDebug(savedData);
        //return savedData;

        GachaManager gachaManager = new GachaManager(logger, apiClient);
        string result = await gachaManager.DoGacha(context);
        return result;
    }
    [CloudCodeFunction("RegisterDeck")]
    public async Task RegisterDeck(IExecutionContext context, IGameApiClient gameApiClient)
    {
        BattleSimulator simulator = new BattleSimulator(logger, apiClient);
        await simulator.RegisterPlayerDeck(context);
    }
    [CloudCodeFunction("Battle")]
    public async Task Battle(IExecutionContext context, IGameApiClient gameApiClient, string opponentPlayer)
    {
        BattleSimulator simulator=new BattleSimulator(logger, apiClient);
        await simulator.Battle(context, opponentPlayer);
    }
    public class ModuleConfig : ICloudCodeSetup //������ ����
    {
        public void Setup(ICloudCodeConfig config)
        {
            config.Dependencies.AddSingleton(GameApiClient.Create());
        }
    }
}