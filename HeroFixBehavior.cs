using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace Heritage
{
    internal class HeroFixBehavior : CampaignBehaviorBase
    {
        public HeroFixBehavior()
        {
            PropertyObject FixEquipmentProperty = new PropertyObject("zenDzeeMods_fix_equipment");
            PropertyObject tmp = ZenDzeeCompatibilityHelper.RegisterPresumedObject(FixEquipmentProperty);
            if (tmp != null)
            {
                FixEquipmentProperty = tmp;
            }
            FixEquipmentProperty.Initialize(new TextObject("zenDzeeMods_fix_equipment"),
                new TextObject("Non-zero value - equipment is fixed."));

            HeroFixHelper.HeroFixEquipmentProperty = FixEquipmentProperty;
        }

        public override void SyncData(IDataStore dataStore)
        {
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnGivenBirthEvent.AddNonSerializedListener(this, OnGivenBirth);
            //CampaignEvents.DailyTickHeroEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        private void OnHeroGrows(Hero hero)
        {
            HeroFixHelper.FixHeroStats(hero);

            if (hero.Age > 6f
                && !hero.CharacterObject.IsOriginalCharacter)
            {
                HeroFixHelper.FixEquipment(hero);
            }
        }

        private static void OnGivenBirth(Hero mother, List<Hero> children, int arg3)
        {
            foreach (Hero child in children)
            {
                if (child.IsAlive)
                {
                    HeroFixHelper.FixHeroStats(child);

                    if (child.Mother.Occupation != Occupation.Lord && child.Father.Occupation == Occupation.Lord)
                    {
                        child.SetNewOccupation(Occupation.Lord);
                        child.Clan = child.Father.Clan;
                    }
                    else if (child.Mother.Occupation == Occupation.Lord && child.Father.Occupation != Occupation.Lord)
                    {
                        child.SetNewOccupation(Occupation.Lord);
                        child.Clan = child.Mother.Clan;
                    }
                }
            }
        }
    }
}
