using DorkyProductions.Core;
using Mirror;
using TMPro;
using UnityEngine;

public class PlayerNameDisplayer : NetworkBehaviour
{
    [SerializeField] private TextMeshProUGUI playerName;
    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        playerName.text = GameMaster.Instance.GetLocalPlayer().DisplayName;
    }
}
