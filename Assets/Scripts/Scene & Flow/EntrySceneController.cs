using System;
using System.Collections.Generic;
using Epic.OnlineServices.Lobby;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Attribute = Epic.OnlineServices.Lobby.Attribute;
using UnityEngine.UI;

namespace  DorkyProductions
{
public class EntrySceneController : MonoBehaviour
{
    [SerializeField] private Button joinButton;
    [SerializeField] private Button hostPrivateLobbyButton;
    [SerializeField] private GameObject joinFailedPopup;
    
    [Header("Host UI: Display Code")]
    [SerializeField] private TMP_InputField codeInputField;
    [SerializeField] private Button joinByCodeButton;
    
    // TODO: Remove coin text from here?
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] string OfflineModeLauncherSceneName = "OfflineModeLauncher";
    
    private EOSLobby _eosLobby;
    private NetworkRoomManager manager;
    
    private List<LobbyDetails> _foundLobbies = new List<LobbyDetails>();
    private List<Attribute> _lobbyData = new List<Attribute>();
    
    // Constants for Lobby Attribute Keys
    private const string KEY_LOBBY_NAME = "LOBBY_NAME";
    private const string KEY_ACCESS_LEVEL = "ACCESS_LEVEL"; // Distinguishes Public vs Private
    private const string KEY_JOIN_CODE = "JOIN_CODE";       // The secret code
    
    private bool _pendingLobbyCreationRequest = false;
    private void OnEnable() {
        //subscribe to events
        _eosLobby.CreateLobbySucceeded += OnCreateLobbySuccess;
        _eosLobby.CreateLobbyFailed += OnCreateLobbyFail;
        _eosLobby.JoinLobbySucceeded += OnJoinLobbySuccess;
        _eosLobby.FindLobbiesSucceeded += OnFindLobbiesSuccess;
        _eosLobby.FindLobbiesFailed += OnFindLobbiesFailed;
    }
    private void OnDisable() {
        //unsubscribe from events
        _eosLobby.CreateLobbySucceeded -= OnCreateLobbySuccess;
        _eosLobby.CreateLobbyFailed -= OnCreateLobbyFail;
        _eosLobby.JoinLobbySucceeded -= OnJoinLobbySuccess;
        _eosLobby.FindLobbiesSucceeded -= OnFindLobbiesSuccess;
        _eosLobby.FindLobbiesFailed -= OnFindLobbiesFailed;
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
        
        //Load coins
        // TODO: This should be centralized. UI scripts should query that central location, or get "push notifs".
        coinText.text = PlayerLocalSave.GetSavedCoins().ToString();
        
        joinButton.onClick.AddListener(() => 
        {
            AudioManager.Instance.PlaySFX(SFXType.TapPlayButton);
            JoinRandomLobby();
        });

        hostPrivateLobbyButton.onClick.AddListener(() =>
        {
            AudioManager.Instance.PlaySFX(SFXType.TapPlayButton);
            CreatePrivateLobby();
        });
            
        if (joinByCodeButton != null)
        {
            joinByCodeButton.onClick.AddListener(OnJoinByCodeClicked);
        }
        
        if (codeInputField != null)
        {
            codeInputField.onValidateInput += (input, charIndex, addedChar) => char.ToUpper(addedChar);
        }
    }
    public void LoadOfflineLauncherScene()
    {
        AudioManager.Instance.PlaySFX(SFXType.TapPlayButton);
        SceneManager.LoadScene(OfflineModeLauncherSceneName);
    }
    private void OnJoinByCodeClicked()
    {
        string rawInput = codeInputField.text;
        if (string.IsNullOrEmpty(rawInput))
        {
            Debug.LogWarning("[UI] Invite code is empty!");
            return;
        }

        // 3. Sanitize (Optional but recommended)
        // - Trim whitespace, ToUpper() because our generator uses uppercase (ABCDEF...)
        string cleanCode = rawInput.Trim().ToUpper();
        Debug.Log($"[UI] Searching for lobby with code: {cleanCode}");
        _eosLobby.FindLobbyByInviteCode(cleanCode);
        
        // change button
        joinByCodeButton.interactable = false;
    }
    private void JoinRandomLobby()
    {
        _eosLobby.FindLobbies();
        // Prevent multiple clicks while Search continues.
        joinButton.interactable = false;
    }

