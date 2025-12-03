using DorkyProductions.UI;
using UnityEngine;

namespace  DorkyProductions
{
    
public class ClientChestHandler : MonoBehaviour
{
    [SerializeField]
    private ChestOpenerUI chestUIController;
    [SerializeField]
    private TurnDisplayUI turnDisplayUI;
    
    [SerializeField] private Sprite rewardIcon;
    private int rewardIdToReceive;

    private NetworkPlayer _localPlayer;
    public void SaveReceivedChest(int rewardId, NetworkPlayer localPlayer)
    {
        this.rewardIdToReceive = rewardId;
        this._localPlayer = localPlayer;
    }

    public void OpenChest()
    {
        chestUIController.gameObject.SetActive(true);
        chestUIController.useButton.onClick.RemoveAllListeners();
        chestUIController.useButton.onClick.AddListener(UseSkill);
        
        // TODO: Actually use the reward index to get the determined Reward.
        chestUIController.Setup("Lightning", rewardIcon);
    }

    private void UseSkill()
    {
        chestUIController.gameObject.SetActive(false);
        
        // Show Lightning effect related stuff, maybe a call-to-action for now
        string lightningCallToAction = "Select a tile to remove from the board.";
        turnDisplayUI?.OverwriteTurnText(lightningCallToAction);
        // this reaches back to NetworkPlayer and trigger a Command
        // to execute this "skill effect" on the server side.
        _localPlayer.ActivateLightningInput();
    }
    
}

}