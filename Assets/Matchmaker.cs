// using UnityEngine;
// using Unity.Services.Core;
// using Unity.Services.Authentication;
// using Unity.Services.Lobbies.Models;
// using Unity.Services.Relay;
// using Unity.Networking.Transport.Relay;
// using Mirror;
// using System.Threading.Tasks;
// using Unity.Services.Lobbies;
//
// using Unity.Networking.Transport;
// public class Matchmaker : MonoBehaviour
// {
//     private string lobbyId;
//     private const string RelayJoinCodeKey = "RelayJoinCode";
//
//     // --- Singleton Pattern ---
//     public static Matchmaker instance;
//
//     private void Awake()
//     {
//         if (instance == null)
//         {
//             instance = this;
//         }
//         else
//         {
//             Destroy(gameObject);
//         }
//     }
//
//     async void Start()
//     {
//         // Initialize Unity Services
//         await UnityServices.InitializeAsync();
//
//         // Sign in the player anonymously
//         await SignInPlayerAsync();
//     }
//
//     async Task SignInPlayerAsync()
//     {
//         try
//         {
//             if (!AuthenticationService.Instance.IsSignedIn)
//             {
//                 await AuthenticationService.Instance.SignInAnonymouslyAsync();
//                 Debug.Log($"Signed in as Player ID: {AuthenticationService.Instance.PlayerId}");
//             }
//         }
//         catch (AuthenticationException ex)
//         {
//             Debug.LogError($"Sign in failed: {ex.Message}");
//         }
//     }
//
//     // This is the main function called by your UI button
//     public async void FindMatch()
//     {
//         Debug.Log("Looking for a lobby...");
//         try
//         {
//             // 1. Query for available lobbies
//             QueryLobbiesOptions options = new QueryLobbiesOptions
//             {
//                 // Filter for lobbies with at least one available slot
//                 Filters = new System.Collections.Generic.List<QueryFilter>
//                 {
//                     new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "1", QueryFilter.OpOptions.GE)
//                 }
//             };
//
//             QueryResponse lobbies = await LobbyService.Instance.QueryLobbiesAsync(options);
//
//             // 2. Check if any lobbies were found
//             if (lobbies.Results.Count > 0)
//             {
//                 // Join the first available lobby found
//                 Lobby lobby = lobbies.Results[0];
//                 Debug.Log($"Found an existing lobby! Joining lobby with ID: {lobby.Id}");
//                 await JoinLobby(lobby.Id);
//             }
//             else
//             {
//                 // No lobbies found, create a new one
//                 Debug.Log("No lobbies found. Creating a new one...");
//                 await CreateLobby();
//             }
//         }
//         catch (LobbyServiceException e)
//         {
//             Debug.LogError($"Failed to find or create lobby: {e}");
//         }
//     }
//
//     private async Task CreateLobby()
//     {
//         try
//         {
//             // Create the Relay allocation (the Host's connection data)
//             var allocation = await RelayService.Instance.CreateAllocationAsync(1); // Max 1 other player
//             string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
//
//             // Create a lobby and store the Relay join code in its data
//             CreateLobbyOptions options = new CreateLobbyOptions
//             {
//                 Data = new System.Collections.Generic.Dictionary<string, DataObject>
//                 {
//                     { RelayJoinCodeKey, new DataObject(DataObject.VisibilityOptions.Member, joinCode) }
//                 }
//             };
//
//             Lobby lobby = await LobbyService.Instance.CreateLobbyAsync("My Game Lobby", 2, options);
//             this.lobbyId = lobby.Id;
//
//             Debug.Log($"Lobby created! Lobby ID: {lobby.Id}, Relay Join Code: {joinCode}");
//             
//             // Start the host via Mirror using the Relay data
//             var relayServerData = new RelayServerData(allocation, "dtls");
//             NetworkManager.singleton.GetComponent<UtpTransport>().SetRelayServerData(relayServerData);
//             NetworkManager.singleton.StartHost();
//         }
//         catch (LobbyServiceException e)
//         {
//             Debug.LogError($"Failed to create lobby: {e}");
//         }
//         catch (RelayServiceException e)
//         {
//             Debug.LogError($"Failed to create Relay allocation: {e}");
//         }
//     }
//
//     private async Task JoinLobby(string lobbyId)
//     {
//         try
//         {
//             // Join the lobby by its ID
//             Lobby joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId);
//             this.lobbyId = joinedLobby.Id;
//             
//             // Retrieve the Relay join code from the lobby's data
//             string joinCode = joinedLobby.Data[RelayJoinCodeKey].Value;
//             Debug.Log($"Joined lobby. Got Relay Join Code: {joinCode}");
//
//             // Join the Relay allocation using the join code
//             var joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
//
//             // Start the client via Mirror using the Relay data
//             var relayServerData = new RelayServerData(joinAllocation, "dtls");
//             NetworkManager.singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
//             NetworkManager.singleton.StartClient();
//         }
//         catch (LobbyServiceException e)
//         {
//             Debug.LogError($"Failed to join lobby: {e}");
//         }
//         catch (RelayServiceException e)
//         {
//             Debug.LogError($"Failed to join Relay allocation: {e}");
//         }
//     }
// }