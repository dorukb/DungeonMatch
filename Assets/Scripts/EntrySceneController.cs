using System.Collections.Generic;
using System.IO;
using Epic.OnlineServices.Lobby;
using EpicTransport;
using Mirror;
using TMPro;
using UnityEngine;
using Attribute = Epic.OnlineServices.Lobby.Attribute;
using UnityEngine.UI;

namespace  DorkyProductions
{
public class EntrySceneController : MonoBehaviour
{
    [SerializeField] private Button joinButton;
    [SerializeField] private TextMeshProUGUI coinText; // Add your UI Text here
    
    private EOSLobby _eosLobby;
    private NetworkRoomManager manager;
    // [SerializeField] private TextMeshProUGUI statusText;
    
    private List<LobbyDetails> _foundLobbies = new List<LobbyDetails>();
    private List<Attribute> _lobbyData = new List<Attribute>();
    
    private static System.Random random = new System.Random();

    private void OnEnable() {
        //subscribe to events
        _eosLobby.CreateLobbySucceeded += OnCreateLobbySuccess;
        _eosLobby.JoinLobbySucceeded += OnJoinLobbySuccess;
        _eosLobby.FindLobbiesSucceeded += OnFindLobbiesSuccess;
    }


    private void OnDisable() {
        //unsubscribe from events
        _eosLobby.CreateLobbySucceeded -= OnCreateLobbySuccess;
        _eosLobby.JoinLobbySucceeded -= OnJoinLobbySuccess;
        _eosLobby.FindLobbiesSucceeded -= OnFindLobbiesSuccess;
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
        //joinButton.onClick.AddListener(JoinMatch);
        
        joinButton.onClick.AddListener(() => 
        {
            AudioManager.Instance.PlaySFX(SFXType.TapPlayButton); // Play sound first
            JoinMatch();                            // Then run logic
        });
    }
    
    private void Start()
    {
        // Start a safer checking loop
        StartCoroutine(WaitForSDKAndLoadCoins());
    }

    private System.Collections.IEnumerator WaitForSDKAndLoadCoins()
    {
        // 1. Specifically check the Epic SDK component status
        // This is the component causing your previous crash
        var eosSDK = FindAnyObjectByType<EpicTransport.EOSSDKComponent>();

        // Keep waiting as long as the SDK isn't ready
        while (eosSDK == null || !EOSSDKComponent.Initialized)
        {
            Debug.Log("[Lobby] Waiting for EOS SDK to initialize...");
            yield return null; // Wait for the next frame and check again
        }

        // 2. Once the loop breaks, the SDK is 100% ready
        Debug.Log("[Lobby] EOS SDK Ready. Loading Coins.");
        RefreshCoinDisplay();
    }

    public void RefreshCoinDisplay()
    {
        // Use SystemInfo.deviceUniqueIdentifier (the "Guest ID")
        string guestID = SystemInfo.deviceUniqueIdentifier;
        string path = Application.persistentDataPath + "/" + guestID + "_data.json";

        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            // Access the PlayerData class defined in your NetworkPlayer
            NetworkPlayer.PlayerData data = JsonUtility.FromJson<NetworkPlayer.PlayerData>(json);
            if (coinText != null) coinText.text = data.coins.ToString();
        }
        else
        {
            // First time playing or no data found
            if (coinText != null) coinText.text = "50"; 
        }
    }

    public void JoinMatch()
    {
        _eosLobby.FindLobbies();
        
        // Prevent multiple clicks while Search continues.
        joinButton.interactable = false;
    }

    //when the lobby is successfully created, start the host
    private void OnCreateLobbySuccess(List<Attribute> attributes) {
        _lobbyData = attributes;
        
        manager.isOfflineMode = false;
        manager.StartHost();
        
        Debug.Log("[Lobby] Created Lobby. waiting for other player.");
        joinButton.gameObject.SetActive(false);
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

        manager.isOfflineMode = false;
        manager.networkAddress = hostAddressAttribute.Data.Value.Value.AsUtf8;
        manager.StartClient();
        
        Debug.Log("[Lobby] In the Lobby with another player.");
        joinButton.gameObject.SetActive(false);
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
}
    
}
