using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.GameComponents.Map;
using TaleWorlds.Localization;

namespace BuyPatrols
{
    [HarmonyPatch(typeof(DefaultPartySpeedCalculatingModel), "CalculateFinalSpeed")]
    public class PartySpeedModelForPatrols 
    {
        static void Postfix(DefaultPartySpeedCalculatingModel __instance, MobileParty mobileParty, float baseSpeed, StatExplainer explanation, ref float __result)
        {
            if (mobileParty.Name.Contains("{=modbp015}Patrol") && Settings.Instance.AddPatrolSpeedEnabled)
            {
                if (mobileParty.HomeSettlement.OwnerClan == Clan.PlayerClan)
                {
                    __result += Settings.Instance.AddPatrolSpeed;
                } else
                {
                    __result += Settings.Instance.AddPatrolSpeedForAi;
                }
            } 
        }
    }
}
