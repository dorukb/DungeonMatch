using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace DorkyProductions.Skills
{
    public class ChestSkillHelper
    {
        private readonly List<TileView> selectedTilesForPhantomMatch = new();

        // Returns: True if enough tiles(3) of same type are selected, false otherwise
        public bool OnNewTileSelected(TileView selectedTile)
        {
            if (selectedTilesForPhantomMatch.Contains(selectedTile))
            {
                selectedTilesForPhantomMatch.Remove(selectedTile);
                selectedTile.OnSelectedStateChange(false);
            }    
            else if (selectedTilesForPhantomMatch.Count == 0)
            {
                // select this one.
                selectedTile.OnSelectedStateChange(true);
                selectedTilesForPhantomMatch.Add(selectedTile);
            }
            else if (selectedTilesForPhantomMatch.FirstOrDefault().Type != selectedTile.Type)
            {
                // if different from previous selected ones, remove the others, start a "new 3" with this.
                foreach (var tile in selectedTilesForPhantomMatch)
                {
                    tile.OnSelectedStateChange(false);
                }
                selectedTilesForPhantomMatch.Clear();
                
                selectedTile.OnSelectedStateChange(true);
                selectedTilesForPhantomMatch.Add(selectedTile);
            }
            // when 3 are selected. either show OK/submit button, or automatically send the command.
            return selectedTilesForPhantomMatch.Count == 3;
        }

        public List<Vector2Int> GetSelectedTilePositions()
        {
            return selectedTilesForPhantomMatch
            .Select(tv => tv.GridPosition)
            .ToList();
        }
    }
}