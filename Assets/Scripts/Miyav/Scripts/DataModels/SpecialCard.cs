namespace DorkyProductions
{
    public class SpecialCard
    {
        public SpecialCardData data { get; private set; }
        public IEffect effect { get; set; }
        
        public SpecialCard(SpecialCardData data)
        {
            this.data = data;
            this.effect = null;
        }
        public SpecialCard(SpecialCardData data, IEffect effect)
        {
            this.data = data;
            this.effect = effect;
        }
        
    }
    
}