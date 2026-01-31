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
    }
}