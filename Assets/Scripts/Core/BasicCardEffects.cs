using UnityEngine;

namespace DorkyProductions
{
    public static class BasicCardEffects
    {
        public static int GetDamage(int matchedTileCount)
        {
            if (matchedTileCount == 3)
            {
                return 3;
            }
            else if (matchedTileCount == 4)
            {
                return 4;
            }
            else if (matchedTileCount == 5)
            {
                return 6;
            }
            Debug.LogError($"Matched tile count is incorrect: {matchedTileCount}");
            return 3;
        }
        public static int GetHeal(int matchedTileCount)
        {
            if (matchedTileCount == 3)
            {
                return 3;
            }
            else if (matchedTileCount == 4)
            {
                return 4;
            }
            else if (matchedTileCount == 5)
            {
                return 6;
            }
            Debug.LogError($"Matched tile count is incorrect: {matchedTileCount}");
            return 3;
        }
        public static int GetShield(int matchedTileCount)
        {
            if (matchedTileCount == 3)
            {
                return 3;
            }
            else if (matchedTileCount == 4)
            {
                return 4;
            }
            else if (matchedTileCount == 5)
            {
                return 6;
            }
            Debug.LogError($"Matched tile count is incorrect: {matchedTileCount}");
            return 3;
        }
        public static float GetCrossMultiplier(int matchedTileCount)
        {
            if (matchedTileCount == 3)
            {
                return 2f;
            }
            else if (matchedTileCount == 4)
            {
                return 2.25f;
            }
            else if (matchedTileCount == 5)
            {
                return 2.5f;
            }
            Debug.LogError($"Matched tile count is not in [3-5]: {matchedTileCount}");
            return 2f;
        }
    }
}