using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Services.CloudCode;
using Unity.Services.CloudCode.GeneratedBindings;
using System.Collections;

public class GachaManager : MonoBehaviour
{
    [SerializeField] GameObject gate;
    [SerializeField] Transform characterParent;
    [SerializeField] GameObject blockCanvas;
    [SerializeField] CharacterPrefab[] prefabs;

    bool isWaiting = false;
    string characterGet;
    async void Start()
    {
        await UnityServices.InitializeAsync();
        if (AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        //SignInAnonymouslyAsync() - 계정을 따로 안 만들고 로그인. 흔히 아는 게스트 로그인
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            BackPressed();
        }
    }
    public void BackPressed()
    {
        SceneManager.LoadScene("MainScene");
    }
    public async void GachaPressed()
    {
        StartCoroutine(WaitGachaRoutine());
        CallGachaCloudCode();
    }
    IEnumerator WaitGachaRoutine()
    {
        blockCanvas.SetActive(true);
        float timer = 0;

        gate.GetComponent<Animator>().Play("Idle");

        yield return null;

        while (timer < 2 || isWaiting)
        {
            timer += 2;
            gate.GetComponent<Animator>().Play("Shaking");
            yield return new WaitForSeconds(2f);
        }
        if (characterParent.childCount != 0)
        {
            Destroy(characterParent.GetChild(0).gameObject);
        }
        foreach(CharacterPrefab prefab in prefabs)
        {
            if (prefab.characterName.Equals(characterGet))
            {
                Instantiate(prefab.prefab, characterParent.transform);
                break;
            }
        }
        blockCanvas.SetActive(false);
        gate.GetComponent<Animator>().Play("Open");
    }
    async void CallGachaCloudCode()
    {
        isWaiting = true;
        var module = new ProjectBindings(CloudCodeService.Instance);
        var result = await module.GetGacha();

        Debug.Log("Cloud code result : " + result);
        characterGet = result;
        isWaiting = false;
    }
}
