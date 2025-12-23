using System.Collections.Generic;
using UnityEngine;

namespace DorkyProductions
{
    public class SpawnController
    {
        private static int _specialTokens = 0;
        private static int _tokenThreshold = 50;
        public static Queue<Tile> _spawnQueue = new Queue<Tile>();
        

        public static void AddToken(int matchSize)
        {
            _specialTokens += (matchSize * 2); // 3-match = 6 tokens, 5-match = 10 tokens

            if (_specialTokens >= _tokenThreshold) {
                // When threshold is hit, decide which one to spawn
                // Using 50/50 chance makes them perfectly equal
                Tile nextSpecial = (Random.value < 0.5f) ? Tile.Cross : Tile.Chest;
        
                _spawnQueue.Enqueue(nextSpecial);
                _specialTokens = 0; // Reset
            }
        }
    }
}