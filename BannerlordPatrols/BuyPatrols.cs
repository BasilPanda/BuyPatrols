using System;
using System.Collections.Generic;
using TaleWorlds.Localization;
using TaleWorlds.CampaignSystem;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.SaveSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Actions;
using System.Windows.Forms;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.CampaignSystem.SandBox.GameComponents.Map;
using TaleWorlds.Library;

namespace BuyPatrols
{
    public class BuyPatrols : CampaignBehaviorBase
    {
        Dictionary<string, PatrolProperties> settlementPatrolProperties = new Dictionary<string, PatrolProperties>();
        List<MobileParty> allPatrols = new List<MobileParty>(); // all Patrols
        List<MobileParty> playerPatrols = new List<MobileParty>(); // Player Patrols
        Random rand = new Random();
        MobilePartiesAroundPositionList partiesAroundPosition = new MobilePartiesAroundPositionList(32);
        DefaultMapDistanceModel d = new DefaultMapDistanceModel();
        
        #region Settings
        public bool TargetCaravans = Settings.Instance.TargetCaravans;
        public bool TargetVillagers = Settings.Instance.TargetVillagers;
        public bool ForceRegenPatrol = Settings.Instance.ForceRegenPatrol;
        public bool ForceTroopCapEnabled = Settings.Instance.ForceTroopCapEnabled;
        public bool NotifyNotableRelations = Settings.Instance.NotifyNotableRelations;
        public int TroopsPerPatrol = Settings.Instance.TroopsPerPatrol;
        public int BaseCost = Settings.Instance.BaseCost;
        public float DailyPatrolWageModifier = Settings.Instance.DailyPatrolWageModifier;
        public int PatrolTetherRange = Settings.Instance.PatrolTetherRange;
        public int MaxPatrolCountPerVillage = Settings.Instance.MaxPatrolCountPerVillage;
        public int MaxPatrolCountPerCastle = Settings.Instance.MaxPatrolCountPerCastle;
        public int MaxPatrolCountPerTown = Settings.Instance.MaxPatrolCountPerTown;
        public int RelationCap = Settings.Instance.RelationCap;
        public int MaxTotalPatrols = Settings.Instance.MaxTotalPatrols;
        public TextObject patrolWord = new TextObject("{=BUYPATROLS000}Patrol");
        #endregion

        private void OnSessionLaunched(CampaignGameStarter obj)
        {
            if (Settings.Instance.NukeAllPatrols)
            {
                InformationManager.DisplayMessage(new InformationMessage("{=BUYPATROLSWARNING}BuyPatrols WARNING: DESTROYING ALL PATROLS DAILY OPTION ENABLED.", new Color(255, 0, 0)));
            }
            TrackPatrols();
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
                AddWalledSettlementDialog(obj);
            } catch (Exception e)
            {
                MessageBox.Show("Something screwed up in adding patrol dialog. " + e.ToString());
            }
            
        }

