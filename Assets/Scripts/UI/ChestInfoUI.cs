using System;
using System.Collections.Generic;
using DorkyProductions;
using TMPro;
using UnityEngine;
using DorkyProductions.UI; // Make sure this is correct

// NOTE: We don't need 'using System.Collections;' anymore!

public class ChestInfoUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI chestInfoText;
    [SerializeField] private TileDatabase tileDatabase; 
    
    private void OnEnable()
    {
        // Ensure the text is initially hidden
        chestInfoText.gameObject.SetActive(false);
        UIMediator.OnPlayerChestOpened += UpdateChestInfoText;
        UIMediator.OnPlayerChestEnded += HideChestInfoText;

    }
    
    private void OnDisable()
    {
        UIMediator.OnPlayerChestOpened -= UpdateChestInfoText;
        UIMediator.OnPlayerChestEnded -= HideChestInfoText;
    }
    
    private void UpdateChestInfoText(int rewardId)
    {
        
        if (rewardId == (int)SkillType.StoneGuard)
        {
            int val = RemoteConfigManager.Instance.GetRewardShield();
            chestInfoText.text = $"You gain {val} shields!";
        }

        else if (rewardId == (int)SkillType.SoulReaver)
        {
            int val = RemoteConfigManager.Instance.GetStolenHealth();
            chestInfoText.text = $"You stole {val} souls!";
        }

        else
        {
            chestInfoText.text = tileDatabase.allSkills[rewardId].callToAction;
        }
        // 3. Make the object visible
        chestInfoText.gameObject.SetActive(true);
    }
    
    // This is the function that will be called after the delay
    // It must be public or private, but it MUST take zero arguments.
    private void HideChestInfoText()
    {
        // Hide the text object
        chestInfoText.gameObject.SetActive(false);
    }
    
}