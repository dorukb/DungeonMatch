namespace DorkyProductions
{
    using System.Collections.Generic;
    using UnityEngine;

    public interface IMatchStrategy
    {
        List<MatchResult> FindMatchesAfterSwap(GameBoard board, Vector2Int swapPos1, Vector2Int swapPos2);
        List<MatchResult> FindAllMatchesOnBoard(GameBoard board);
        List<MatchResult> FindMatchesAt(GameBoard board, Vector2Int pos);
        Tile GetBestMatchedType(GameBoard board, Vector2Int posA, Vector2Int posB);
    }

    public static class MatchAlgorithm
    {
        // Strategy Instances
        private static readonly IMatchStrategy _basicStrategy = new BasicMatchStrategy();
        private static readonly IMatchStrategy _adjacencyStrategy = new AdjacencyMatchStrategy();
        private static readonly IMatchStrategy _structuralStrategy = new StructuralMatchStrategy();

        private static IMatchStrategy CurrentStrategy
        {
            get
            {
                // 0 -> Basic
                // 1 -> Structural (Candy Crush T/L)
                // 2 -> Adjacency (Blob)
                // Default -> Basic
                int strategyIndex = RemoteConfigManager.Instance.GetMatchStrategyIdx();

                return strategyIndex switch
                {
                    1 => _structuralStrategy,
                    2 => _adjacencyStrategy,
                    _ => _basicStrategy
                };
            }
        }

        public static bool IsPosInBounds(Vector2Int pos)
        {
            return pos.x >= 0 && pos.x < GameBoard.BoardWidth &&
                   pos.y >= 0 && pos.y < GameBoard.BoardHeight;
        }

        // --- PUBLIC API ---

        public static List<MatchResult> FindMatchesAt(GameBoard board, Vector2Int pos)
        {
            return CurrentStrategy.FindMatchesAt(board, pos);
        }

        public static List<MatchResult> FindMatchesAfterSwap(GameBoard board, Vector2Int swapPos1, Vector2Int swapPos2)
        {
            return CurrentStrategy.FindMatchesAfterSwap(board, swapPos1, swapPos2);
        }

        public static List<MatchResult> FindAllMatchesOnBoardAlternative(GameBoard board)
        {
            return CurrentStrategy.FindAllMatchesOnBoard(board);
        }

        public static Tile GetBestMatchedType(GameBoard board, Vector2Int posA, Vector2Int posB)
        {
            return CurrentStrategy.GetBestMatchedType(board, posA, posB);
        }

        public static int GetPriorityWeight(Tile type)
        {
            return type switch
            {
                Tile.Cross => 1,
                Tile.Attack => 2,
                Tile.Chest => 3,
                Tile.Shield => 4,
                Tile.Heal => 5,
                _ => 100
            };
        }

        // --- SHARED UTILS (Exposed for Strategies) ---
        public static class SharedPredictionLogic
        {
            public static Tile Predict(IMatchStrategy strategy, GameBoard board, Vector2Int posA, Vector2Int posB)
            {
                int idxA = (posA.x * GameBoard.BoardHeight) + posA.y;
                int idxB = (posB.x * GameBoard.BoardHeight) + posB.y;

                var stateA = board.GetTileAt(posA);
                var stateB = board.GetTileAt(posB);

                // Fake Swap
                board.SetTileAtInternal(idxA, stateB);
                board.SetTileAtInternal(idxB, stateA);

                var matches = strategy.FindMatchesAfterSwap(board, posA, posB);

                Tile bestType = Tile.Unknown;
                int bestPriority = int.MaxValue;

                foreach (var match in matches)
                {
                    int currentPriority = MatchAlgorithm.GetPriorityWeight(match.tileType);
                    if (currentPriority < bestPriority)
                    {
                        bestPriority = currentPriority;
                        bestType = match.tileType;
                    }
                }

                // Undo Swap
                board.SetTileAtInternal(idxA, stateA);
                board.SetTileAtInternal(idxB, stateB);

                return bestType;
            }
        }
    }
    
    // Note: AdjacencyMatchStrategy and StructuralMatchStrategy 
    // remain as defined in the previous steps (omitted here to save space 
    // but they must be present in the file).
}