using System;

namespace DorkyProductions
{
    // Will be the complete runtime representation defined by the data.
    public class CatCard
    {
        public CatCardData data { get; private set; }
        public IEffect effect { get; private set; }
    
        public Guid id => data.id;
        public CatCard(CatCardData data, IEffect effect)
        {
            this.data = data;
            this.effect = effect;
        }

        public int GetValue()
        {
            return data.value;
        }
    }
}