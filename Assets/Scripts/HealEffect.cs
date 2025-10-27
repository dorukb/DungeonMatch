using UnityEngine;

namespace DorkyProductions
{
    public class HealEffect
    {
        public static int GetHeal(int matchedTileCount)
        {
            if (matchedTileCount == 3)
            {
                return 3;
            }
            else if (matchedTileCount == 4)
            {
                return 5;
            }
            else if (matchedTileCount == 5)
            {
                return 8;
            }
            Debug.LogError($"Matched tile count is incorrect: {matchedTileCount}");
            return 3;
        }
    }
}