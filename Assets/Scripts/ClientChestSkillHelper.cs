using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace DorkyProductions.Skills
{
    public class ClientChestSkillHelper
    {
        //private readonly List<TileView> selectedTilesForPhantomMatch = new();
        //private readonly List<TileView> selectedTilesForPhaseShift = new();
        private readonly List<TileView> selectedTilesForSkill = new();
        
        private ClientBoardVisualizer _visualizer;
        
        public List<TileView> GetVisuals(bool isRow, Vector2Int gridPos)
        {
            List<TileView> views = new List<TileView>();
            // 1. Check if we already found it before
            // 2. If not, try to find it in the scene right now
            if (_visualizer == null)
            {
                _visualizer = Object.FindAnyObjectByType<ClientBoardVisualizer>();
            }
    
            // 3. Check if we actually found something
            if (_visualizer != null)
            {
                views = _visualizer.GetVisualTilesAtLine(isRow, gridPos);
            }
            else
            {
                Debug.LogError("ChestHelper: Visualizer not found in this scene!");
            }

            return views; 
        }
        
        // Returns: True if enough tiles(3) of same type are selected, false otherwise
        public bool OnNewTileSelected(TileView selectedTile, SkillType skill)
        {
            if (skill == SkillType.PhantomMatch)
            {
                if (selectedTilesForSkill.Contains(selectedTile))
                {
                    // TODO:call phantom_tab_1 or unselect?
                    AudioManager.Instance.PlaySFX(SFXType.PhantomMatchTap1);

                    selectedTilesForSkill.Remove(selectedTile);
                    selectedTile.SetSelected(false);
                }    
                else if (selectedTilesForSkill.Count == 0)
                {
                    // select this one.
                    AudioManager.Instance.PlaySFX(SFXType.PhantomMatchTap1);

                    selectedTile.SetSelected(true);
                    selectedTilesForSkill.Add(selectedTile);
                }
                else if (selectedTilesForSkill.FirstOrDefault().Type != selectedTile.Type)
                {
                    // if different from previous selected ones, remove the others, start a "new 3" with this.
                    // call phantom_tab_1

                    AudioManager.Instance.PlaySFX(SFXType.PhantomMatchTap1);

                    foreach (var tile in selectedTilesForSkill)
                    {
                        tile.SetSelected(false);
                    }
                    selectedTilesForSkill.Clear();
                    
                    selectedTile.SetSelected(true);
                    selectedTilesForSkill.Add(selectedTile);
                }
                else if (selectedTilesForSkill.Count == 1 &&
                         selectedTilesForSkill[0].Type == selectedTile.Type)
                {
                    // add 2nd tile to selescted list
                    AudioManager.Instance.PlaySFX(SFXType.PhantomMatchTap2);

                    selectedTile.SetSelected(true);
                    selectedTilesForSkill.Add(selectedTile);
                }
                else  // continue the batch.
                {
                    // 3rd tile
                    AudioManager.Instance.PlaySFX(SFXType.PhantomMatchTap3);
                    selectedTile.SetSelected(true);
                    selectedTilesForSkill.Add(selectedTile);
                }
                // when 3 are selected. either show OK/submit button, or automatically send the command.
                return selectedTilesForSkill.Count == 3;
            }

            if (skill == SkillType.PhaseShift)
            {
                if (selectedTilesForSkill.Contains(selectedTile))
                {
                    // TODO:call phase_ta_1 or unselect?
                    AudioManager.Instance.PlaySFX(SFXType.PhantomMatchTap1);

                    selectedTilesForSkill.Remove(selectedTile);
                    selectedTile.SetSelected(false);
                }
                else if (selectedTilesForSkill.Count == 0)
                {
                    // select this one.
                    //TODO:update the audio?
                    AudioManager.Instance.PlaySFX(SFXType.PhantomMatchTap1);

                    selectedTile.SetSelected(true);
                    selectedTilesForSkill.Add(selectedTile);
                }
                else
                {
                    // 2nd tile
                    //TODO: update audio?
                    AudioManager.Instance.PlaySFX(SFXType.PhantomMatchTap3);
                    selectedTile.SetSelected(true);
                    selectedTilesForSkill.Add(selectedTile);
                }
                // when 2 are selected. either show OK/submit button, or automatically send the command.
                
                return selectedTilesForSkill.Count == 2;
            }

            if (skill == SkillType.ArcaneSweep)
            {
                if (selectedTilesForSkill.Count == 0) //selecting a row
                {
                    selectedTilesForSkill.AddRange(GetVisuals(true, selectedTile.GridPosition));
                    Debug.Log($"selected line: {selectedTilesForSkill[2]}");
                    //SetLineSelected();
                }
                else
                {
                    Debug.Log("somehow managed to select another row. Shouldnt happen.");
                }

                return selectedTilesForSkill.Count == 5;
            }
            return false;
        }

        public List<Vector2Int> GetSelectedTilePositions()
        {
            return selectedTilesForSkill
            .Select(tv => tv.GridPosition)
            .ToList();
        }

        public void ClearSelectedTiles()
        {
            selectedTilesForSkill.Clear();
        }

        public void SetLineSelected()
        {
            foreach (TileView tile in selectedTilesForSkill)
            {
                tile.SetSelected(true);
            }
        }

        public void UnselectTiles()
        {
            foreach (TileView tile in selectedTilesForSkill)
            {
                tile.SetSelected(false);
            }
        }

    }
}