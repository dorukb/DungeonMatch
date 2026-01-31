using UnityEngine;

namespace DorkyProductions
{
    public class PlayerIdentity
    {
        public static string GetGuestID()
        {
            // Check if we already have an ID saved
            string guestID = PlayerPrefs.GetString("GuestID", string.Empty);

            if (string.IsNullOrEmpty(guestID))
            {
                // First time playing: Create a new unique ID
                guestID = System.Guid.NewGuid().ToString();
                PlayerPrefs.SetString("GuestID", guestID);
                PlayerPrefs.Save();
            }
            return guestID;
        }
        
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