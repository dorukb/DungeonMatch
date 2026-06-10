using UnityEngine;
using Mirror;
public class TutorialLauncher : MonoBehaviour
{
    //TODO: Remove this, and check PlayerPrefs/save file to decide whether to launch tutorial or not.
    public bool forceTutorial = true;

    private void Start()
    {
        if (forceTutorial)
        {
            // Check whether we need a delay for any setup purposes in the MainMenu scene, might be the case.
            // lets be safe for now with a short delay.
            Invoke("StartGameForTutorial", 0.25f);
        }
    }

    public void StartGameForTutorial()
    {
        NetworkRoomManager manager = NetworkManager.singleton as NetworkRoomManager;
        if (manager != null)
        {
            manager.minPlayers = 1;
            manager.gameStartConfig = new GameStartConfig(true, true, LobbyType.Beginner);
            manager.StartHost();
        }
        else
        {
            Debug.LogError("NetworkRoomManager not found!");
        }
    }

}
