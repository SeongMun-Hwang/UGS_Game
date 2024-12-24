using System;
using TMPro;
using UnityEngine;

public class BattleButton : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI playerNameTmp;
    string playerId;
    public Action<string> onBattleStart;

    public void SetPlayerId(int rank,string playerId,string playerName)
    {
        playerNameTmp.text = "#" + rank + " " + playerName;
        this.playerId = playerId;
    }
    public void BattleStart()
    {
        if (onBattleStart != null)
        {
            onBattleStart(playerId);
        }
    }
}
