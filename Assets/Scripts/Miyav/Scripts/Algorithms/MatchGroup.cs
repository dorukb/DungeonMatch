using System;
using System.Collections.Generic;

namespace DorkyProductions.Algorithms
{
    // TODO: Unify this and MatchGroup
    public class MatchGroup
    {
        private readonly List<Guid> _matchingCardIDs = new();
        public void AddCard(Guid cardID)
        {
            _matchingCardIDs.Add(cardID);
        }

        public List<Guid> GetAllCards()
        {
            return _matchingCardIDs;
        }

        public override string ToString()
        {
            string result = "(";
            foreach (Guid cardID in _matchingCardIDs)
            {
                result += cardID + " , ";
            }
            return result + ")";
        }
    }
}