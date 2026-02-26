using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChestOpenerUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI rewardText;
    [SerializeField] private TextMeshProUGUI rewardDescriptionText;
    [SerializeField] private Image rewardIcon;
    
    public Button useButton;

    public void Setup(string skillName, Sprite skillIcon, string skillDescription)  
    {
        this.rewardText.text = skillName;
        this.rewardIcon.sprite = skillIcon;
        this.rewardDescriptionText.text = skillDescription;
    }
    
}
