
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors;

namespace BuyPatrols
{
    [HarmonyPatch(typeof(MilitiasCampaignBehavior), "HourlyTick")]
    class IgnorePatrolsPatch
    {
        static bool Prefix(MilitiasCampaignBehavior __instance)
        {
            foreach (MobileParty mobileParty in MilitiasCampaignBehavior.MilitiaParties)
            {
                if (mobileParty.IsActive && !mobileParty.Name.Contains("Patrol"))
                {
                    CheckProvocation(mobileParty);
                    if (mobileParty.MapEvent == null && mobileParty.CurrentSettlement == null && mobileParty.HomeSettlement.GetComponent<Town>() != null)
                    {
                        mobileParty.SetMoveGoToSettlement(mobileParty.HomeSettlement);
                    }
                }
            }
            return false;
        }

        private static void CheckProvocation(MobileParty militia)
        {
            if (militia.MapEvent == null)
            {
                Settlement homeSettlement = militia.HomeSettlement;
                MobileParty mobileParty = null;
                foreach (MobileParty mobileParty2 in homeSettlement.Parties)
                {
                    if (FactionManager.IsAtWarAgainstFaction(mobileParty2.MapFaction, militia.MapFaction))
                    {
                        mobileParty = mobileParty2;
                    }
                }
                if (mobileParty != null && militia.Party.TotalStrength >= mobileParty.Party.TotalStrength)
                {
                    militia.SetMoveEngageParty(mobileParty);
                }
            }
        }

        /* trying transpiler il editing another time.
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            int findIndex = -1;
            bool edited = false;
            for(int i = 0; i < codes.Count; i++)
            {
                if(codes[i].opcode == OpCodes.Ldloc_1 && !edited)
                {
                    findIndex = i + 1;
                    
                }
            }
        }
        */
    }
}
