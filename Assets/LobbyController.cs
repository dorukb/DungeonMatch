using System;
using System.Collections.Generic;
using Epic.OnlineServices.Lobby;
using Mirror;
using UnityEngine;
using Attribute = Epic.OnlineServices.Lobby.Attribute;
using TMPro;

public class LobbyController : MonoBehaviour
{
    [SerializeField] private EOSLobby _eosLobby;
    
    [SerializeField] private NetworkManager manager;
    [SerializeField] private GameObject joinButton;

    [SerializeField] private TextMeshProUGUI statusText;
    
    private List<LobbyDetails> _foundLobbies = new List<LobbyDetails>();
    private List<Attribute> _lobbyData = new List<Attribute>();
    
    // Create a single, reusable Random instance.
    private static System.Random random = new System.Random();
    
    //register events
    private void OnEnable() {
        //subscribe to events
        _eosLobby.CreateLobbySucceeded += OnCreateLobbySuccess;
        _eosLobby.JoinLobbySucceeded += OnJoinLobbySuccess;
        _eosLobby.FindLobbiesSucceeded += OnFindLobbiesSuccess;
        _eosLobby.LeaveLobbySucceeded += OnLeaveLobbySuccess;
    }

    //deregister events
    private void OnDisable() {
        //unsubscribe from events
        _eosLobby.CreateLobbySucceeded -= OnCreateLobbySuccess;
        _eosLobby.JoinLobbySucceeded -= OnJoinLobbySuccess;
        _eosLobby.FindLobbiesSucceeded -= OnFindLobbiesSuccess;
        _eosLobby.LeaveLobbySucceeded -= OnLeaveLobbySuccess;
    }

    public void JoinMatch()
    {
        _eosLobby.FindLobbies();
        
    }
    //when the lobby is successfully created, start the host
    private void OnCreateLobbySuccess(List<Attribute> attributes) {
        _lobbyData = attributes;
        manager.StartHost();
        
        statusText.text = "Created Lobby. waiting for other player.";
        joinButton.SetActive(false);
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
        joinButton.SetActive(false);
    }

    private void OnFindLobbiesSuccess(List<LobbyDetails> lobbiesFound) {
        _foundLobbies = lobbiesFound;

        if (lobbiesFound.Count > 0)
        {
            Debug.Log("trying to join the first found lobby");
            LobbyDetails lobbyDetails = lobbiesFound[0];
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
        manager.StopHost();
        manager.StopClient();
    }

}
