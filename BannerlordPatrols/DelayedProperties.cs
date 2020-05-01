using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.SaveSystem;
using System.Collections.Generic;

namespace BuyPatrols
{
    public class DelayedProperties
    {
        [SaveableField(1)]
        public Settlement settlement;

        [SaveableField(2)]
        public int multiplier;

        [SaveableField(3)]
        public int days;

        public DelayedProperties(Settlement settlement, int multiplier, int days)
        {
            this.settlement = settlement;
            this.multiplier = multiplier;
            this.days = days;
        }
    }
}
