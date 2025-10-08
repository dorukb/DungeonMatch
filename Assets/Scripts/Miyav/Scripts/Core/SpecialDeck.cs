using System.Collections.Generic;

namespace DorkyProductions.Core
{
    public class SpecialDeck
    {
        public List<SpecialCard> cards { get; private set; } = new();

        public void Shuffle()
        {
            cards.Shuffle();
        }

        public void AddOnTop(SpecialCard catCard)
        {
            cards.Add(catCard);
        }

        public void AddOnBottom(SpecialCard catCard)
        {
            cards.Insert(0, catCard);
        }
    }
}