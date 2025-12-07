using UnityEngine;
using Mirror;

namespace DorkyProductions
{
    public class OfflineLauncher : MonoBehaviour
    {
        [Header("References")] 
        public NetworkRoomManager netman;

        public void StartOfflineMatch()
        {
            if (NetworkManager.singleton == null)
            {
                Debug.LogError("NetworkManager not found!");
                return;
            }

            netman.isOfflineMode = true;
            netman.minPlayers = 1;
            netman.StartHost();
        }
    }
}