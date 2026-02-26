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
    }
}