        private void OnDailyTick()
        {
            PatrolDailyAi();
            if (!Settings.Instance.PatrolWagesHintBox)
            {
                PayPatrols();
            }
            if (Settings.Instance.IncreaseNotableRelations)
            {
                IncreaseRelations();
            }
            if(Settings.Instance.AiHirePatrols)
            {
                AiGeneratePatrols();
            }
            if (Settings.Instance.NukeAllPatrols)
            {
                AttemptToDestroyAllPatrols();
            }
        }
        
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionLaunched));
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, new Action(this.PatrolHourlyAi));
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, new Action(this.OnDailyTick));
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, new Action<MobileParty, Settlement, Hero>(this.OnSettlementEntered));
            CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, new Action<MobileParty, PartyBase>(this.NotifyDestroyedPatrol));
            CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, new Action<Settlement, bool, Hero, Hero, Hero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail>(this.OnSettlementOwnerChanged));
            
        }

        #region Dialog & Menus Stuff

        public void AddPatrolMenu(CampaignGameStarter obj)
        {
            try
            {
                obj.AddGameMenuOption("village", "basilpatrol_manage_patrol", "{=BUYPATROLSMENU001}Manage patrols",
                    (MenuCallbackArgs args) =>
                    {
                        args.optionLeaveType = GameMenuOption.LeaveType.Manage;
                        
                        if (Settlement.CurrentSettlement.OwnerClan != Clan.PlayerClan && MaxPatrolCountPerVillage != 0)
                        {
                            return false;
                        }
                        
                        return true;
                    },
                    (MenuCallbackArgs args) => { GameMenu.SwitchToMenu("basilpatrol_pay_menu"); }, false, 4);

                obj.AddGameMenu("basilpatrol_pay_menu", "{=BUYPATROLSMENU002}The {BASILPATROL_SETTLEMENT_TYPE} says they have the manpower but not the gear to " +
                    "send men on patrols. He tells he can send up to {BASILPATROL_MAX_PATROL_AMOUNT} patrols if you are willing to pay their " +
                    "gear. Your clan must own the village to hire patrols. Patrols will switch allegiance to the new clan owner if ownership changes. " +
                    "Disbanding patrols will only affect patrols that are not engaging an enemy. You can have a total of {BASILPATROLS_CAP_PATROLS} patrols in your clan. " +
                    "You have {BASILPATROLS_CAP_LEFT} patrols you can still hire for your clan.",
                    (MenuCallbackArgs args) =>
                    {
                        if (Settlement.CurrentSettlement.IsVillage)
                        {
                            MBTextManager.SetTextVariable("BASILPATROL_SETTLEMENT_TYPE", "{=BUYPATROLSMENU003}village spokesman", false);
                            MBTextManager.SetTextVariable("BASILPATROL_MAX_PATROL_AMOUNT", MaxPatrolCountPerVillage, false);
                        } else if(Settlement.CurrentSettlement.IsTown)
                        {
                            MBTextManager.SetTextVariable("BASILPATROL_SETTLEMENT_TYPE", "{=BUYPATROLSMENU004}castle sergeant", false);
                            MBTextManager.SetTextVariable("BASILPATROL_MAX_PATROL_AMOUNT", MaxPatrolCountPerTown, false);
                        }
                        else
                        {
                            MBTextManager.SetTextVariable("BASILPATROL_SETTLEMENT_TYPE", "{=BUYPATROLSMENU004}castle sergeant", false);
                            MBTextManager.SetTextVariable("BASILPATROL_MAX_PATROL_AMOUNT", MaxPatrolCountPerCastle, false);
                        }
                        MBTextManager.SetTextVariable("BASILPATROLS_CAP_PATROLS", MaxTotalPatrols, false);
                        MBTextManager.SetTextVariable("BASILPATROLS_CAP_LEFT", (MaxTotalPatrols - playerPatrols.Count).ToString(), false);
                    });

                #region Hiring

                obj.AddGameMenuOption("basilpatrol_pay_menu", "basilpatrol_pay_small", "{=BUYPATROLSMENU006}Pay for a small patrol ({BASILPATROL_SMALL_COST}{GOLD_ICON})",
                    (MenuCallbackArgs args) =>
                    {
                        try
                        {
                            args.optionLeaveType = GameMenuOption.LeaveType.Recruit;
                            PatrolProperties patrolProperties;
                            string settlementID = Settlement.CurrentSettlement.StringId;
                            settlementPatrolProperties.TryGetValue(settlementID, out patrolProperties);
                            int cost = BaseCost + patrolProperties.getPatrolCost();
                            MBTextManager.SetTextVariable("BASILPATROL_SMALL_COST", cost, false);
                            MBTextManager.SetTextVariable("GOLD_ICON", "<img src=\"Icons\\Coin@2x\">");
                            return ConditionalCheckOnSettlementMenuOption(Settlement.CurrentSettlement, cost, patrolProperties);
                        } catch(Exception e)
                        {
                            MessageBox.Show("Error in small button..." + e.ToString());
                            return false;
                        }
                        
                    },
                    (MenuCallbackArgs args) =>
                    {
                        try
                        {
                            PatrolProperties patrolProperties;
                            string settlementID = Settlement.CurrentSettlement.StringId;
                            settlementPatrolProperties.TryGetValue(settlementID, out patrolProperties);
                            int cost = BaseCost + patrolProperties.getPatrolCost();
                            if (AttemptAddPatrolToDictionary(Settlement.CurrentSettlement, patrolProperties, cost))
                            {
                                TextObject text = new TextObject("{=BUYPATROLSMENU005}You have hired a patrol at {BUYPATROLSCURRENTSETTLEMENT}.");
                                text.SetTextVariable("BUYPATROLSCURRENTSETTLEMENT", Settlement.CurrentSettlement.ToString());
                                InformationManager.DisplayMessage(new InformationMessage(text.ToString()));
                            }
                            GameMenu.SwitchToMenu("basilpatrol_pay_menu");
                            
                        } catch(Exception e)
                        {
                            MessageBox.Show("Error in small purchase..." + e.ToString());
                        }
                        
                    });

                obj.AddGameMenuOption("basilpatrol_pay_menu", "basilpatrol_pay_medium", "{=BUYPATROLSMENU007}Pay for a medium patrol ({BASILPATROL_MEDIUM_COST}{GOLD_ICON})",
                    (MenuCallbackArgs args) =>
                    {
                        try
                        {
                            args.optionLeaveType = GameMenuOption.LeaveType.Recruit;
                            PatrolProperties patrolProperties;
                            string settlementID = Settlement.CurrentSettlement.StringId;
                            settlementPatrolProperties.TryGetValue(settlementID, out patrolProperties);
                            int cost = (BaseCost + patrolProperties.getPatrolCost()) * 2;
                            MBTextManager.SetTextVariable("BASILPATROL_MEDIUM_COST", cost, false);
                            MBTextManager.SetTextVariable("GOLD_ICON", "<img src=\"Icons\\Coin@2x\">");
                            //MBTextManager.SetTextVariable("BASILPATROL_COST", patrolProperties.getPatrolCost, false);
                            return ConditionalCheckOnSettlementMenuOption(Settlement.CurrentSettlement, cost, patrolProperties);
                        } catch (Exception e)
                        {
                            MessageBox.Show("Error in medium button... " + e.ToString());
                            return false;
                        }
                        
                    },
                    (MenuCallbackArgs args) =>
                    {
                        try
                        {
                            PatrolProperties patrolProperties;
                            string settlementID = Settlement.CurrentSettlement.StringId;
                            settlementPatrolProperties.TryGetValue(settlementID, out patrolProperties);
                            int cost = (BaseCost + patrolProperties.getPatrolCost()) * 2;
                            if (AttemptAddPatrolToDictionary(Settlement.CurrentSettlement, patrolProperties, cost, 2))
                            {
                                TextObject text = new TextObject("{=BUYPATROLSMENU005}You have hired a patrol at {BUYPATROLSCURRENTSETTLEMENT}.");
                                text.SetTextVariable("BUYPATROLSCURRENTSETTLEMENT", Settlement.CurrentSettlement.ToString());
                                InformationManager.DisplayMessage(new InformationMessage(text.ToString()));
                            }
                            GameMenu.SwitchToMenu("basilpatrol_pay_menu");
                            
                        } catch (Exception e)
                        {
                            MessageBox.Show("Error in medium purchase... " + e.ToString());
                        }
                        
                    });

                obj.AddGameMenuOption("basilpatrol_pay_menu", "basilpatrol_pay_large", "{=BUYPATROLSMENU008}Pay for a large patrol ({BASILPATROL_LARGE_COST}{GOLD_ICON})",
                    (MenuCallbackArgs args) =>
                    {
                        try
                        {
                            args.optionLeaveType = GameMenuOption.LeaveType.Recruit;
                            PatrolProperties patrolProperties;
                            string settlementID = Settlement.CurrentSettlement.StringId;
                            settlementPatrolProperties.TryGetValue(settlementID, out patrolProperties);
                            int cost = (BaseCost + patrolProperties.getPatrolCost()) * 3;
                            MBTextManager.SetTextVariable("BASILPATROL_LARGE_COST", cost, false);
                            MBTextManager.SetTextVariable("GOLD_ICON", "<img src=\"Icons\\Coin@2x\">");
                            //MBTextManager.SetTextVariable("BASILPATROL_COST", patrolProperties.getPatrolCost, false);
                            return ConditionalCheckOnSettlementMenuOption(Settlement.CurrentSettlement, cost, patrolProperties);
                        }
                        catch(Exception e)
                        {
                            MessageBox.Show("Error in large button..." + e.ToString());
                            return false;
                        }
                        
                    },
                    (MenuCallbackArgs args) =>
                    {
                        try
                        {
                            PatrolProperties patrolProperties;
                            string settlementID = Settlement.CurrentSettlement.StringId;
                            settlementPatrolProperties.TryGetValue(settlementID, out patrolProperties);
                            int cost = (BaseCost + patrolProperties.getPatrolCost()) * 3;
                            if (AttemptAddPatrolToDictionary(Settlement.CurrentSettlement, patrolProperties, cost, 3))
                            {
                                TextObject text = new TextObject("{=BUYPATROLSMENU005}You have hired a patrol at {BUYPATROLSCURRENTSETTLEMENT}.");
                                text.SetTextVariable("BUYPATROLSCURRENTSETTLEMENT", Settlement.CurrentSettlement.ToString());
                                InformationManager.DisplayMessage(new InformationMessage(text.ToString()));
                            }
                            GameMenu.SwitchToMenu("basilpatrol_pay_menu");
                        }
                        catch(Exception e)
                        {
                            MessageBox.Show("Error in large purchase..." + e.ToString());
                        }
                        
                    });
                #endregion

                obj.AddGameMenuOption("basilpatrol_pay_menu", "basilpatrol_disband_all", "{=BUYPATROLSMENU009}Disband all patrols",
                    (MenuCallbackArgs args) =>
                    {
                        try
                        {
                            args.optionLeaveType = GameMenuOption.LeaveType.Manage;
                            PatrolProperties patrolProperties;
                            if (Settlement.CurrentSettlement.IsVillage || Settlement.CurrentSettlement.IsCastle || Settlement.CurrentSettlement.IsTown)
                            {
                                if (settlementPatrolProperties.TryGetValue(Settlement.CurrentSettlement.StringId, out patrolProperties))
                                {
                                    if (patrolProperties.patrols.Count > 0 && Settlement.CurrentSettlement.OwnerClan == Clan.PlayerClan)
                                    {
                                        return true;
                                    }
                                }
                            }
                            return false;
                        }
                        catch(Exception e)
                        {
                            MessageBox.Show("Error in disband option..." + e.ToString());
                            return false;
                        }
                        
                    }, (MenuCallbackArgs args) =>
                    {
                        try
                        {
                            PatrolProperties patrolProperties;
                            if (settlementPatrolProperties.TryGetValue(Settlement.CurrentSettlement.StringId, out patrolProperties))
                            {
                                if (patrolProperties.patrols.Count > 0)
                                {

                                    foreach (MobileParty patrol in patrolProperties.patrols.ToList())
                                    {
                                        if (!patrol.IsEngaging)
                                        {
                                            if (patrol.HomeSettlement.OwnerClan == Clan.PlayerClan)
                                            {
                                                playerPatrols.Remove(patrol);
                                            }
                                            //DisbandPartyAction.ApplyDisband(patrol);
                                            allPatrols.Remove(patrol);
                                            patrolProperties.patrols.Remove(patrol);
                                            patrol.RemoveParty();
                                        }
                                    }
                                    settlementPatrolProperties[Settlement.CurrentSettlement.StringId] = patrolProperties;
                                }
                            }
                            GameMenu.SwitchToMenu("basilpatrol_pay_menu");
                        }
                        catch(Exception e)
                        {
                            MessageBox.Show("Error in disbanding all patrols..." + e.ToString());
                        }
                        
                    });

                obj.AddGameMenuOption("basilpatrol_pay_menu", "basilpatrol_leave", "{=BUYPATROLSMENU010}Leave", game_menu_just_add_leave_conditional, game_menu_switch_to_main_menu);
            } catch(Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }

        public void AddWalledSettlementDialog(CampaignGameStarter obj)
        {
            obj.AddGameMenuOption("castle", "basilpatrol_castle_patrol", "{=BUYPATROLSMENU001}Manage patrols", (MenuCallbackArgs args) =>
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Manage;
                if (Settlement.CurrentSettlement.OwnerClan != Clan.PlayerClan && MaxPatrolCountPerCastle != 0)
                {
                    return false;
                }
                return true;
            },
            (MenuCallbackArgs args) => { GameMenu.SwitchToMenu("basilpatrol_pay_menu"); }, false, 4);

            obj.AddGameMenuOption("town_keep", "basilpatrol_town_patrol", "{=BUYPATROLSMENU001}Manage patrols", (MenuCallbackArgs args) =>
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Manage;
                if (Settlement.CurrentSettlement.OwnerClan != Clan.PlayerClan && MaxPatrolCountPerTown != 0)
                {
                    return false;
                }
                return true;
            },
            (MenuCallbackArgs args) => { GameMenu.SwitchToMenu("basilpatrol_pay_menu"); }, false, 6);
        }

        public void AddPatrolDialog(CampaignGameStarter obj)
        {
            #region Player Patrols

            obj.AddDialogLine("mod_buypatrols_talk_start", "start", "mod_buypatrols_talk", "{=BUYPATROLS001}Hello my lord. What do you need us to do?", new ConversationSentence.OnConditionDelegate(this.patrol_talk_start_on_conditional), null, 100, null);
            obj.AddPlayerLine("mod_buypatrols_donate_troops", "mod_buypatrols_talk", "mod_buypatrols_after_donate", "{=BUYPATROLS002}Donate Troops", null, new ConversationSentence.OnConsequenceDelegate(this.conversation_patrol_donate_troops_on_consequence), 100, null, null);
            obj.AddPlayerLine("mod_buypatrols_disband", "mod_buypatrols_talk", "close_window", "{=BUYPATROLS003}Disband.", null, new ConversationSentence.OnConsequenceDelegate(this.conversation_patrol_disband_on_consequence), 100, null, null);
            obj.AddPlayerLine("mod_buypatrols_leave", "mod_buypatrols_talk", "close_window", "{=BUYPATROLS004}Carry on. Farewell.", null, new ConversationSentence.OnConsequenceDelegate(this.conversation_patrol_leave_on_consequence), 100, null, null);
            obj.AddDialogLine("mod_buypatrols_after_donate", "mod_buypatrols_after_donate", "mod_buypatrols_talk", "{=BUYPATROLS005}Anything else?", null, null, 100, null);
            obj.AddPlayerLine("mod_leaderless_party_answer", "disbanding_leaderless_party_start_response", "close_window", "{=BUYPATROLS006}Disband now.", null, new ConversationSentence.OnConsequenceDelegate(this.conversation_patrol_disband_now_on_consequence), 100, null, null);

            #endregion

            #region Neutral Patrols

            obj.AddDialogLine("mod_buypatrols_talk_neutral_start", "start", "mod_buypatrols_neutral_talk", "{=BUYPATROLS007}We are patrolling around {BASILPATROL_AREA} under commands of {BASILPATROLS_LIEGE_0}. What do you want?", new ConversationSentence.OnConditionDelegate(this.patrol_talk_neutral_start_conditional), null, 100, null);
            obj.AddPlayerLine("mod_buypatrols_neutral_attack", "mod_buypatrols_neutral_talk", "mod_buypatrols_neutral_aggresive", "{=BUYPATROLS008}Surrender or die.", null, null, 100, null, null);
            obj.AddPlayerLine("mod_buypatrols_neutral_leave", "mod_buypatrols_neutral_talk", "close_window", "{=BUYPATROLS009}Nothing just passing by.", null, new ConversationSentence.OnConsequenceDelegate(this.conversation_patrol_leave_on_consequence), 100, null, null);
            obj.AddDialogLine("mod_buypatrols_neutral_aggresive", "mod_buypatrols_neutral_aggresive", "mod_buypatrols_neutral_aggresive_player_response", "{=BUYPATROLS010}What? You can't be serious?", null,null,100,null);
            obj.AddPlayerLine("mod_buypatrols_neutral_aggro_attack", "mod_buypatrols_neutral_aggresive_player_response", "close_window", "{=BUYPATROLS011}I didn't stutter. Surrender or die!", null, new ConversationSentence.OnConsequenceDelegate(this.convo_neutral_war_on_consequence), 100, null, null);
            obj.AddPlayerLine("mod_buypatrols_neutral_aggro_oops", "mod_buypatrols_neutral_aggresive_player_response", "close_window", "{=BUYPATROLS012}Just joking, goodbye!", null, new ConversationSentence.OnConsequenceDelegate(this.conversation_patrol_leave_on_consequence), 100, null, null);

            #endregion

            #region Enemy Patrols

            obj.AddDialogLine("mod_buypatrols_talk_enemy_start", "start", "mod_buypatrols_enemy_talk", "{=BUYPATROLS013}Stop right there! You're an enemy of {BASILPATROLS_LIEGE} and we shall capture you.", new ConversationSentence.OnConditionDelegate(this.patrol_talk_enemy_start_conditional), null, 100, null);
            obj.AddPlayerLine("mod_buypatrols_enemy_leave", "mod_buypatrols_enemy_talk", "close_window", "{=BUYPATROLS014}We can do this the easy way or the hard way.", null, new ConversationSentence.OnConsequenceDelegate(this.conversation_patrol_leave_on_consequence), 100, null, null);

            #endregion
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
            try
            {
                if (PlayerEncounter.Current != null && Campaign.Current.CurrentConversationContext == ConversationContext.PartyEncounter && 
                    encounteredParty.IsMobile && encounteredParty.Name.ToString().EndsWith(patrolWord.ToString()) && encounteredParty.IsActive && encounteredParty.MobileParty.HomeSettlement.OwnerClan == Clan.PlayerClan)
                {
                    return true;
                }
                return false;
            }
            catch(Exception e)
            {
                MessageBox.Show(e.ToString());
                return false;
            }
            
        }

        private bool patrol_talk_enemy_start_conditional()
        {
            PartyBase encounteredParty = PlayerEncounter.EncounteredParty;
            if (PlayerEncounter.Current != null && Campaign.Current.CurrentConversationContext == ConversationContext.PartyEncounter &&
                    encounteredParty.IsMobile && encounteredParty.Name.ToString().EndsWith(patrolWord.ToString()) && encounteredParty.IsActive && encounteredParty.MobileParty.HomeSettlement.OwnerClan.MapFaction.IsAtWarWith(Clan.PlayerClan.MapFaction))
            {
                MBTextManager.SetTextVariable("BASILPATROLS_LIEGE", encounteredParty.MobileParty.HomeSettlement.OwnerClan.Leader);
                return true;
            }
            return false;
        }

        private bool patrol_talk_neutral_start_conditional()
        {
            PartyBase encounteredParty = PlayerEncounter.EncounteredParty;
            if (PlayerEncounter.Current != null && Campaign.Current.CurrentConversationContext == ConversationContext.PartyEncounter &&
                    encounteredParty.IsMobile && encounteredParty.Name.ToString().EndsWith(patrolWord.ToString()) && encounteredParty.IsActive && !encounteredParty.MobileParty.HomeSettlement.OwnerClan.MapFaction.IsAtWarWith(Clan.PlayerClan.MapFaction)
                    && encounteredParty.MobileParty.HomeSettlement.OwnerClan != Clan.PlayerClan)
            {
                MBTextManager.SetTextVariable("BASILPATROL_AREA", encounteredParty.MobileParty.HomeSettlement);
                MBTextManager.SetTextVariable("BASILPATROLS_LIEGE_0", encounteredParty.MobileParty.HomeSettlement.OwnerClan.Leader);
                return true;
            }
            return false;
        }

        #endregion

        #region Consequences

        private void game_menu_switch_to_main_menu(MenuCallbackArgs args)
        {
            if(Settlement.CurrentSettlement.IsVillage) 
            {
                GameMenu.SwitchToMenu("village");
            }
            else if (Settlement.CurrentSettlement.IsTown)
            {
                GameMenu.SwitchToMenu("town_keep");
            }
            else
            {
                GameMenu.SwitchToMenu("castle");
            }
        }

        private void conversation_patrol_leave_on_consequence()
        {
            PlayerEncounter.LeaveEncounter = true;
        }

        private void conversation_patrol_disband_on_consequence()
        {
            PartyBase encounteredParty = PlayerEncounter.EncounteredParty;
            PatrolProperties patrolProperties;
            settlementPatrolProperties.TryGetValue(encounteredParty.MobileParty.HomeSettlement.StringId, out patrolProperties);
            patrolProperties.patrols.Remove(encounteredParty.MobileParty);
            settlementPatrolProperties[encounteredParty.MobileParty.HomeSettlement.StringId] = patrolProperties;
            encounteredParty.MobileParty.RemoveParty();
            
            PlayerEncounter.LeaveEncounter = true;
        }

        private void conversation_patrol_donate_troops_on_consequence()
        {
            PartyBase encounteredParty = PlayerEncounter.EncounteredParty;
            PartyScreenManager.OpenScreenAsDonateTroops(encounteredParty.MobileParty);
        }

        private void conversation_patrol_disband_now_on_consequence()
        {
            PartyBase encounteredParty = PlayerEncounter.EncounteredParty;
            encounteredParty.MobileParty.RemoveParty();
            PlayerEncounter.LeaveEncounter = true;
        }

        private void convo_neutral_war_on_consequence()
        {
            PartyBase encounteredParty = PlayerEncounter.EncounteredParty;
            if (encounteredParty.Name.ToString().EndsWith(patrolWord.ToString()) && encounteredParty.IsActive && !encounteredParty.MobileParty.HomeSettlement.OwnerClan.IsAtWarWith(Clan.PlayerClan)
                    && encounteredParty.MobileParty.HomeSettlement.OwnerClan != Clan.PlayerClan)
            {
                DeclareWarAction.Apply(Clan.PlayerClan.MapFaction, encounteredParty.MobileParty.HomeSettlement.OwnerClan.MapFaction);
                ChangeRelationAction.ApplyPlayerRelation(encounteredParty.MobileParty.HomeSettlement.OwnerClan.Leader, -5, true, true);
            }
            PlayerEncounter.LeaveEncounter = true;
        }

        #endregion

        #endregion  

        #region Spawn Patrol Stuff

        public MobileParty spawnPatrol(Settlement settlement, int amount)
        {
            PartyTemplateObject partyTemplate = settlement.Culture.DefaultPartyTemplate;
            int numberOfCreated = partyTemplate.NumberOfCreated;
            partyTemplate.IncrementNumberOfCreated();
            MobileParty mobileParty = MBObjectManager.Instance.CreateObject<MobileParty>(settlement.OwnerClan.StringId + "_" + numberOfCreated);

            TextObject textObject;
            textObject = new TextObject("{BASILPATROL_SETTLEMENT_NAME} Patrol", null);
            textObject.SetTextVariable("BASILPATROL_SETTLEMENT_NAME", settlement.Name);
            mobileParty.InitializeMobileParty(textObject, partyTemplate, settlement.GatePosition, 0, 0, 0, rand.Next((int)(amount*0.9), (int)Math.Ceiling((amount + 1)*1.1)));
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
            mobileParty.InitializeMobileParty(textObject, partyTemplate, settlement.GatePosition, 0, 0, 0, rand.Next((int)(amount * 0.9), (int)Math.Ceiling((amount + 1) * 1.1)));
            InitPatrolParty(mobileParty, textObject, clan, settlement);
            mobileParty.Aggressiveness = 1f;
            mobileParty.SetMovePatrolAroundSettlement(settlement);
            return mobileParty;
        }

        public void InitPatrolParty(MobileParty patrolParty, TextObject name, Clan faction, Settlement homeSettlement)
        {
            patrolParty.Name = name;
            patrolParty.IsMilitia = true;
            patrolParty.HomeSettlement = homeSettlement;
            patrolParty.Party.Owner = faction.Leader;
            patrolParty.SetInititave(1f, 0f, 100000000f);
            patrolParty.Party.Visuals.SetMapIconAsDirty();
            GenerateFood(patrolParty);
        }

        private bool AttemptAddPatrolToDictionary(Settlement currentSettlement, PatrolProperties properties, int cost, int multiplier = 1)
        {
            if(cost <= Hero.MainHero.Gold)
            {
                GiveGoldAction.ApplyForCharacterToSettlement(Hero.MainHero, Settlement.CurrentSettlement, cost);
                MobileParty party = spawnPatrol(Settlement.CurrentSettlement, TroopsPerPatrol * multiplier);
                properties.patrols.Add(party);
                settlementPatrolProperties[currentSettlement.StringId] = properties;
                if (currentSettlement.OwnerClan == Clan.PlayerClan)
                {
                    playerPatrols.Add(party);
                }
                allPatrols.Add(party);
                return true;
            }
            return false;
        }

        #endregion

        #region Patrol Behavior

        public void PatrolHourlyAi()
        {
            
            PatrolProperties patrolProperties;
            foreach(Settlement settlement in Settlement.All)
            {
                if (settlement.IsVillage || settlement.IsCastle)
                {
                    settlementPatrolProperties.TryGetValue(settlement.StringId, out patrolProperties);
                    patrolProperties.patrols.RemoveAll(x => x.MemberRoster.IsEmpty());
                    allPatrols.RemoveAll(x => x.MemberRoster.IsEmpty());
                    bool flag = true;
                    foreach (MobileParty patrol in patrolProperties.patrols.ToList())
                    {
                        flag = true;
                        // Unknown Behavior Potential Fix
                        if(CheckUnknownBehavior(patrol))
                        {
                            patrol.ResetAiBehaviorObject();
                            patrol.SetMoveGoToPoint(patrol.FindReachablePointAroundPosition(patrol.HomeSettlement.GatePosition, 5));
                            continue;
                        }
                        // Prisoner Section
                        Settlement closestTown = FindClosestTown(patrol);
                        if (closestTown != null)
                        {
                            patrol.SetMoveGoToSettlement(closestTown);
                            continue;
                        }
                        // Engage/Disband section
                        if (patrol.DefaultBehavior == AiBehavior.EngageParty || patrol.DefaultBehavior == AiBehavior.FleeToPoint || patrol.IsGoingToSettlement)
                        {
                            if (patrol.DefaultBehavior == AiBehavior.EngageParty)
                            {
                                if (patrol.IsGoingToSettlement && patrol.IsDisbanding && closestTown == null)
                                {
                                    patrol.SetMoveGoToSettlement(closestTown);
                                }
                                else if (d.GetDistance(patrol, patrol.HomeSettlement) > PatrolTetherRange)
                                {
                                    patrol.SetMoveGoToPoint(patrol.HomeSettlement.GatePosition);
                                }
                                else if (patrol.TargetParty == MobileParty.MainParty && patrol.HomeSettlement.OwnerClan == Clan.PlayerClan)
                                {
                                    patrol.Party.Owner = Hero.MainHero;
                                    patrol.SetMovePatrolAroundSettlement(patrol.HomeSettlement);
                                }
                                else if (patrol.TargetParty != null)
                                {
                                    if (!patrol.HomeSettlement.OwnerClan.IsAtWarWith(patrol.TargetParty.HomeSettlement.OwnerClan))
                                    {
                                        patrol.SetMovePatrolAroundSettlement(patrol.HomeSettlement);
                                    }
                                } 

                            }
                            if(patrol.IsGoingToSettlement && closestTown == null)
                            {
                                if (!patrol.IsDisbanding && d.GetDistance(patrol, patrol.HomeSettlement) <= 5)
                                {
                                    patrol.SetMovePatrolAroundSettlement(patrol.HomeSettlement);
                                }
                            }
                            continue;
                        }
                        if (!patrol.IsMoving)
                        {
                            patrol.SetMovePatrolAroundSettlement(patrol.HomeSettlement);
                            continue;
                        }

                        // Target AI
                        List<MobileParty> parties = partiesAroundPosition.GetPartiesAroundPosition(patrol.Position2D, 10f);
                        foreach(MobileParty possibleEnemy in parties.ToList())
                        {
                            if(patrol.IsActive && possibleEnemy.MapFaction.IsAtWarWith(patrol.MapFaction) && possibleEnemy.IsActive && 
                                !possibleEnemy.IsGarrison && (possibleEnemy.IsMoving || possibleEnemy.IsEngaging  || possibleEnemy.IsRaiding))

                            {
                                if (!TargetVillagers && possibleEnemy.IsVillager)
                                {
                                    continue;
                                }
                                if(!TargetCaravans && possibleEnemy.IsCaravan)
                                {
                                    continue;
                                }
                                if(patrol.Party.TotalStrength > possibleEnemy.Party.TotalStrength && possibleEnemy.Party.NumberOfAllMembers != 0)
                                {
                                    patrol.SetMoveEngageParty(possibleEnemy);
                                    flag = false;
                                    break;
                                }
                            }
                        }
                        if (flag)
                        {
                            if (d.GetDistance(patrol, patrol.HomeSettlement) > PatrolTetherRange)
                            {
                                patrol.SetMoveGoToPoint(patrol.HomeSettlement.GatePosition);
                            } else
                            {
                                patrol.SetMovePatrolAroundSettlement(patrol.HomeSettlement);
                            }
                        }
                    }
                    //allPatrols.AddRange(patrolProperties.patrols);
                    settlementPatrolProperties[settlement.StringId] = patrolProperties;
                }
            }
            
        }

        public void PatrolDailyAi()
        {
            //KillUnknownBehaviorParties();
            PatrolProperties patrolProperties;
            foreach (Settlement settlement in Settlement.All)
            {
                if(settlement.IsVillage || settlement.IsCastle)
                {
                    settlementPatrolProperties.TryGetValue(settlement.StringId, out patrolProperties);
                    patrolProperties.patrols.RemoveAll(x => x.MemberRoster.IsEmpty());
                    allPatrols.RemoveAll(x => x.MemberRoster.IsEmpty());

                    foreach (MobileParty patrol in patrolProperties.patrols.ToList())
                    {
                        if (patrol.Food <= 3)
                        {
                            GenerateFood(patrol);
                        }
                        if (patrol.Party.NumberOfAllMembers > TroopsPerPatrol * 3 * 1.1 && ForceTroopCapEnabled)
                        {
                            patrol.MemberRoster.KillNumberOfMenRandomly((int)Math.Floor(patrol.MemberRoster.Count - TroopsPerPatrol * 3 * 1.1), false);
                        }
                        if (patrol.Party.NumberOfAllMembers < TroopsPerPatrol * 3 * 1.1 && ForceRegenPatrol)
                        {
                            patrol.AddElementToMemberRoster(patrol.Party.Culture.BasicTroop, 1, false);
                        }
                    }
                    //allPatrols.AddRange(patrolProperties.patrols);
                    settlementPatrolProperties[settlement.StringId] = patrolProperties;
                }
            }
        }

        public void PayPatrols()
        {
            int totalWages = 0;
            PatrolProperties patrolProperties;
            foreach (Settlement settlement in Settlement.All)
            {
                if ((settlement.IsVillage || settlement.IsCastle) && settlement.OwnerClan == Clan.PlayerClan)
                {
                    settlementPatrolProperties.TryGetValue(settlement.StringId, out patrolProperties);
                    foreach (MobileParty patrol in patrolProperties.patrols.ToList())
                    {
                        GiveGoldAction.ApplyForCharacterToParty(Hero.MainHero, patrol.Party, (int)(patrol.GetTotalWage() * DailyPatrolWageModifier), true);
                        totalWages += (int)(patrol.GetTotalWage() * DailyPatrolWageModifier);
                    }
                    settlementPatrolProperties[settlement.StringId] = patrolProperties;
                }
            }
            if (totalWages > 0)
            {
                TextObject text = new TextObject("Daily Patrol Wages: -{BASILPATROL_DAILY_WAGES}{GOLD_ICON}", null);
                text.SetTextVariable("GOLD_ICON", "<img src=\"Icons\\Coin@2x\">");
                text.SetTextVariable("BASILPATROL_DAILY_WAGES", totalWages);
                InformationManager.DisplayMessage(new InformationMessage(text.ToString()));
            }
        }

        public void GenerateFood(MobileParty patrolParty)
        {
            foreach (ItemObject itemObject in ItemObject.All)
            {
                if (itemObject.IsFood)
                {
                    int foodAmount = MBRandom.RoundRandomized((float)patrolParty.MemberRoster.TotalManCount * (1f / (float)itemObject.Value) * (float)1 * MBRandom.RandomFloat * MBRandom.RandomFloat * MBRandom.RandomFloat * MBRandom.RandomFloat);
                    if (foodAmount > 0)
                    {
                        patrolParty.ItemRoster.AddToCounts(itemObject, foodAmount, true);
                    }
                }
            }
        }

        public void IncreaseRelations()
        {
            PatrolProperties patrolProperties;
            bool flag = false;
            foreach (Settlement settlement in Settlement.All)
            {
                if (settlement.IsVillage)
                {
                    settlementPatrolProperties.TryGetValue(settlement.StringId, out patrolProperties);
                    if (patrolProperties.patrols.Count > 0 && settlement.OwnerClan == Clan.PlayerClan)
                    {
                        foreach (Hero notable in settlement.Notables)
                        {
                            if(notable.GetRelationWithPlayer() < RelationCap)
                            {
                                // 10% chance of apply increase in relations
                                if (rand.Next(0, 100) < 10)
                                {
                                    ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, notable, rand.Next(1, patrolProperties.patrols.Count + 1), NotifyNotableRelations);
                                    flag = true;
                                }
                            }
                        }

                    }
                }
            }
            if (flag)
            {
                InformationManager.DisplayMessage(new InformationMessage(new TextObject("Your relation with notables in some of your settlements increased due to having patrols", null).ToString()));
            }
        }

        public void OnSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero hero) 
        {
            if(mobileParty != null && mobileParty.IsActive && mobileParty.Name.ToString().EndsWith(patrolWord.ToString()))
            {
                //InformationManager.DisplayMessage(new InformationMessage(new TextObject("Patrol entered " + settlement.Name, null).ToString()));
                if (settlement.IsTown || settlement.IsCastle)
                {
                    for (int i = 0; i < mobileParty.PrisonRoster.Count; i++)
                    {
                        TroopRosterElement prisoner = mobileParty.PrisonRoster.GetElementCopyAtIndex(i);
                        int woundedNumber = prisoner.WoundedNumber;
                        if (prisoner.Character.IsHero)
                        {
                            EnterSettlementAction.ApplyForPrisoner(prisoner.Character.HeroObject, settlement);
                        }
                        settlement.Party.PrisonRoster.AddToCounts(prisoner.Character,  prisoner.Number, false, woundedNumber, 0, true, -1);
                        mobileParty.PrisonRoster.RemoveTroop(prisoner.Character, prisoner.Number, default(UniqueTroopDescriptor), 0);
                    }
                    mobileParty.SetMoveGoToPoint(mobileParty.HomeSettlement.GatePosition);
                }
            }
        }

        public bool CheckUnknownBehavior(MobileParty patrol)
        {
            TextObject textObject = new TextObject("{=QXBf26Rv}Unknown Behavior", null);
            if (CampaignUIHelper.GetMobilePartyBehaviorText(patrol) == textObject.ToString())
            {
                return true;
            }
            return false;
        }

        public Settlement FindClosestTown(MobileParty patrol)
        {
            Settlement settlement = null;
            float shortestDistance = float.MaxValue;
            if (patrol.Party.NumberOfPrisoners > 10)
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

        public void KillUnknownBehaviorParties()
        {
            foreach(MobileParty party in MobileParty.All.ToList())
            {
                if (party.Name.ToString().EndsWith(patrolWord.ToString()) && CheckUnknownBehavior(party))
                {
                    party.RemoveParty();
                }
            }
        }

        public void AttemptToDestroyAllPatrols()
        {
            PatrolProperties properties;
            int remaining = 0;
            int destroyed = 0;
            foreach(string id in settlementPatrolProperties.Keys.ToList())
            {
                settlementPatrolProperties.TryGetValue(id, out properties);
                if(properties != null)
                {
                    if(properties.patrols.Count != 0)
                    {
                        foreach(MobileParty patrol in properties.patrols.ToList())
                        {
                            if (patrol.IsEngaging)
                            {
                                remaining++;
                            }
                            else
                            {
                                properties.patrols.Remove(patrol);
                                allPatrols.Remove(patrol);
                                patrol.RemoveParty();
                                destroyed++;
                            }
                        }
                    }
                }
            }
            TextObject removed = new TextObject("{=BUYPATROLSREMOVEDNOTIFY}{PATROLSREMOVED} patrols have been removed today.");
            removed.SetTextVariable("PATROLSREMOVED", destroyed);
            TextObject remainingText = new TextObject("{=BUYPATROLSREMAININGNOTIFY}There are {PATROLSREMAINING} patrols remaining and are currently in engagement.");
            remainingText.SetTextVariable("PATROLSREMAINING", remaining);
            InformationManager.DisplayMessage(new InformationMessage(removed.ToString(), Colors.Red));
            InformationManager.DisplayMessage(new InformationMessage(remainingText.ToString(), Colors.Cyan));
        }

        public bool ConditionalCheckOnSettlementMenuOption(Settlement settlement, int cost, PatrolProperties patrolProperties)
        {
            if (Settlement.CurrentSettlement.IsVillage)
            {
                if (cost > Hero.MainHero.Gold || patrolProperties.getPatrolCount() >= MaxPatrolCountPerVillage || Settlement.CurrentSettlement.OwnerClan != Clan.PlayerClan || playerPatrols.Count >= MaxTotalPatrols)
                {
                    return false;
                }
                return true;
            }
            else if (Settlement.CurrentSettlement.IsTown)
            {
                if (cost > Hero.MainHero.Gold || patrolProperties.getPatrolCount() >= MaxPatrolCountPerTown || Settlement.CurrentSettlement.OwnerClan != Clan.PlayerClan || playerPatrols.Count >= MaxTotalPatrols)
                {
                    return false;
                }
                return true;
            }
            else
            {
                if (cost > Hero.MainHero.Gold || patrolProperties.getPatrolCount() >= MaxPatrolCountPerCastle || Settlement.CurrentSettlement.OwnerClan != Clan.PlayerClan || playerPatrols.Count >= MaxTotalPatrols)
                {
                    return false;
                }
                return true;
            }
        }
        #endregion

        private void NotifyDestroyedPatrol(MobileParty destroyedParty, PartyBase destroyerParty)
        {
            try
            {
                if (destroyedParty != null && destroyedParty.Name.ToString().EndsWith(patrolWord.ToString()))
                {
                    MobileParty patrol = playerPatrols.FirstOrDefault(x => x == destroyedParty);
                    if (patrol != null)
                    {
                        if (Settings.Instance.NotifyDestroyedPatrol)
                        {
                            TextObject text = new TextObject("{=BUYPATROLSNOTIFY}{PATROLNAME} has been wiped out by {DESTROYERNAME}.");
                            text.SetTextVariable("PATROLNAME", patrol.Name);
                            text.SetTextVariable("DESTROYERNAME", destroyedParty.Name);
                            InformationManager.DisplayMessage(new InformationMessage(text.ToString(), new Color(255 / 255, 128 / 255, 0)));
                        }
                        playerPatrols.Remove(patrol);
                    }
                    allPatrols.Remove(destroyedParty);
                    destroyedParty.RemoveParty();
                }
            }
            catch(Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }

        public void AiGeneratePatrols()
        {
            PatrolProperties properties;
            foreach (Settlement settlement in Settlement.All)
            {
                if (settlement.IsVillage && settlement.OwnerClan != Clan.PlayerClan && settlement.Village.Hearth <= Settings.Instance.HearthMaximum)
                {
                    if (settlementPatrolProperties.ContainsKey(settlement.StringId))
                    {
                        settlementPatrolProperties.TryGetValue(settlement.StringId, out properties);
                        if(properties != null && properties.patrols.Count < Settings.Instance.AiMaxPatrolPerSettlement && settlement.OwnerClan.Gold > (BaseCost + properties.getPatrolCost()) * 3)
                        {
                            if(rand.Next(0, 100) <= Settings.Instance.AiGenerationChance)
                            {
                                MobileParty party;
                                if (rand.Next(0, 100) < 60)
                                {
                                    GiveGoldAction.ApplyForCharacterToSettlement(settlement.OwnerClan.Leader, settlement, (BaseCost + properties.getPatrolCost()) * 3, true);
                                    party = spawnPatrol(settlement, TroopsPerPatrol * 3);
                                } else if(rand.Next(0, 100) < 85)
                                {
                                    GiveGoldAction.ApplyForCharacterToSettlement(settlement.OwnerClan.Leader, settlement, (BaseCost + properties.getPatrolCost()) * 2, true);
                                    party = spawnPatrol(settlement, TroopsPerPatrol * 2);
                                } else
                                {
                                    GiveGoldAction.ApplyForCharacterToSettlement(settlement.OwnerClan.Leader, settlement, BaseCost + properties.getPatrolCost(), true);
                                    party = spawnPatrol(settlement, TroopsPerPatrol);
                                }
                                properties.patrols.Add(party);
                                settlementPatrolProperties[settlement.StringId] = properties;
                                allPatrols.Add(party);
                                //InformationManager.DisplayMessage(new InformationMessage(new TextObject(settlement.OwnerClan.Leader.ToString() + " has hired a patrol at " + settlement.ToString()).ToString()));
                            }
                        }
                    }
                }
            }
        }

        private void TrackPatrols()
        {
            foreach (Settlement settlement in Settlement.All)
            {
                if (settlement.IsVillage || settlement.IsCastle || settlement.IsTown)
                {
                    if (!settlementPatrolProperties.ContainsKey(settlement.StringId))
                    {
                        settlementPatrolProperties.Add(settlement.StringId, new PatrolProperties(settlement.StringId, new List<MobileParty>()));
                    }
                }
            }

        }
        
        private void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
        {
            if(settlementPatrolProperties.ContainsKey(settlement.StringId))
            {
                PatrolProperties props;
                settlementPatrolProperties.TryGetValue(settlement.StringId, out props);
                foreach(MobileParty patrol in props.patrols)
                {
                    patrol.Party.Owner = newOwner;
                }
                settlementPatrolProperties[settlement.StringId] = props;
            }
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("settlementPatrolProperties", ref settlementPatrolProperties);
            dataStore.SyncData("allPatrols", ref allPatrols);
            dataStore.SyncData("playerPatrols", ref playerPatrols);
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
