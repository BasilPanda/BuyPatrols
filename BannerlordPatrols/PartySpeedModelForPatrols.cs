using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.GameComponents.Map;

namespace BuyPatrols
{
    public class PartySpeedModelForPatrols : DefaultPartySpeedCalculatingModel
    {
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
        }
    }
}
