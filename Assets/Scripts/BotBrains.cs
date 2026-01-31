namespace DorkyProductions
{
    using System.Collections.Generic;
    using UnityEngine;
    
    public interface IBotBrain
    {
        List<Vector2Int> FindSwap(GameBoard board);
    }
    
    public enum BotDifficulty { Easy = 0, Medium = 1, Hard = 2 }

    public static class BotBrain
    {
        private static BotDifficulty _difficulty = (BotDifficulty)RemoteConfigManager.Instance.GetDefaultBot();        
        
        //Brain Instances
        private static readonly IBotBrain _easyBotBrain = new EasyBotBrain();
        private static readonly IBotBrain _mediumBotBrain = new MediumBotBrain();
        private static readonly IBotBrain _hardBotBrain = new HardBotBrain();

        private static IBotBrain CurrentBrain
        {
            get
            {
                return _difficulty switch
                {
                    BotDifficulty.Medium => _mediumBotBrain,
                    BotDifficulty.Hard   => _hardBotBrain,
                    _                    => _easyBotBrain
                };
            }
        }
        
        public static BotDifficulty GetDifficulty() => _difficulty;

        public static void SetDifficulty(BotDifficulty difficultyLevel)
        {
            _difficulty = difficultyLevel;
        }
        
        // --- PUBLIC API ---

        public static List<Vector2Int> FindSwap(GameBoard board)
        {
            return CurrentBrain.FindSwap(board);
        }
        

    }

    public abstract class BaseBotBrain : IBotBrain
    {
        protected List<Vector2Int> RandomSwap(Vector2Int posA, Vector2Int posB, GameBoard board)
        {
            // Try up to 250 random combinations
            for (int i = 0; i < 250; i++)
            {
                // 1. Pick random tile
                posA = new Vector2Int(Random.Range(0, 5), Random.Range(0, 5));
                
                // 2. Pick a random neighbor (Up, Right, Down, Left)
                Vector2Int[] dirs = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
                posB = posA + dirs[Random.Range(0, 4)];

                // 3. Validation
                if (board.IsValidSwap(posA, posB))
                {
                    // This acts like a 'break' - it stops the loop and the method immediately
                    return new List<Vector2Int> { posA, posB };
                }
            }

            // 4. Emergency Fallback
            // If we tried 250 times and found nothing, return an empty list 
            // to prevent the game from freezing.
            Debug.LogWarning("Bot could not find any valid random moves!");
            return new List<Vector2Int>();
        }

        public abstract List<Vector2Int> FindSwap(GameBoard board);
        
    }
}