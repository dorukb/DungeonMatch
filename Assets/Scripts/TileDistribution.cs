using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

namespace DorkyProductions.UI
{
    public class TileDistribution
    {
        //Scaling constant
        private static int _k = 50;
        
        private static readonly Dictionary<Tile, float> _targetPercentages = new Dictionary<Tile, float>
        {
            { Tile.Attack, 0.10f }, { Tile.Heal, 0.10f }, { Tile.Shield, 0.10f }, { Tile.Cross, 0.35f }, { Tile.Chest, 0.35f }
        };

        private static readonly Dictionary<Tile, int> _currentTileCounts = new Dictionary<Tile, int>
        {
            { Tile.Attack, 0 }, { Tile.Heal, 0 }, { Tile.Shield, 0 }, { Tile.Cross, 0 }, { Tile.Chest, 0 }
        };

        private static readonly Random Rng = new Random();
        
        public static Dictionary<Tile, double> CalculateDynamicWeights()
        {
            var dynamicWeights = new Dictionary<Tile, double>();

            int boardSize = GameBoard.BoardWidth * GameBoard.BoardHeight;
            
            foreach (var tile in _targetPercentages)
            {
                Tile tileType = tile.Key;
                
                double baseWeight = tile.Value;

                if (!_targetPercentages.TryGetValue(tileType, out float t_i))
                {
                    Debug.LogError("Tile type exists but there is no target percentage for it!");
                    t_i = 0.0f;
                }

                if (!_currentTileCounts.TryGetValue(tileType, out int c_i))
                {
                    c_i = 0; // tile isnt tracked yet
                }
                
                //current density d_i = c_i/boardSize
                double d_i = (double)c_i / boardSize;
                
                //calculate delta based on board state i.e. current tile densities
                //Delta = k * (t_i - d_i)
                double deltaBoardState = _k * (t_i - d_i);
                
                //calculate dynamic weight
                //w_(d,i) = w_(base, i) + deltaBoardState_i
                double weightDynamicI = baseWeight + deltaBoardState;
                dynamicWeights[tileType] = Math.Max(0.0, weightDynamicI);
            }
            
            return dynamicWeights;
        }

        public static Dictionary<Tile, double> GetDynamicWeightsForAllowedTypes(List<Tile> allowedTypes)
        {
            var allowedWeights = new Dictionary<Tile, double>();
    
            foreach (var type in allowedTypes)
            {
                // Use the existing logic to calculate the dynamic weight for this type
                double weight = GetDynamicWeight(type); 
                allowedWeights[type] = weight;
                // Debug.Log($"weight: {weight} for allowed type:{type}");
            }
            return allowedWeights;
        }

        public static double GetDynamicWeight(Tile type)
        {
            if (!_targetPercentages.ContainsKey(type))
            {
                Debug.LogError("tile type Not found in baseWeights");
            }
    
            float baseWeight = _targetPercentages[type];
    
            // 1. Calculate Total Counts
            int totalTiles = _currentTileCounts.Values.Sum();
            if (totalTiles == 0 || _currentTileCounts[type] == 0)
            {
                // If the board is empty or there is no tile for given type yet
                // use the base weight directly (no balancing needed yet)
                return baseWeight;
            }
            
            // The target probability/density (P_target)
            double targetDensity = _targetPercentages[type];
    
            // The current actual probability/density (P_current)
            double currentDensity = (double)_currentTileCounts[type] / totalTiles; 
            // 3. Calculate Adjustment Factor (A)
            // If currentDensity > targetDensity, this factor will be less than 1 (weight reduced).
            // If currentDensity < targetDensity, this factor will be greater than 1 (weight increased).
            double adjustmentFactor = targetDensity / currentDensity;
            double dynamicWeight = baseWeight * adjustmentFactor;
            // Ensure the weight is not negative (though highly unlikely with a small K_FACTOR)
            return Math.Max(0.001f, dynamicWeight); 
        }

        public static Tile SelectTileFromWeights(Dictionary<Tile, double> weights)
        {
            double total = weights.Values.Sum();
            if (total <= 0) 
            {
                Debug.LogError($"weight total is {total}. should be > 0");
            }
    
            double random = Rng.NextDouble() * total; // Assuming Rng.NextFloat() or similar
            double cumulativeWeight = 0;
            
            foreach (var kvp in weights)
            {
                cumulativeWeight += kvp.Value;
                if (random < cumulativeWeight)
                {
                    return kvp.Key;
                }
            }
            Debug.LogWarning("Cumulative weighting did not work, selecting Attack Tile as fallback option.");
            return Tile.Attack; // Should not happen
        }
        
        public static void TileAdded(Tile type)
        {
            if (_currentTileCounts.ContainsKey(type))
            {
                _currentTileCounts[type]++;
            }
        }

        public static void TileRemoved(Tile type)
        {
            if (_currentTileCounts.ContainsKey(type) && _currentTileCounts[type] > 0)
            {
                _currentTileCounts[type]--;
            }
        }

        /// <summary>
        /// Calculates the current density (D_i) for a specific tile type.
        /// </summary>
        private static float GetCurrentDensity(Tile type)
        {
            _currentTileCounts.TryGetValue(type, out int count);
            int boardSize = GameBoard.BoardWidth * GameBoard.BoardHeight;
            return (float)count / boardSize;
        }

        public static void LogBoardDensity()
        {
            foreach (Tile t in  Enum.GetValues(typeof(Tile)))
            {
                var density = GetCurrentDensity(t);
                Debug.Log($"{t} density is:  {density}");
            }
        } 
        
        public static List<Tile> GetAllPossibleTileTypes()
        {
            return Enum.GetValues(typeof(Tile))
                .Cast<Tile>()
                // Filter: We convert the enum value to its integer representation (index)
                // and keep only those whose index is greater than 0.
                //since type 0 is "unknown"
                .Where(t => (int)t > 0) 
                .ToList();
        }
    }
}