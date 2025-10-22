using DorkyProductions.Core;

namespace DorkyProductions
{
    public class HumanMiyavPlayer : MiyavPlayer
    {
        public HumanMiyavPlayer(int id, string displayName = "Player??")
        {
            this.id = id;
            this.DisplayName = displayName;
        }
        protected override void OnTurnStarted(Context context)
        {
            InteractableCard.OnTapped += PlayTheCard;
        }

        protected override void OnTurnEnded(Context context)
        {
            InteractableCard.OnTapped -= PlayTheCard;
        }
    }
}