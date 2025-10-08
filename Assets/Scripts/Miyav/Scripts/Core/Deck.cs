using System.Collections.Generic;

namespace DorkyProductions.Core
{
    public class Deck
    {
        public List<CatCard> cards {get; private set;} = new();

        public void Shuffle()
        {
            cards.Shuffle();
        }

        public CatCard Draw()
        {
            int drawIdx = cards.Count - 1;
            CatCard card = cards[drawIdx];
            cards.RemoveAt(drawIdx);
            return card;
        }
        public void AddOnTop(CatCard catCard)
        {
            cards.Add(catCard);
        }

        public void AddOnBottom(CatCard catCard)
        {
            cards.Insert(0, catCard);
        }
    }
}