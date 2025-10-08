using System;
using System.Collections.Generic;
using DorkyProductions.Algorithms;
using UnityEngine;

namespace DorkyProductions.Views
{
    public class CaptureGroupSelectionView : MonoBehaviour
    {
        private Action<Guid, MatchGroup, int> _selectedCallback;
        private List<MatchGroup> _captureGroups;
        private Guid _playedCardGuid = Guid.Empty;
        private int _playerID;
        public void Show(Guid playedCardID, List<MatchGroup> groups, int playerID, Action<Guid, MatchGroup, int> selectedCallback)
        {
            this._captureGroups = groups;
            this._selectedCallback = selectedCallback;
            this._playedCardGuid = playedCardID;
            this._playerID = playerID;
            
            // TODO: actually display some groups here. and wait for their button(?) to call SelectGroup.
            SelectGroup(0);
        }

        public void SelectGroup(int idx)
        { 
            _selectedCallback.Invoke(_playedCardGuid, _captureGroups[idx], _playerID);
        }
        
    }
}