using UnityEngine;

namespace DorkyProductions
{
    public class PlayerLocalSave
    {
        public static bool HasSavedData()
        {
            // Returns true if "UserCoins" has ever been saved before
            return PlayerPrefs.HasKey("UserCoins");
        }
        
        public static void SaveCoins(int amount) {
            PlayerPrefs.SetInt("UserCoins", amount);
            PlayerPrefs.Save();
        }

        public static int GetSavedCoins() {
            if (!HasSavedData())
            {
                return RemoteConfigManager.Instance.GetStartingCoins();
            }
            else
            {
                return PlayerPrefs.GetInt("UserCoins", 0);
            }
        }
        
        public static bool HasCompletedTutorial()
        {
            // Returns true if the key exists and equals 1
            return PlayerPrefs.GetInt("HasCompletedTutorial", 0) == 1;
        }

        public static void SetTutorialCompleted()
        {
            PlayerPrefs.SetInt("HasCompletedTutorial", 1);
            PlayerPrefs.Save();
        }
    }
}