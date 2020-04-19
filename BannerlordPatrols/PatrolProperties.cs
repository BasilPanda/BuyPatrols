using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.SaveSystem;
using System.Collections.Generic;

namespace BuyPatrols
{
    public class PatrolProperties
    {
        [SaveableField(1)]
        public string settlementID;

        [SaveableField(2)]
        public List<MobileParty> patrols;

        public PatrolProperties(string settlementID, List<MobileParty> patrols)
        {
            this.settlementID = settlementID;
            this.patrols = patrols;
        }

        public Settlement getSettlement()
        {
            return Settlement.Find(this.settlementID);
        }

        public int getProsperity()
        {
                return (int)Math.Ceiling(this.getSettlement().Prosperity);
        }

        public int getPatrolCount()
        {
                return patrols.Count;
        }

        public int getPatrolCost()
        {
            if (getSettlement().IsVillage)
            {
                return (int)getSettlement().Village.Hearth * 3;
            } else if (getSettlement().IsCastle)
            {
                return getProsperity() * 2;
            } else
            {
                return getProsperity() / 2;
            }
        }
    }
}
