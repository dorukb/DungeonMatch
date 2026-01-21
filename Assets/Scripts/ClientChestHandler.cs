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
    
    [SerializeField]
    private TileDatabase tileDatabase; 

    private NetworkPlayer _localPlayer;
    private SkillDefinitionSO _rewardedSkill;
    public void SaveReceivedChest(int rewardedSkillID, NetworkPlayer localPlayer)
    {
        this._localPlayer = localPlayer;
        this._rewardedSkill = tileDatabase.GetSkill(rewardedSkillID);
        if (_rewardedSkill == null)
        {
            Debug.LogError($"Could not find reward skill with id: {rewardedSkillID}");
        }
    }

    public void OpenChest()
    {
        chestUIController.gameObject.SetActive(true);
        chestUIController.useButton.onClick.RemoveAllListeners();
        chestUIController.useButton.onClick.AddListener(UseSkill);
        
        chestUIController.Setup(_rewardedSkill.skillName, _rewardedSkill.icon);
        AudioManager.Instance.PlaySFX(SFXType.ChestOpen);
    }

    private void UseSkill()
    {
        chestUIController.gameObject.SetActive(false);
        UIMediator.OnPlayerChestOpened?.Invoke(_rewardedSkill.id);

        // Enable specific input logic that allows the use of the Skill.
        // NetworkPlayer triggers the command.
        if (_rewardedSkill.skillType == SkillType.Lightning)
        {
            _localPlayer.ActivateLightningInput();
        }
        else if (_rewardedSkill.skillType == SkillType.PhantomMatch)
        {
            _localPlayer.ActivatePhantomMatchInput();
        }
        else if (_rewardedSkill.skillType == SkillType.PhaseShift)
        {
            _localPlayer.ActivatePhaseShiftInput();
        }
        else if (_rewardedSkill.skillType == SkillType.StoneGuard)
        {
            _localPlayer.OnUseStoneGuard();
        }
        else if (_rewardedSkill.skillType == SkillType.SoulReaver)
        {
            _localPlayer.OnUseSoulReaver();
        }
        else if (_rewardedSkill.skillType == SkillType.ArcaneSweep)
        {
            _localPlayer.ActivateArcaneSweepInput();
        }
        else if (_rewardedSkill.skillType == SkillType.ArcaneCleave)
        {
            _localPlayer.ActivateArcaneCleaveInput();
        }
        
    }
    
}

}