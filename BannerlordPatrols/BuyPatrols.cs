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
        List<MobileParty> allPatrols = new List<MobileParty>();

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
            try
            {
                AddPatrolDialog(obj);
            } catch (Exception e)
            {
                MessageBox.Show("Something screwed up in adding patrol dialog. " + e.ToString());
            }
            
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionLaunched));
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, new Action(this.PatrolHourlyAi));
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

            obj.AddGameMenuOption("basilpatrol_pay_menu", "basilpatrol_pay_small", "Pay {BASILPATROL_SMALL_COST} for a small patrol",
                (MenuCallbackArgs args) =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Recruit;
                    PatrolProperties patrolProperties;
                    string settlementID = Settlement.CurrentSettlement.StringId;
                    settlementPatrolProperties.TryGetValue(settlementID, out patrolProperties);
                    int cost = patrolProperties.getPatrolCost();
                    MBTextManager.SetTextVariable("BASILPATROL_SMALL_COST", cost, false);
                    //MBTextManager.SetTextVariable("BASILPATROL_COST", patrolProperties.getPatrolCost, false);
                    if (cost >= Hero.MainHero.Gold || patrolProperties.getPatrolCount() >= 3)
                    {
                        return false;
                    }
                    return true;
                },
                (MenuCallbackArgs args) =>
                {
                    PatrolProperties patrolProperties;
                    string settlementID = Settlement.CurrentSettlement.StringId;
                    settlementPatrolProperties.TryGetValue(settlementID, out patrolProperties);
                    int cost = patrolProperties.getPatrolCost();
                    if (cost <= Hero.MainHero.Gold)
                    {
                        GiveGoldAction.ApplyForCharacterToSettlement(Hero.MainHero, Settlement.CurrentSettlement, cost);
                        MobileParty party = spawnPatrol(Settlement.CurrentSettlement, 18);
                        patrolProperties.patrols.Add(party);
                        settlementPatrolProperties[settlementID] = patrolProperties;
                        allPatrols.Add(party);
                    }
                    GameMenu.SwitchToMenu("village");
                });

            obj.AddGameMenuOption("basilpatrol_pay_menu", "basilpatrol_pay_medium", "Pay {BASILPATROL_MEDIUM_COST} for a medium patrol",
                (MenuCallbackArgs args) =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Recruit;
                    PatrolProperties patrolProperties;
                    string settlementID = Settlement.CurrentSettlement.StringId;
                    settlementPatrolProperties.TryGetValue(settlementID, out patrolProperties);
                    int cost = patrolProperties.getPatrolCost() * 4;
                    MBTextManager.SetTextVariable("BASILPATROL_MEDIUM_COST", cost, false);
                    //MBTextManager.SetTextVariable("BASILPATROL_COST", patrolProperties.getPatrolCost, false);
                    if (cost >= Hero.MainHero.Gold || patrolProperties.getPatrolCount() >= 3)
                    {
                        return false;
                    }
                    return true;
                },
                (MenuCallbackArgs args) =>
                {
                    PatrolProperties patrolProperties;
                    string settlementID = Settlement.CurrentSettlement.StringId;
                    settlementPatrolProperties.TryGetValue(settlementID, out patrolProperties);
                    int cost = patrolProperties.getPatrolCost() * 4;
                    if (cost <= Hero.MainHero.Gold)
                    {
                        GiveGoldAction.ApplyForCharacterToSettlement(Hero.MainHero, Settlement.CurrentSettlement, cost);
                        MobileParty party = spawnPatrol(Settlement.CurrentSettlement, 36);
                        patrolProperties.patrols.Add(party);
                        settlementPatrolProperties[settlementID] = patrolProperties;
                        allPatrols.Add(party);
                    }
                    GameMenu.SwitchToMenu("village");
                });

            obj.AddGameMenuOption("basilpatrol_pay_menu", "basilpatrol_pay_large", "Pay {BASILPATROL_LARGE_COST} for a large patrol",
                (MenuCallbackArgs args) =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Recruit;
                    PatrolProperties patrolProperties;
                    string settlementID = Settlement.CurrentSettlement.StringId;
                    settlementPatrolProperties.TryGetValue(settlementID, out patrolProperties);
                    int cost = patrolProperties.getPatrolCost() * 8;
                    MBTextManager.SetTextVariable("BASILPATROL_LARGE_COST", cost, false);
                    //MBTextManager.SetTextVariable("BASILPATROL_COST", patrolProperties.getPatrolCost, false);
                    if (cost >= Hero.MainHero.Gold || patrolProperties.getPatrolCount() >= 3)
                    {
                        return false;
                    }
                    return true;
                },
                (MenuCallbackArgs args) =>
                {
                    PatrolProperties patrolProperties;
                    string settlementID = Settlement.CurrentSettlement.StringId;
                    settlementPatrolProperties.TryGetValue(settlementID, out patrolProperties);
                    int cost = patrolProperties.getPatrolCost() * 8;
                    if (cost <= Hero.MainHero.Gold)
                    {
                        GiveGoldAction.ApplyForCharacterToSettlement(Hero.MainHero, Settlement.CurrentSettlement, cost);
                        MobileParty party = spawnPatrol(Settlement.CurrentSettlement, 64);
                        patrolProperties.patrols.Add(party);
                        settlementPatrolProperties[settlementID] = patrolProperties;
                        allPatrols.Add(party);
                    }
                    GameMenu.SwitchToMenu("village");
                });

            obj.AddGameMenuOption("basilpatrol_pay_menu", "basilpatrol_leave", "Leave", game_menu_just_add_leave_conditional, game_menu_switch_to_village_menu);
        }

        public void AddPatrolDialog(CampaignGameStarter obj)
        {
            obj.AddDialogLine("mod_buypatrols_talk_start", "start", "mod_buypatrols_talk", "My lord! I have nothing report at this time.", new ConversationSentence.OnConditionDelegate(this.patrol_talk_start_on_conditional), null, 100, null);
            obj.AddPlayerLine("mod_buy_patrols_leave", "mod_buypatrols_talk", "close_window", "Carry on, then. Farewell.", null, new ConversationSentence.OnConsequenceDelegate(this.conversation_patrol_leave_on_consequence), 100, null, null);
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

        private bool patrol_talk_start_on_conditional()
        {
            PartyBase encounteredParty = PlayerEncounter.EncounteredParty;
            if (PlayerEncounter.Current != null && Campaign.Current.CurrentConversationContext == ConversationContext.PartyEncounter && encounteredParty.IsMobile && allPatrols.Contains(encounteredParty.MobileParty))
            {
                // ? idk
            }
                return true;
        }

        #endregion

        #region Consequences

        private void game_menu_switch_to_village_menu(MenuCallbackArgs args)
        {
            GameMenu.SwitchToMenu("village");
        }

        private void conversation_patrol_leave_on_consequence()
        {
            PlayerEncounter.LeaveEncounter = true;
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
        public MobileParty spawnPatrol(Settlement settlement, int amount)
        {
            PartyTemplateObject partyTemplate = settlement.Culture.DefaultPartyTemplate;
            int numberOfCreated = partyTemplate.NumberOfCreated;
            partyTemplate.IncrementNumberOfCreated();
            MobileParty mobileParty = MBObjectManager.Instance.CreateObject<MobileParty>(settlement.OwnerClan.StringId + "_" + numberOfCreated);

            TextObject textObject;
            textObject = new TextObject("{BASILPATROL_SETTLEMENT_NAME} Patrol", null);
            textObject.SetTextVariable("BASILPATROL_SETTLEMENT_NAME", settlement.Name);
            mobileParty.InitializeMobileParty(textObject, partyTemplate, settlement.GatePosition, 0, 0, 0, rand.Next((int)(amount*0.9), amount + 1));
            InitPatrolParty(mobileParty, textObject, settlement.OwnerClan, settlement);
            mobileParty.Aggressiveness = 1f;
            mobileParty.SetMovePatrolAroundSettlement(settlement);
            return mobileParty;
        }

        public MobileParty spawnPatrol(Settlement settlement, Clan clan, int amount)
        {
            PartyTemplateObject partyTemplate = clan.Culture.DefaultPartyTemplate;
            int numberOfCreated = partyTemplate.NumberOfCreated;
            partyTemplate.IncrementNumberOfCreated();
            MobileParty mobileParty = MBObjectManager.Instance.CreateObject<MobileParty>(clan.StringId + "_" + numberOfCreated);

            TextObject textObject;
            textObject = new TextObject("{BASILPATROL_SETTLEMENT_NAME} Patrol", null);
            textObject.SetTextVariable("BASILPATROL_SETTLEMENT_NAME", settlement.Name);
            mobileParty.InitializeMobileParty(textObject, partyTemplate, settlement.GatePosition, 0, 0, 0, rand.Next((int)(amount * 0.9), amount + 1));
            InitPatrolParty(mobileParty, textObject, clan, settlement);
            mobileParty.Aggressiveness = 1f;
            mobileParty.SetMovePatrolAroundSettlement(settlement);
            return mobileParty;
        }

        public void PatrolHourlyAi()
        {
            PatrolProperties patrolProperties;
            foreach(Settlement settlement in Settlement.All)
            {
                if (settlement.IsVillage)
                {
                    settlementPatrolProperties.TryGetValue(settlement.StringId, out patrolProperties);
                    foreach (MobileParty patrol in patrolProperties.patrols)
                    {
                        if (patrol.DefaultBehavior == AiBehavior.EngageParty || patrol.DefaultBehavior == AiBehavior.FleeToPoint)
                        {
                            continue;
                        }
                        patrol.SetMovePatrolAroundSettlement(patrolProperties.getSettlement());
                    }
                    settlementPatrolProperties[settlement.StringId] = patrolProperties;
                }
            }
        }
        public void PatrolDailyAi()
        {
            PatrolProperties patrolProperties;
            foreach (Settlement settlement in Settlement.All)
            {
                if(settlement.IsVillage)
                {
                    settlementPatrolProperties.TryGetValue(settlement.StringId, out patrolProperties);
                    patrolProperties.patrols.RemoveAll(x => x.MemberRoster.Count == 0);
                    allPatrols.Clear();
                    foreach (MobileParty patrol in patrolProperties.patrols)
                    {

                        if (patrol.Food <= 10)
                        {
                            generateFood(patrol);
                        }
                        if (patrol.PrisonRoster.Count > patrol.MemberRoster.Count / 4)
                        {
                            SellPrisonersAction.ApplyForAllPrisoners(patrol, patrol.PrisonRoster, patrol.HomeSettlement, false);
                            GiveGoldAction.ApplyForPartyToSettlement(patrol.Party, patrol.HomeSettlement, patrol.PartyTradeGold, true);
                        }
                    }
                    allPatrols.AddRange(patrolProperties.patrols);
                    settlementPatrolProperties[settlement.StringId] = patrolProperties;
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
            dataStore.SyncData("allPatrols", ref allPatrols);
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
