using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using System.Windows.Forms;
using HarmonyLib;
using TaleWorlds.Library;

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
                harmony.PatchAll();
                
            }
            catch (Exception ex)
            {
                FileLog.Log("Overall Patcher " + ex.Message);
            }
        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            InformationManager.DisplayMessage(new InformationMessage("BuyPatrols Loaded!", new Color(95/255, 1, 90/255)));
            if (Settings.Instance.NukeAllPatrols)
            {
                InformationManager.DisplayMessage(new InformationMessage("BuyPatrols WARNING: DESTROYING ALL PATROLS DAILY OPTION ENABLED.", new Color(1, 73/255, 73/255)));
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
                if (Settings.Instance.PatrolWagesHintBox)
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
