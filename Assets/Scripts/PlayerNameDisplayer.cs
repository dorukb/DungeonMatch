using Mirror;
using TMPro;
using UnityEngine;

namespace DorkyProductions
{
    
public class PlayerNameDisplayer : NetworkBehaviour
{
    [SerializeField] private TextMeshProUGUI playerNameText;

    // This is the variable that will be synchronized across the network.
    // The 'hook' will automatically call our update method whenever this changes.
    [SyncVar(hook = nameof(OnNameUpdated))]
    public string networkDisplayName = "Waiting..."; // Default name while loading

    private PlayerAvatarUIController _avatarUIController;
    private void Start()
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
        playerNameText.text = newName;
        if (!isLocalPlayer)
        {
            _avatarUIController.SetOpponentPlayerNameText(newName);
        }
    }

    // This is called for every player object when they are first created on a client.
    public override void OnStartClient()
    {
        // Set the initial name. This ensures that when a new player joins,
        // everyone sees their default name immediately. The hook will update it shortly after.
        playerNameText.text = networkDisplayName;
    }

    // This is called ONLY for the player that you control.
    public override void OnStartLocalPlayer()
    {
        string localName = PlayerNameGenerator.GetChosenName();
        if (string.IsNullOrEmpty(localName))
        {
            localName = "Unnamed Player"; // Fallback name
        }

        _avatarUIController.SetLocalPlayerNameText(localName);
        playerNameText.text = localName;
        CmdSetDisplayName(localName);
    }

    // This [Command] is sent from your client to the server.
    [Command]
    private void CmdSetDisplayName(string name)
    {
        // The server receives the name and sets the SyncVar.
        // As soon as this line runs on the server, Mirror will send the new name
        // to all clients, which will trigger the 'OnNameUpdated' hook on each client.
        networkDisplayName = name;
    }
}

}