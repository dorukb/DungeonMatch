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
                // TODO:call phantom_tab_1 or unselect?
                AudioManager.Instance.PlaySFX(SFXType.PhantomMatchTap1);

                selectedTilesForPhantomMatch.Remove(selectedTile);
                selectedTile.SetSelected(false);
            }    
            else if (selectedTilesForPhantomMatch.Count == 0)
            {
                // select this one.
                AudioManager.Instance.PlaySFX(SFXType.PhantomMatchTap1);

                selectedTile.SetSelected(true);
                selectedTilesForPhantomMatch.Add(selectedTile);
            }
            else if (selectedTilesForPhantomMatch.FirstOrDefault().Type != selectedTile.Type)
            {
                // if different from previous selected ones, remove the others, start a "new 3" with this.
                // call phantom_tab_1

                AudioManager.Instance.PlaySFX(SFXType.PhantomMatchTap1);

                foreach (var tile in selectedTilesForPhantomMatch)
                {
                    tile.SetSelected(false);
                }
                selectedTilesForPhantomMatch.Clear();
                
                selectedTile.SetSelected(true);
                selectedTilesForPhantomMatch.Add(selectedTile);
            }
            else if (selectedTilesForPhantomMatch.Count == 1 &&
                     selectedTilesForPhantomMatch[0].Type == selectedTile.Type)
            {
                // add 2nd tile to selescted list
                AudioManager.Instance.PlaySFX(SFXType.PhantomMatchTap2);

                selectedTile.SetSelected(true);
                selectedTilesForPhantomMatch.Add(selectedTile);
            }
            else  // continue the batch.
            {
                // 3rd tile
                AudioManager.Instance.PlaySFX(SFXType.PhantomMatchTap3);
                selectedTile.SetSelected(true);
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

        public void ClearSelectedTiles()
        {
            selectedTilesForPhantomMatch.Clear();
        }
    }
}