using System;
using System.Collections.Generic;
using DG.Tweening;
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
    
    private Dictionary<SkillType, Action> _skillActions;

    private void Start()
    {
        InitializeSkillMap();
    }
    private void InitializeSkillMap()
    {
        _skillActions = new Dictionary<SkillType, Action>
        {
            { SkillType.Lightning,    () => _localPlayer.ActivateLightningInput() },
            { SkillType.PhantomMatch, () => _localPlayer.ActivatePhantomMatchInput() },
            { SkillType.PhaseShift,   () => _localPlayer.ActivatePhaseShiftInput() },
            { SkillType.ArcaneSweep,  () => _localPlayer.ActivateArcaneSweepInput() },
            { SkillType.ArcaneCleave, () => _localPlayer.ActivateArcaneCleaveInput() },
        
            // Handle types that require the DOTween Sequence
            { SkillType.StoneGuard,   () => CreateSkillSequence(_localPlayer.OnUseStoneGuard) },
            { SkillType.SoulReaver,   () => CreateSkillSequence(_localPlayer.OnUseSoulReaver) }
        };
    }
    
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

    

    private void CreateSkillSequence(Action onCompleteAction)
    {
        Sequence s = DOTween.Sequence();
        s.AppendInterval(1f); 
        // s.Append(_someIcon.DOMove(target, 0.5f));
        s.OnComplete(() => onCompleteAction?.Invoke());
    }

    private void UseSkill()
    {
        chestUIController.gameObject.SetActive(false);
        UIMediator.OnPlayerChestOpened?.Invoke(_rewardedSkill.id);

        if (_skillActions.TryGetValue(_rewardedSkill.skillType, out Action skillLogic))
        {
            skillLogic.Invoke();
        }
        else
        {
            Debug.LogWarning($"SkillType {_rewardedSkill.skillType} not implemented in map.");
        }
    }
    
}

}