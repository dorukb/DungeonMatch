using System;
using System.Collections.Generic;
using Epic.OnlineServices;
using Epic.OnlineServices.Lobby;
using Mirror;
using UnityEngine;
using Attribute = Epic.OnlineServices.Lobby.Attribute;
using TMPro;
using UnityEngine.UI;

public class LobbyController : MonoBehaviour
{
    [SerializeField] private EOSLobby _eosLobby;
    
    [SerializeField] private NetworkManager manager;
    [SerializeField] private Button joinButton;
    [SerializeField] private Button leaveLobbyButton;

    [SerializeField] private TextMeshProUGUI statusText;
    
    private List<LobbyDetails> _foundLobbies = new List<LobbyDetails>();
    private List<Attribute> _lobbyData = new List<Attribute>();
    
    private static System.Random random = new System.Random();
    
    private void OnEnable() {
        //subscribe to events
        _eosLobby.CreateLobbySucceeded += OnCreateLobbySuccess;
        _eosLobby.JoinLobbySucceeded += OnJoinLobbySuccess;
        _eosLobby.FindLobbiesSucceeded += OnFindLobbiesSuccess;
        _eosLobby.LeaveLobbySucceeded += OnLeaveLobbySuccess;
        _eosLobby.LeaveLobbyFailed += OnLeaveLobbyFailed;
        
        // TODO: We had to change the Server code to include this extra event, which feels wrong.
        // maybe using NetworkManager.singleton.OnServerDisconnect() is a better integration way.
        // then, we need our own CustomNetworkManager and override that func.
        EpicTransport.Server.OnClientDisconnectedFromServer += HandleClientDisconnect;
    }


    private void OnDisable() {
        //unsubscribe from events
        _eosLobby.CreateLobbySucceeded -= OnCreateLobbySuccess;
        _eosLobby.JoinLobbySucceeded -= OnJoinLobbySuccess;
        _eosLobby.FindLobbiesSucceeded -= OnFindLobbiesSuccess;
        _eosLobby.LeaveLobbySucceeded -= OnLeaveLobbySuccess;
        _eosLobby.LeaveLobbyFailed -= OnLeaveLobbyFailed;
        
        EpicTransport.Server.OnClientDisconnectedFromServer -= HandleClientDisconnect;
    }

    private void Start()
    {
        leaveLobbyButton.gameObject.SetActive(false);
    }

    public void JoinMatch()
    {
        _eosLobby.FindLobbies();
        
        // Prevent multiple clicks while Search continues.
        joinButton.interactable = false;
    }

    public void RequestLeaveLobby()
    {
        _eosLobby.LeaveLobby();
        leaveLobbyButton.interactable = false;
    }
    
    private void HandleClientDisconnect(ProductUserId leavingClientID)
    {
        // Note: We might wait for reconnect in the future. For now, directly close down the Lobby if client is DC'ed.
        Debug.Log($"Client {leavingClientID} disconnected, Close the Lobby.");
        RequestLeaveLobby();
    }

    //when the lobby is successfully created, start the host
    private void OnCreateLobbySuccess(List<Attribute> attributes) {
        _lobbyData = attributes;
        manager.StartHost();
        
        statusText.text = "Created Lobby. waiting for other player.";
        joinButton.gameObject.SetActive(false);
        leaveLobbyButton.gameObject.SetActive(true);
    }

    //when the user joined the lobby successfully, set network address and connect
    private void OnJoinLobbySuccess(List<Attribute> attributes) {
        _lobbyData = attributes;

        Attribute hostAddressAttribute = attributes.Find((x) => x.Data.HasValue && x.Data.Value.Key == EOSLobby.hostAddressKey);
        if (!hostAddressAttribute.Data.HasValue)
        {
            Debug.LogError("Host address not found in lobby attributes. Cannot connect to host.");
            return;
        }

        manager.networkAddress = hostAddressAttribute.Data.Value.Value.AsUtf8;
        manager.StartClient();
        
        statusText.text = "Game Started.";
        joinButton.gameObject.SetActive(false);
        leaveLobbyButton.gameObject.SetActive(true);
    }

    private void OnFindLobbiesSuccess(List<LobbyDetails> lobbiesFound) {
        _foundLobbies = lobbiesFound;

        if (lobbiesFound.Count > 0)
        {
            Debug.Log("trying to join the first found lobby");
            LobbyDetails lobbyDetails = lobbiesFound[0];
            // lobbyDetails.
            _eosLobby.JoinLobby(lobbyDetails);
        }
        else
        {
            const string lobbyNameKey = "LobbyName";
            int randomNumber = random.Next(0, 1000);
            string lobbyName = $"lobby{randomNumber}";
            Debug.Log("No lobbies. Creating one called: " + lobbyName);
            _eosLobby.CreateLobby(2, LobbyPermissionLevel.Publicadvertised, false,
                new AttributeData[]
                {
                    new AttributeData
                    {
                        Key = lobbyNameKey, Value = lobbyName
                    },
                });
        }
    }
    
    //when the lobby was left successfully, stop the host/client
    private void OnLeaveLobbySuccess() {
        Debug.Log($"Succesfully Left the lobby. Closing P2P connection.");
        manager.StopHost();
        manager.StopClient();
        leaveLobbyButton.gameObject.SetActive(false);
        leaveLobbyButton.interactable = true;
        
        statusText.text = "Left the Lobby, Main Menu.";
        joinButton.gameObject.SetActive(true);
        joinButton.interactable = true;
    }
    
    private void OnLeaveLobbyFailed(string errormessage)
    {
        Debug.LogError($"LeaveLobby failed: {errormessage}, maybe try again? or cry.");
        leaveLobbyButton.interactable = true;
    }


}
