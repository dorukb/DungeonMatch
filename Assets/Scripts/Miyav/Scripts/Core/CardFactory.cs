using System;
using DorkyProductions.Effects;

namespace DorkyProductions.Core
{
    public class CardFactory
    {
        public static CatCard CreateCatCard(CatCardBlueprint cardBlueprint)
        {
            return new CatCard(new CatCardData(cardBlueprint), new BasicCaptureEffect());
        }

        public static SpecialCard CreateSpecialCard(SpecialCardBlueprint cardBlueprint)
        {
            // Each special card has its own unique Effect.
            
            SpecialCard card = new SpecialCard(new SpecialCardData(cardBlueprint));
            
            switch (cardBlueprint.type)
            {
                case SpecialCardType.ShelterLockdown:
                    break;
                case SpecialCardType.Hiss:
                    break;
                case SpecialCardType.CardboardBox:
                    break;
                case SpecialCardType.NursingCat:
                    break;
                case SpecialCardType.CatCafe:
                    break;
                case SpecialCardType.LaserPointer:
                    break;
                case SpecialCardType.LookOut:
                    break;
                case SpecialCardType.Catnip:
                    break;
                case SpecialCardType.Sphynx:
                    break;
                case SpecialCardType.Siamese:
                    break;
                case SpecialCardType.MaineCoon:
                    break;
                case SpecialCardType.ScottishFold:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return card;
        }
    }
}