    //when the lobby is successfully created, start the host
    private void OnCreateLobbySuccess(List<Attribute> attributes) 
    {
        _pendingLobbyCreationRequest = false;
        _lobbyData = attributes;

        var gameConfig = new GameStartConfig(isOfflineMode: false, lobbyType: LobbyType.Beginner, "");
        var codeAttr = attributes.Find(x => x.Data.HasValue && x.Data.Value.Key == KEY_JOIN_CODE);

        // 2. Check if we found it (and if the value isn't null/empty)
        //    AttributeDataValue is a struct, so we check the actual string property
        if (codeAttr.Data.HasValue && !string.IsNullOrEmpty(codeAttr.Data.Value.Value.AsUtf8)) 
        {
            string inviteCode = codeAttr.Data.Value.Value.AsUtf8;
            Debug.Log($"[Lobby] PRIVATE Lobby Created! Share this code: {inviteCode}");
            gameConfig.joinCode = inviteCode;
        }
        else
        {
            Debug.Log("[Lobby] PUBLIC Lobby Created. No invite code needed.");
        }

        manager.gameStartConfig = gameConfig;
        manager.StartHost();
    
        Debug.Log("[Lobby] Waiting for other player...");
        joinButton.gameObject.SetActive(false);
    }
    private void OnCreateLobbyFail(string errormessage)
    {
        _pendingLobbyCreationRequest = false;
        Debug.LogWarning($"Create Lobby request failed: {errormessage}");
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

        manager.gameStartConfig.isOfflineMode = false;
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
            CreatePublicLobby();
        }
    }

    private void OnFindLobbiesFailed(string errormessage)
    {
        Debug.LogWarning($"Find lobbies failed: {errormessage}");
        joinFailedPopup.SetActive(true);
        joinByCodeButton.interactable = true;
    }

    private void CreatePublicLobby()
    {
        if (_pendingLobbyCreationRequest)
        {
            Debug.Log("Trying to create a new lobby while prev request is pending. Ignored.");
            return;
        }
        _pendingLobbyCreationRequest = true;
        
        int randomNumber = new System.Random().Next(0, 1000);
        string lobbyName = $"lobby{randomNumber}";
    
        Debug.Log($"Creating PUBLIC lobby: {lobbyName}");

        var attributes = new AttributeData[]
        {
            new AttributeData { 
                Key = KEY_LOBBY_NAME, 
                Value = new AttributeDataValue { AsUtf8 = lobbyName } 
            },
            // Attribute: Access Level (Public)
            new AttributeData { 
                Key = KEY_ACCESS_LEVEL, 
                Value = new AttributeDataValue { AsUtf8 = "PUBLIC" } 
            }
        };
        // We use PublicAdvertised so random search can find it
        _eosLobby.CreateLobby(
            2, 
            LobbyPermissionLevel.Publicadvertised, 
            false, 
            attributes
        );
    }
    private void CreatePrivateLobby()
    {
        if (_pendingLobbyCreationRequest)
        {
            Debug.Log("Trying to create a new lobby while prev request is pending. Ignored.");
            return;
        }
        _pendingLobbyCreationRequest = true;
        
        int randomNumber = new System.Random().Next(0, 1000);
        string lobbyName = $"lobby{randomNumber}";
        string inviteCode = GenerateInviteCode(5); // e.g., "AF32D"

        Debug.Log($"Creating PRIVATE lobby: {lobbyName} | Code: {inviteCode}");

        var attributes = new AttributeData[]
        {
            new AttributeData { 
                Key = KEY_LOBBY_NAME, 
                Value = new AttributeDataValue { AsUtf8 = lobbyName } 
            },
            // Attribute: Access Level (Private)
            // This tag allows the "Random Search" to filter this lobby OUT.
            new AttributeData { 
                Key = KEY_ACCESS_LEVEL, 
                Value = new AttributeDataValue { AsUtf8 = "PRIVATE" } 
            },
            // Attribute: The Invite Code, only way to join a Private Lobby.
            new AttributeData { 
                Key = KEY_JOIN_CODE, 
                Value = new AttributeDataValue { AsUtf8 = inviteCode } 
            }
        };

        // We still use PublicAdvertised so the specific search can find it.
        // The "Private" AccessLevel attrib is what keeps randoms out (via filtering during search).
        _eosLobby.CreateLobby(
            2, 
            LobbyPermissionLevel.Publicadvertised, 
            false, 
            attributes
        );
    }
    private string GenerateInviteCode(int length = 5)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var random = new System.Random();
        var result = new char[length];
        for (int i = 0; i < length; i++) result[i] = chars[random.Next(chars.Length)];
        return new string(result);
    }
    
}
    
}
