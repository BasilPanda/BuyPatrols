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
        public static TextObject patrolWord = new TextObject("{=modbp015}Patrol");

        static void Postfix(DefaultPartySpeedCalculatingModel __instance, MobileParty mobileParty, float baseSpeed, StatExplainer explanation, ref float __result)
        {
            if (mobileParty.Name.ToString().EndsWith(patrolWord.ToString()) && Settings.Instance.AddPatrolSpeedEnabled)
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
        
       /*
       public override float CalculateFinalSpeed(MobileParty mobileParty, float baseSpeed, StatExplainer explanation)
        {
            float speed = base.CalculateFinalSpeed(mobileParty, baseSpeed, explanation);
            if (mobileParty.Name.Contains("Patrol"))
            {
                try
                {
                    speed += float.Parse(Settings.LoadSetting("AddPatrolSpeed"));
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
            return speed;
        }*/
    }
}
