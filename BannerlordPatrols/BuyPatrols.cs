using System;
using System.Xml;
using System.Configuration;
using System.Collections.Generic;
using TaleWorlds.Localization;
using TaleWorlds.CampaignSystem;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Actions;
using System.Windows.Forms;

namespace BuyPatrols
{
    public class BuyPatrols : CampaignBehaviorBase
    {
        Dictionary<string, PatrolProperties> settlementPatrolProperties = new Dictionary<string, PatrolProperties>();
        Random rand = new Random();

        private void OnSessionLaunched(CampaignGameStarter obj)
        {
            trackPatrols();
            try
            {
                AddPatrolMenu(obj);
            } catch (Exception e)
            {
                MessageBox.Show("Something screwed up in adding patrol menu. " + e.ToString());
            }
            
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionLaunched));
            //CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, new Action(this.PatrolHourlyAi));
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, new Action(this.PatrolDailyAi));
        }

        public void AddPatrolMenu(CampaignGameStarter obj)
        {
            obj.AddGameMenuOption("village", "basilpatrol_hire_patrol", "Hire a patrol",
                (MenuCallbackArgs args) =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Recruit;
                    if (Settlement.CurrentSettlement.OwnerClan != Clan.PlayerClan)
                    {
                        return false;
                    }
                    return true;
                }, 
                (MenuCallbackArgs args) => { GameMenu.SwitchToMenu("basilpatrol_pay_menu"); }, false, 4);

            obj.AddGameMenu("basilpatrol_pay_menu", "The village spokesman says they have the manpower but not the gear to send men on patrols. He tells he can send up to 3 patrols if you are willing to pay their gear.", null);

            obj.AddGameMenuOption("basilpatrol_pay_menu", "basilpatrol_pay_button", "Pay {BASILPATROL_COST}",
                (MenuCallbackArgs args) =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Recruit;
                    PatrolProperties patrolProperties;
                    settlementPatrolProperties.TryGetValue(Settlement.CurrentSettlement.StringId, out patrolProperties);
                    int cost = patrolProperties.getPatrolCost();
                    MBTextManager.SetTextVariable("BASILPATROL_COST", 1, false);
                    //MBTextManager.SetTextVariable("BASILPATROL_COST", patrolProperties.getPatrolCost, false);
                    if(1 >= Hero.MainHero.Gold && patrolProperties.getPatrolCount() > 3)
                    {
                        return false;
                    }
                    return true;
                },
                (MenuCallbackArgs args) =>
                {
                    PatrolProperties patrolProperties;
                    settlementPatrolProperties.TryGetValue(Settlement.CurrentSettlement.StringId, out patrolProperties);
                    int cost = patrolProperties.getPatrolCost();
                    if (1 <= Hero.MainHero.Gold)
                    {
                        GiveGoldAction.ApplyForCharacterToSettlement(Hero.MainHero, Settlement.CurrentSettlement, cost);
                        spawnPatrol(Settlement.CurrentSettlement);
                    }
                    GameMenu.SwitchToMenu("village");
                });

            obj.AddGameMenuOption("basilpatrol_pay_menu", "basilpatrol_leave", "Leave", game_menu_just_add_leave_conditional, game_menu_switch_to_village_menu);
        }

        #region Conditionals

        private bool game_menu_just_add_recruit_conditional(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Recruit;
            return true;
        }

        private bool game_menu_just_add_pay_conditional(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Trade;
            return true;
        }

        private bool game_menu_just_add_leave_conditional(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Leave;
            return true;
        }

        #endregion

        #region Consequences

        private void game_menu_switch_to_village_menu(MenuCallbackArgs args)
        {
            GameMenu.SwitchToMenu("village");
        }

        #endregion

        private void trackPatrols()
        {
            if(settlementPatrolProperties.Count == 0)
            {
                foreach(Settlement settlement in Settlement.All)
                {
                    if(settlement.IsVillage)
                    {
                        settlementPatrolProperties.Add(settlement.StringId, new PatrolProperties(settlement.StringId, new List<MobileParty>()));
                    }
                }
            }
        }

        //SpawnAPartyInFaction
        public void spawnPatrol(Settlement settlement)
        {
            PartyTemplateObject partyTemplate = settlement.Culture.MilitiaPartyTemplate;
            int numberOfCreated = partyTemplate.NumberOfCreated;
            partyTemplate.IncrementNumberOfCreated();
            MobileParty mobileParty = MBObjectManager.Instance.CreateObject<MobileParty>(settlement.OwnerClan.StringId + "_" + numberOfCreated);

            TextObject textObject;
            textObject = new TextObject("{BASILPATROL_SETTLEMENT_NAME} Patrol", null);
            textObject.SetTextVariable("BASILPATROL_SETTLEMENT_NAME", settlement.Name);
            mobileParty.InitializeMobileParty(textObject, partyTemplate, settlement.GatePosition, 0, 0, 0, 25);
            InitPatrolParty(mobileParty, textObject, settlement.OwnerClan, settlement);
            mobileParty.Aggressiveness = 1f;
            mobileParty.SetMovePatrolAroundSettlement(settlement);
        }

        public void spawnPatrol(Settlement settlement, Clan clan)
        {
            PartyTemplateObject partyTemplate = clan.Culture.MilitiaPartyTemplate;
            int numberOfCreated = partyTemplate.NumberOfCreated;
            partyTemplate.IncrementNumberOfCreated();
            MobileParty mobileParty = MBObjectManager.Instance.CreateObject<MobileParty>(clan.StringId + "_" + numberOfCreated);

            TextObject textObject;
            textObject = new TextObject("{BASILPATROL_SETTLEMENT_NAME} Patrol", null);
            textObject.SetTextVariable("BASILPATROL_SETTLEMENT_NAME", settlement.Name);
            mobileParty.InitializeMobileParty(textObject, partyTemplate, settlement.GatePosition, 0, 0, 0, 25);
            InitPatrolParty(mobileParty, textObject, clan, settlement);
            mobileParty.Aggressiveness = 1f;
            mobileParty.SetMovePatrolAroundSettlement(settlement);
        }

        public void PatrolHourlyAi()
        {
            PatrolProperties patrolProperties;
            foreach(string id in settlementPatrolProperties.Keys)
            {
                settlementPatrolProperties.TryGetValue(id, out patrolProperties);
                foreach(MobileParty patrol in patrolProperties.patrols)
                {
                    if(patrol.DefaultBehavior == AiBehavior.EngageParty || patrol.DefaultBehavior == AiBehavior.FleeToPoint)
                    {
                        continue;
                    }
                    patrol.SetMovePatrolAroundSettlement(patrolProperties.getSettlement());
                }
            }
        }
        public void PatrolDailyAi()
        {
            PatrolProperties patrolProperties;
            foreach (string id in settlementPatrolProperties.Keys)
            {
                settlementPatrolProperties.TryGetValue(id, out patrolProperties);
                foreach (MobileParty patrol in patrolProperties.patrols)
                {
                    if (patrol.Food <= 10)
                    {
                        generateFood(patrol);
                    }
                    if (patrol.MemberRoster.Count < 25 && rand.Next(0,2) == 1)
                    {
                        patrol.AddElementToMemberRoster(patrol.Party.Owner.Culture.MilitiaArcher, 1);
                        patrol.AddElementToMemberRoster(patrol.Party.Owner.Culture.MilitiaSpearman, 1);
                    }
                    if(patrol.PrisonRoster.Count > 0)
                    {
                        patrol.pr
                    }
                    if (patrol.PrisonRoster.Count > patrol.MemberRoster.Count / 4)
                    {
                        SellPrisonersAction.ApplyForAllPrisoners(patrol, patrol.PrisonRoster, patrol.HomeSettlement, false);
                        GiveGoldAction.ApplyForPartyToSettlement(patrol.Party, patrol.HomeSettlement, patrol.PartyTradeGold, true);
                    }
                }
            }
        }

        public void InitPatrolParty(MobileParty patrolParty, TextObject name, Clan faction, Settlement homeSettlement)
        {
            patrolParty.Name = name;
            patrolParty.HomeSettlement = homeSettlement;
            patrolParty.Party.Owner = faction.Leader;
            patrolParty.SetInititave(1f, 0.2f, 24);
            patrolParty.Party.Visuals.SetMapIconAsDirty();
            generateFood(patrolParty);
        }

        public void generateFood(MobileParty patrolParty)
        {
            foreach (ItemObject itemObject in ItemObject.All)
            {
                if (itemObject.IsFood)
                {
                    int foodAmount = MBRandom.RoundRandomized((float)patrolParty.MemberRoster.TotalManCount * (1f / (float)itemObject.Value) * (float)5 * MBRandom.RandomFloat * MBRandom.RandomFloat * MBRandom.RandomFloat * MBRandom.RandomFloat);
                    if (foodAmount > 0)
                    {
                        patrolParty.ItemRoster.AddToCounts(itemObject, foodAmount, true);
                    }
                }
            }
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("settlementPatrolProperties", ref settlementPatrolProperties);
        }
        
        public class BannerlordPatrolSaveDefiner : SaveableTypeDefiner
        {
            public BannerlordPatrolSaveDefiner() : base(91115129)
            {
            }

            protected override void DefineClassTypes()
            {
                AddClassDefinition(typeof(PatrolProperties), 1);
            }

            protected override void DefineContainerDefinitions()
            {
                ConstructContainerDefinition(typeof(Dictionary<string, PatrolProperties>));
            }
        }

    }
}
