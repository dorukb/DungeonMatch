using UnityEngine;

namespace  DorkyProductions
{
    
public class ClientChestHandler : MonoBehaviour
{
    [SerializeField]
    private ChestOpenerUI chestUIController;

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
        chestUIController.useButton.onClick.AddListener(UseButtonCallback);
        
        // TODO: Actually use the reward index to get the determined Reward.
        chestUIController.Setup("Chest Skill #1", rewardIcon);
    }

    private void UseButtonCallback()
    {
        chestUIController.gameObject.SetActive(false);
        // this reaches back to NetworkPlayer and trigger a Command
        // to execute this "skill effect" on the server side.
        // TODO: It should actually start a "new input sequence" based on the Skill effect.
        // i.e, Select A row, Select a tile to remove and so on.
        _localPlayer.AttemptSkillUse(rewardIdToReceive);
    }
    
}

}