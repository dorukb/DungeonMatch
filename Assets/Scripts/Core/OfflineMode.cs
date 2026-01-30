using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;

namespace DorkyProductions
{
    public class OfflineLauncher : MonoBehaviour
    {
        [Header("References")] 
        public NetworkRoomManager netman;
        public string optionsSceneName = "OfflineOptions";
        public void GoToOptions()
        {
            AudioManager.Instance.PlaySFX(SFXType.TapPlayButton);
            SceneManager.LoadScene(optionsSceneName);
        }
        /*public void StartOfflineMatch()
        {
            if (NetworkManager.singleton == null)
            {
                Debug.LogError("NetworkManager not found!");
                return;
            }
            
            AudioManager.Instance.PlaySFX(SFXType.TapPlayButton);
            netman.isOfflineMode = true;
            netman.minPlayers = 1;
            netman.StartHost();
        }*/
    }
}