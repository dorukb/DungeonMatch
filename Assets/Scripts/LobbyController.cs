using System.Collections.Generic;
using Epic.OnlineServices;
using Epic.OnlineServices.Lobby;
using Mirror;
using UnityEngine;
using Attribute = Epic.OnlineServices.Lobby.Attribute;
using UnityEngine.UI;

namespace DorkyProductions
{
public class LobbyController : MonoBehaviour
{
    [SerializeField] private Button leaveLobbyButton;

    private EOSLobby _eosLobby;
    private NetworkRoomManager manager;
    // [SerializeField] private TextMeshProUGUI statusText;
    
    private List<LobbyDetails> _foundLobbies = new List<LobbyDetails>();
    private List<Attribute> _lobbyData = new List<Attribute>();
    
    private static System.Random random = new System.Random();

    private void OnEnable() {
        //subscribe to events
        _eosLobby.LeaveLobbySucceeded += OnLeaveLobbySuccess;
        _eosLobby.LeaveLobbyFailed += OnLeaveLobbyFailed;
        
        // TODO: We had to change the Server code to include this extra event, which feels wrong.
        // maybe using NetworkManager.singleton.OnServerDisconnect() is a better integration way.
        // then, we need our own CustomNetworkManager and override that func.
        EpicTransport.Server.OnClientDisconnectedFromServer += HandleClientDisconnect;
        EpicTransport.Client.OnHostShutdownAbruptly += HandleHostShutdownAbruptly;
    }


    private void OnDisable() {
        //unsubscribe from events
        _eosLobby.LeaveLobbySucceeded -= OnLeaveLobbySuccess;
        _eosLobby.LeaveLobbyFailed -= OnLeaveLobbyFailed;
        
        EpicTransport.Server.OnClientDisconnectedFromServer -= HandleClientDisconnect;
        EpicTransport.Client.OnHostShutdownAbruptly -= HandleHostShutdownAbruptly;
    }

    private void Awake()
    {
        _eosLobby = FindAnyObjectByType<EOSLobby>();
        manager = FindAnyObjectByType<NetworkRoomManager>();

        if (_eosLobby == null)
        {
            Debug.LogError("EOS Lobby not found!");
        }

        if (manager == null)
        {
            Debug.LogError("Network Manager not found!");
        }
        
        // Connect the Button click programmatically.
        leaveLobbyButton.onClick.AddListener(RequestLeaveLobby);
    }
    public void RequestLeaveLobby()
    {
        if (manager.isOfflineMode)
        {
            manager.StopHost();
        }
        else
        {
            _eosLobby.LeaveLobby();
            leaveLobbyButton.interactable = false;
        }
    }
    
    private void HandleClientDisconnect(ProductUserId leavingClientID)
    {
        // Note: We might wait for reconnect in the future. For now, directly close down the Lobby if client is DC'ed.
        Debug.Log($"Client {leavingClientID} disconnected, Close the Lobby.");
        RequestLeaveLobby();
    }

    private void HandleHostShutdownAbruptly()
    {
        Debug.Log($"EOS sent DC signal, interpreted as Lobby Shutdown. Make sure the User knows about this.");
        // statusText.text = "Connected Lobby was Destroyed, Look for a new game.";
        leaveLobbyButton.gameObject.SetActive(false);
    }

    //when the lobby was left successfully, stop the host/client
    private void OnLeaveLobbySuccess() {
        Debug.Log($"Successfully Left the lobby. Closing P2P connection.");
        manager.StopHost();
        manager.StopClient();
        leaveLobbyButton.gameObject.SetActive(false);
        leaveLobbyButton.interactable = true;
    }
    
    private void OnLeaveLobbyFailed(string errormessage)
    {
        Debug.Log($"LeaveLobby failed: {errormessage}, expected, as we have many redundant calls.");
        leaveLobbyButton.interactable = true;
    }
}

}