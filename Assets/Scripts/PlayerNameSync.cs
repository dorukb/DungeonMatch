using Mirror;
using UnityEngine;

namespace DorkyProductions
{
    
public class PlayerNameSync : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnNameUpdated))]
    private string networkDisplayName = "...";

    private PlayerPortraitUI _localPlayerPortraitUI;
    private PlayerPortraitUI _opponentPlayerPortraitUI;


    private void Awake()
    {
        var playerPortraitUIs = FindObjectsByType<PlayerPortraitUI>(FindObjectsSortMode.None);
        if (playerPortraitUIs.Length == 2)
        {
            for (int i = 0; i < playerPortraitUIs.Length; i++)
            {
                if (playerPortraitUIs[i].IsLocalPlayer)
                {
                    _localPlayerPortraitUI = playerPortraitUIs[i];
                }
                else
                {
                    _opponentPlayerPortraitUI = playerPortraitUIs[i];
                }
            }
        }
        else
        {
            Debug.LogError("There must be exactly 2 Player portrait UI scripts.");
        }
    }

    // This method is called automatically on ALL clients when 'networkDisplayName' changes value on the server.
    private void OnNameUpdated(string oldName, string newName)
    {
        if (!isLocalPlayer)
        {
            _opponentPlayerPortraitUI.SetPlayerNameText(newName);
        }
    }
    
    public override void OnStartLocalPlayer()
    {
        string localName = PlayerNameGenerator.GetChosenName();
        if (string.IsNullOrEmpty(localName))
        {
            localName = "Unnamed Player"; // Fallback name
        }

        _localPlayerPortraitUI.SetPlayerNameText(localName);
        CmdSetDisplayName(localName);
    }

    [Command]
    private void CmdSetDisplayName(string playerName)
    {
        // The server receives the name and sets the SyncVar.
        // As soon as this line runs on the server, Mirror will send the new name
        // to all clients, which will trigger the 'OnNameUpdated' hook on each client.
        networkDisplayName = playerName;
    }
}

}