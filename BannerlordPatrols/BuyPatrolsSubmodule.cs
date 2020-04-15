using System;
using System.Reflection;
using System.IO;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using System.Windows.Forms;
using System.Text;
using HarmonyLib;

namespace BuyPatrols
{
    class BuyPatrolsSubmodule : MBSubModuleBase
    {

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            try
            {
                Harmony harmony = new Harmony("BuyPatrols");
                if (bool.Parse(Settings.LoadSetting("AddPatrolSpeedEnabled")))
                {
                    harmony.PatchAll();
                }
            }
            catch (Exception ex)
            {
                FileLog.Log("Overall Patcher " + ex.Message);
            }
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            if (!(game.GameType is Campaign))
                return;
            CampaignGameStarter gameInitializer = (CampaignGameStarter)gameStarterObject;

            try
            {
                gameInitializer.AddBehavior(new BuyPatrols());
                /*
                if (bool.Parse(Settings.LoadSetting("AddPatrolSpeedEnabled")))
                {
                    gameStarterObject.AddModel(new PartySpeedModelForPatrols());
                }
                */
                if(bool.Parse(Settings.LoadSetting("PatrolWagesHintBox")))
                {
                    gameStarterObject.AddModel(new CalculateClanExpensesForPatrols());
                }
            } catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }
    }
}
