using Mirror;
using UnityEngine;

namespace DorkyProductions
{
    
public class PlayerNameSync : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnNameUpdated))]
    private string networkDisplayName = "...";

    private PlayerAvatarUIController _avatarUIController;
    private void Awake()
    {
        _avatarUIController = FindAnyObjectByType<PlayerAvatarUIController>();
        if (_avatarUIController == null)
        {
            Debug.LogError("PlayerAvatarUIController not found in Game Scene. Make sure one exists.");
        }
    }

    // This method is called automatically on ALL clients when 'networkDisplayName' changes value on the server.
    private void OnNameUpdated(string oldName, string newName)
    {
        if (!isLocalPlayer)
        {
            _avatarUIController.SetOpponentPlayerNameText(newName);
        }
    }
    
    public override void OnStartLocalPlayer()
    {
        string localName = PlayerNameGenerator.GetChosenName();
        if (string.IsNullOrEmpty(localName))
        {
            localName = "Unnamed Player"; // Fallback name
        }

        _avatarUIController.SetLocalPlayerNameText(localName);
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