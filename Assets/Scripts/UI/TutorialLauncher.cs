using DorkyProductions;
using UnityEngine;
using Mirror;
public class TutorialLauncher : MonoBehaviour
{
    private void Start()
    {
        if (PlayerLocalSave.HasCompletedTutorial())
        {
            Debug.Log("Tutorial already completed, skipping tutorial launch.");
            return;
        }
        else
        {
            // Check whether we need a delay for any setup purposes in the MainMenu scene, might be the case.
            // lets be safe for now with a short delay.
            /*todo: sometimes eos not completed. find a way to check it 
            also there can be other thing uncompleted.
            find a way to check all before tutorial starts.*/
            Invoke("StartGameForTutorial", 5.0f);
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
