using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.GameComponents.Map;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace BuyPatrols
{
    public static class Util
    {
        public static DefaultMapDistanceModel d = new DefaultMapDistanceModel();

        public static Settlement FindClosestTown(MobileParty patrol)
        {
            Settlement settlement = null;
            float shortestDistance = float.MaxValue;
            if (patrol.Party.NumberOfPrisoners > 5)
            {
                IEnumerable<Settlement> playerSettlements = patrol.HomeSettlement.OwnerClan.Settlements;
                if (playerSettlements != null)
                {
                    foreach (Settlement s in playerSettlements)
                    {
                        if (s.IsTown || s.IsCastle)
                        {
                            if (d.GetDistance(patrol, s) < shortestDistance)
                            {
                                //InformationManager.DisplayMessage(new InformationMessage(new TextObject("New quickest @ " + d.GetDistance(patrol, s) + " set to " + s.Name, null).ToString()));
                                settlement = s;
                                shortestDistance = d.GetDistance(patrol, s);
                            }
                        }
                    }
                }
            }
            return settlement;
        }

    }
}
