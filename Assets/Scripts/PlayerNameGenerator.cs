using UnityEngine;
using TMPro;
using System.Collections.Generic;
namespace DorkyProductions
{
    
/// <summary>
/// Generates a random dungeon-themed player name from a list of prefixes and suffixes.
/// Displays the name in a TextMeshProUGUI field and can be re-rolled via a button.
/// </summary>
public class PlayerNameGenerator : MonoBehaviour
{
    private static string _chosenName = "";
    public TextMeshProUGUI nameDisplayText;

    [Header("Masculine/Neutral Names")]
    [SerializeField]
    private List<string> malePrefixes = new List<string>()
    {
        "Grim", "Shadow", "Iron", "Blood", "Stone", "Dread", "Bone", "Rune", "Flame", "Void",
        "Fang", "Storm", "Skull", "Serpent", "Grave"
    };

    [SerializeField]
    private List<string> maleSuffixes = new List<string>()
    {
        "Beard", "Hand", "Shield", "More", "Heart", "Bane", "Fist", "Breaker", "Helm", "Song",
        "Tooth", "Caller", "Wound", "Stride", "Walker"
    };
    
    [Header("Feminine Names")]
    [SerializeField]
    private List<string> femalePrefixes = new List<string>()
    {
        "Luna", "Silver", "Willow", "Rose", "Ember", "Cinder", "Star", "Whisper", "Crystal", "Fae",
        "Thorn", "Ivy", "Night", "Mourn", "Silk"
    };

    [SerializeField]
    private List<string> femaleSuffixes = new List<string>()
    {
        "Shade", "Blossom", "Wind", "Song", "Fire", "Blade", "Moon", "Water", "Wing", "Spell",
        "Dream", "Dancer", "Gleam", "Fall", "Thorn"
    };


    void Start()
    {
        if (string.IsNullOrEmpty(_chosenName))
        {
            SetRandomName();
        }
        else
        {
            nameDisplayText.text = _chosenName;
        }
    }

    /// <summary>
    /// Generates a new random name and updates the display text & player name in GameMaster's cache..
    /// This public method should be linked to the "Reroll" button's OnClick event.
    /// </summary>
    public void SetRandomName()
    {
        AudioManager.Instance.PlaySFX(SFXType.DiceRoll);
        
        string generatedName = GenerateRandomName();
        nameDisplayText.text = generatedName;
        _chosenName = generatedName;
    }
    
    public static string GetChosenName()
    {
        // Returns the last name generated and stored by the generator.
        return _chosenName;
    }
    private string GenerateRandomName()
    {
        // Check if the text display has been assigned to avoid errors.
        if (nameDisplayText == null)
        {
            Debug.LogError("Name Display Text is not assigned in the Inspector!");
            return "";
        }

        string generatedName;

        if (Random.Range(0, 2) == 0)
        {
            string randomPrefix = malePrefixes[Random.Range(0, malePrefixes.Count)];
            string randomSuffix = maleSuffixes[Random.Range(0, maleSuffixes.Count)];
            
            generatedName = randomPrefix + " " + randomSuffix;
        }
        else
        {
            string randomPrefix = femalePrefixes[Random.Range(0, femalePrefixes.Count)];
            string randomSuffix = femaleSuffixes[Random.Range(0, femaleSuffixes.Count)];

            generatedName = randomPrefix + " " + randomSuffix;
        }
        
        return generatedName;
    }
    
}
}