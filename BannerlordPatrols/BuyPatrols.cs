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
using TaleWorlds.CampaignSystem.SandBox.Conversations;
using TaleWorlds.ObjectSystem;

namespace BuyPatrols
{
    public class BuyPatrols : CampaignBehaviorBase
    {
        Dictionary<string, PatrolProperties> settlementPatrolProperties = new Dictionary<string, PatrolProperties>();
        List<DelayedProperties> delayedPatrols = new List<DelayedProperties>(); // delayed spawn patrols
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
        public TextObject patrolWord = new TextObject("{=modbp015}Patrol");
        #endregion

        private void OnSessionLaunched(CampaignGameStarter obj)
        {
            if (Settings.Instance.NukeAllPatrols)
            {
                TextObject warning = new TextObject("{=modbp001}BuyPatrols WARNING: DESTROYING ALL PATROLS DAILY OPTION ENABLED.");
                InformationManager.DisplayMessage(new InformationMessage(warning.ToString(), Colors.Red));
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
            try
            {
                if (Settings.Instance.RemoveDuplicateLords)
                    RemoveDuplicates();
            } catch(Exception e)
            {
                MessageBox.Show("Error in deleting duped lords. " + e.ToString());
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
            //UnknownBehaviorChecker();
            if (Settings.Instance.RemoveDuplicateLords)
                RemoveDuplicates();
            //if (delayedPatrols.Count != 0)
            //{
            //    CheckDelayed();
            //}
        }

        private void OnHourlyTick()
        {
            PatrolHourlyAi();
            UnknownBehaviorChecker();
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionLaunched));
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, new Action(this.OnHourlyTick));
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
                obj.AddGameMenuOption("village", "basilpatrol_manage_patrol", "{=modbp004}Manage patrols",
                    (MenuCallbackArgs args) =>
                    {
                        args.optionLeaveType = GameMenuOption.LeaveType.Manage;
                        //if (Hero.MainHero.IsFactionLeader && Settlement.CurrentSettlement.MapFaction == Hero.MainHero.MapFaction && MaxPatrolCountPerVillage != 0)
                        //{
                        //    return true;
                        //}
                        if (Settlement.CurrentSettlement.OwnerClan != Clan.PlayerClan && MaxPatrolCountPerVillage != 0)
                        {
                            return false;
                        }
                        
                        return true;
                    },
                    (MenuCallbackArgs args) => { GameMenu.SwitchToMenu("basilpatrol_pay_menu"); }, false, 4);

                obj.AddGameMenu("basilpatrol_pay_menu", "{=modbp006}The {BASILPATROL_SETTLEMENT_TYPE} says they have the manpower but not the gear to " +
                    "send men on patrols. He tells he can send up to {BASILPATROL_MAX_PATROL_AMOUNT} patrols if you are willing to pay their " +
                    "gear. Your clan must own the village to hire patrols. Patrols will switch allegiance to the new clan owner if ownership changes. " +
                    "Disbanding patrols will only affect patrols that are not engaging an enemy. You can have a total of {BASILPATROLS_CAP_PATROLS} patrols in your clan. " +
                    "You have {BASILPATROLS_CAP_LEFT} patrols you can still hire for your clan. {BASILPATROLS_STATUS}",
                    (MenuCallbackArgs args) =>
                    {
                        if (Settlement.CurrentSettlement.IsVillage)
                        {
                            MBTextManager.SetTextVariable("BASILPATROL_SETTLEMENT_TYPE", "{=modbp007}village spokesman", false);
                            MBTextManager.SetTextVariable("BASILPATROL_MAX_PATROL_AMOUNT", MaxPatrolCountPerVillage, false);
                        } else if(Settlement.CurrentSettlement.IsTown)
                        {
                            MBTextManager.SetTextVariable("BASILPATROL_SETTLEMENT_TYPE", "{=modbp008}castle sergeant", false);
                            MBTextManager.SetTextVariable("BASILPATROL_MAX_PATROL_AMOUNT", MaxPatrolCountPerTown, false);
                        }
                        else
                        {
                            MBTextManager.SetTextVariable("BASILPATROL_SETTLEMENT_TYPE", "{=modbp008}castle sergeant", false);
                            MBTextManager.SetTextVariable("BASILPATROL_MAX_PATROL_AMOUNT", MaxPatrolCountPerCastle, false);
                        }
                        PatrolProperties patrolProperties;
                        string settlementID = Settlement.CurrentSettlement.StringId;
                        settlementPatrolProperties.TryGetValue(settlementID, out patrolProperties);
                        int cost = BaseCost + patrolProperties.getPatrolCost();
                        if (Hero.MainHero.Gold < cost)
                        {
                            MBTextManager.SetTextVariable("BASILPATROLS_STATUS", "{=modNoMoney}You have no money for any patrols.", false);
                        } else
                        {
                            MBTextManager.SetTextVariable("BASILPATROLS_STATUS", "", false);
                        }
                        MBTextManager.SetTextVariable("BASILPATROLS_CAP_PATROLS", MaxTotalPatrols, false);
                        MBTextManager.SetTextVariable("BASILPATROLS_CAP_LEFT", (MaxTotalPatrols - playerPatrols.Count - delayedPatrols.Count).ToString(), false);
                    });

                #region Hiring

                obj.AddGameMenuOption("basilpatrol_pay_menu", "basilpatrol_pay_small", "{=modbp010}Pay for a small patrol ({BASILPATROL_SMALL_COST}{GOLD_ICON})",
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
                            if (AttemptAddPatrol(Settlement.CurrentSettlement, patrolProperties, cost))
                            {
                                TextObject text = new TextObject("{=modbp009}You have hired a patrol at {BUYPATROLSCURRENTSETTLEMENT}.");
                                text.SetTextVariable("BUYPATROLSCURRENTSETTLEMENT", Settlement.CurrentSettlement.ToString());
                                InformationManager.DisplayMessage(new InformationMessage(text.ToString()));
                            }
                            GameMenu.SwitchToMenu("basilpatrol_pay_menu");
                            
                        } catch(Exception e)
                        {
                            MessageBox.Show("Error in small purchase..." + e.ToString());
                        }
                        
                    });

                obj.AddGameMenuOption("basilpatrol_pay_menu", "basilpatrol_pay_medium", "{=modbp011}Pay for a medium patrol ({BASILPATROL_MEDIUM_COST}{GOLD_ICON})",
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
                            if (AttemptAddPatrol(Settlement.CurrentSettlement, patrolProperties, cost, 2))
                            {
                                TextObject text = new TextObject("{=modbp009}You have hired a patrol at {BUYPATROLSCURRENTSETTLEMENT}.");
                                text.SetTextVariable("BUYPATROLSCURRENTSETTLEMENT", Settlement.CurrentSettlement.ToString());
                                InformationManager.DisplayMessage(new InformationMessage(text.ToString()));
                            }
                            GameMenu.SwitchToMenu("basilpatrol_pay_menu");
                            
                        } catch (Exception e)
                        {
                            MessageBox.Show("Error in medium purchase... " + e.ToString());
                        }
                        
                    });

                obj.AddGameMenuOption("basilpatrol_pay_menu", "basilpatrol_pay_large", "{=modbp012}Pay for a large patrol ({BASILPATROL_LARGE_COST}{GOLD_ICON})",
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
                            if (AttemptAddPatrol(Settlement.CurrentSettlement, patrolProperties, cost, 3))
                            {
                                TextObject text = new TextObject("{=modbp009}You have hired a patrol at {BUYPATROLSCURRENTSETTLEMENT}.");
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

                obj.AddGameMenuOption("basilpatrol_pay_menu", "basilpatrol_disband_all", "{=modbp013}Disband all patrols",
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
                                        if(patrol.MapEvent != null)
                                        {
                                            patrol.MapEvent.FinishBattle();
                                        }
                                        if (patrol.HomeSettlement.OwnerClan == Clan.PlayerClan)
                                        {
                                            playerPatrols.Remove(patrol);
                                        }
                                        //DisbandPartyAction.ApplyDisband(patrol);
                                        allPatrols.Remove(patrol);
                                        patrolProperties.patrols.Remove(patrol);
                                        patrol.RemoveParty();
                                        
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

                obj.AddGameMenuOption("basilpatrol_pay_menu", "basilpatrol_leave", "{=modbp014}Leave", game_menu_just_add_leave_conditional, game_menu_switch_to_main_menu);
            } catch(Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }

        public void AddWalledSettlementDialog(CampaignGameStarter obj)
        {
            obj.AddGameMenuOption("castle", "basilpatrol_castle_patrol", "{=modbp004}Manage patrols", (MenuCallbackArgs args) =>
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Manage;
                //if(Hero.MainHero.IsFactionLeader && Settlement.CurrentSettlement.MapFaction == Hero.MainHero.MapFaction && MaxPatrolCountPerCastle != 0)
                //{
                //    return true;
                //}
                if (Settlement.CurrentSettlement.OwnerClan != Clan.PlayerClan && MaxPatrolCountPerCastle != 0)
                {
                    return false;
                }
                return true;
            },
            (MenuCallbackArgs args) => { GameMenu.SwitchToMenu("basilpatrol_pay_menu"); }, false, 4);

            obj.AddGameMenuOption("town_keep", "basilpatrol_town_patrol", "{=modbp004}Manage patrols", (MenuCallbackArgs args) =>
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Manage;
                //if (Hero.MainHero.IsFactionLeader && Settlement.CurrentSettlement.MapFaction == Hero.MainHero.MapFaction && MaxPatrolCountPerTown != 0)
                //{
                //    return true;
                //}
                if (Settlement.CurrentSettlement.OwnerClan != Clan.PlayerClan && MaxPatrolCountPerTown != 0)
                {
                    return false;
                }
                return true;
            },
            (MenuCallbackArgs args) => { GameMenu.SwitchToMenu("basilpatrol_pay_menu"); }, false, 5);
        }

        public void AddPatrolDialog(CampaignGameStarter obj)
        {
            #region Player Patrols

            obj.AddDialogLine("mod_buypatrols_talk_start", "start", "mod_buypatrols_talk", "{=modbp016}Hello my lord. What do you need us to do?", new ConversationSentence.OnConditionDelegate(this.patrol_talk_start_on_conditional), null, 100, null);
            obj.AddPlayerLine("mod_buypatrols_donate_troops", "mod_buypatrols_talk", "mod_buypatrols_after_donate", "{=modbp017}Donate Troops", null, new ConversationSentence.OnConsequenceDelegate(this.conversation_patrol_donate_troops_on_consequence), 100, null, null);
            obj.AddPlayerLine("mod_buypatrols_disband", "mod_buypatrols_talk", "close_window", "{=modbp018}Disband.", null, new ConversationSentence.OnConsequenceDelegate(this.conversation_patrol_disband_on_consequence), 100, null, null);
            obj.AddPlayerLine("mod_buypatrols_leave", "mod_buypatrols_talk", "close_window", "{=modbp019}Carry on. Farewell.", null, new ConversationSentence.OnConsequenceDelegate(this.conversation_patrol_leave_on_consequence), 100, null, null);
            obj.AddDialogLine("mod_buypatrols_after_donate", "mod_buypatrols_after_donate", "mod_buypatrols_talk", "{=modbp020}Anything else?", null, null, 100, null);
            obj.AddPlayerLine("mod_leaderless_party_answer", "disbanding_leaderless_party_start_response", "close_window", "{=modbp021}Disband now.", null, new ConversationSentence.OnConsequenceDelegate(this.conversation_patrol_disband_now_on_consequence), 100, null, null);

            #endregion

            #region Neutral Patrols

            obj.AddDialogLine("mod_buypatrols_talk_neutral_start", "start", "mod_buypatrols_neutral_talk", "{=modbp022}We are patrolling around {BASILPATROL_AREA} under commands of {BASILPATROLS_LIEGE_0}. What do you want?", new ConversationSentence.OnConditionDelegate(this.patrol_talk_neutral_start_conditional), null, 100, null);
            obj.AddPlayerLine("mod_buypatrols_neutral_attack", "mod_buypatrols_neutral_talk", "mod_buypatrols_neutral_aggresive", "{=modbp023}Surrender or die.", null, null, 100, null, null);
            obj.AddPlayerLine("mod_buypatrols_neutral_leave", "mod_buypatrols_neutral_talk", "close_window", "{=modbp024}Nothing just passing by.", null, new ConversationSentence.OnConsequenceDelegate(this.conversation_patrol_leave_on_consequence), 100, null, null);
            obj.AddDialogLine("mod_buypatrols_neutral_aggresive", "mod_buypatrols_neutral_aggresive", "mod_buypatrols_neutral_aggresive_player_response", "{=modbp025}What? You can't be serious?", null,null,100,null);
            obj.AddPlayerLine("mod_buypatrols_neutral_aggro_attack", "mod_buypatrols_neutral_aggresive_player_response", "close_window", "{=modbp026}I didn't stutter. Surrender or die!", null, new ConversationSentence.OnConsequenceDelegate(this.convo_neutral_war_on_consequence), 100, null, null);
            obj.AddPlayerLine("mod_buypatrols_neutral_aggro_oops", "mod_buypatrols_neutral_aggresive_player_response", "close_window", "{=modbp027}Just joking, goodbye!", null, new ConversationSentence.OnConsequenceDelegate(this.conversation_patrol_leave_on_consequence), 100, null, null);

            #endregion

            #region Enemy Patrols

            obj.AddDialogLine("mod_buypatrols_talk_enemy_start", "start", "mod_buypatrols_enemy_talk", "{=modbp028}Stop right there! You're an enemy of {BASILPATROLS_LIEGE} and we shall capture you.", new ConversationSentence.OnConditionDelegate(this.patrol_talk_enemy_start_conditional), null, 100, null);
            obj.AddPlayerLine("mod_buypatrols_enemy_leave", "mod_buypatrols_enemy_talk", "close_window", "{=modbp029}We can do this the easy way or the hard way.", null, new ConversationSentence.OnConsequenceDelegate(this.convo_enemy_talk_battle_consequence), 100, null, null);

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
            if(encounteredParty.Owner == Hero.MainHero)
            {
                playerPatrols.Remove(encounteredParty.MobileParty);
            }
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

        private void convo_enemy_talk_battle_consequence()
        {
            PlayerEncounter.Current.IsEnemy = true;
        }

        #endregion

        #endregion  

        #region Spawn Patrol Stuff

        public MobileParty spawnPatrol(Settlement settlement, int amount)
        {
            PartyTemplateObject partyTemplate;
            if (Settings.Instance.TroopType.SelectedValue == "Militia")
            {
                partyTemplate = settlement.Culture.MilitiaPartyTemplate;
            } else if (Settings.Instance.TroopType.SelectedValue == "Mercenary")
            {
                partyTemplate = MBObjectManager.Instance.GetObject<PartyTemplateObject>("buypatrols_mercs");
            }
            else
            {
                partyTemplate = settlement.Culture.DefaultPartyTemplate;
            }
            int numberOfCreated = partyTemplate.NumberOfCreated;
            partyTemplate.IncrementNumberOfCreated();
            MobileParty mobileParty = MBObjectManager.Instance.CreateObject<MobileParty>(settlement.OwnerClan.StringId + "_" + numberOfCreated);

            TextObject textObject;
            textObject = new TextObject("{BASILPATROL_SETTLEMENT_NAME} " + patrolWord, null);
            textObject.SetTextVariable("BASILPATROL_SETTLEMENT_NAME", settlement.Name);
            mobileParty.InitializeMobileParty(textObject, partyTemplate, settlement.GatePosition, 2, 0, 0, rand.Next((int)(amount * 0.9), (int)Math.Ceiling((amount + 1) * 1.1))); 
            InitPatrolParty(mobileParty, textObject, settlement.OwnerClan, settlement);

            mobileParty.SetMovePatrolAroundSettlement(settlement);
            return mobileParty;
        }

        public void InitPatrolParty(MobileParty patrolParty, TextObject name, Clan faction, Settlement homeSettlement)
        {
            patrolParty.Name = name;
            patrolParty.IsMilitia = true;
            patrolParty.HomeSettlement = homeSettlement;
            patrolParty.Party.Owner = faction.Leader;
            patrolParty.SetInititave(0f, 0.5f, float.MaxValue);
            patrolParty.Party.Visuals.SetMapIconAsDirty();
            GenerateFood(patrolParty);
        }

        private bool AttemptAddPatrol(Settlement currentSettlement, PatrolProperties properties, int cost, int multiplier = 1)
        {
            if(cost <= Hero.MainHero.Gold)
            {
                GiveGoldAction.ApplyForCharacterToSettlement(Hero.MainHero, Settlement.CurrentSettlement, cost);
                //if (Settings.Instance.ToggleDelaySpawn)
                //{
                //    delayedPatrols.Add(new DelayedProperties(currentSettlement, multiplier, 1));
                //}
                //else
                //{
                AddPatrolToSave(currentSettlement, multiplier, properties);
                //}
                return true;
            }
            return false;
        }

        public void CheckDelayed()
        {
            foreach(DelayedProperties props in delayedPatrols.ToList())
            {
                if(props.days <= 0)
                {
                    PatrolProperties patrolProps;
                    settlementPatrolProperties.TryGetValue(props.settlement.StringId, out patrolProps);
                    AddPatrolToSave(props.settlement, props.multiplier, patrolProps);
                    delayedPatrols.Remove(props);
                } else
                {
                    props.days -= 1;
                }
            }
        }

        public void AddPatrolToSave(Settlement settlement, int multiplier, PatrolProperties properties)
        {
            MobileParty party = spawnPatrol(Settlement.CurrentSettlement, TroopsPerPatrol * multiplier);
            properties.patrols.Add(party);
            settlementPatrolProperties[settlement.StringId] = properties;
            if (settlement.OwnerClan == Clan.PlayerClan)
            {
                playerPatrols.Add(party);
            }
            allPatrols.Add(party);
        }

        #endregion

        #region Patrol Behavior

        public void PatrolHourlyAi()
        {
            
            PatrolProperties patrolProperties;
            foreach(string settlementID in settlementPatrolProperties.Keys.ToList())
            {
                settlementPatrolProperties.TryGetValue(settlementID, out patrolProperties);
                patrolProperties.patrols.RemoveAll(x => x.MemberRoster.IsEmpty());
                allPatrols.RemoveAll(x => x.MemberRoster.IsEmpty());
                bool flag = true;
                foreach (MobileParty patrol in patrolProperties.patrols.ToList())
                {
                    if(patrol.MapEvent != null)
                    {
                        if (patrol.TargetParty != null)
                        {
                            if(patrol.TargetParty.Party.NumberOfHealthyMembers == 0)
                            {
                                patrol.MapEvent.FinishBattle();
                            }
                        }
                    }
                    patrol.Aggressiveness = 0;
                    flag = true;
                    // Prisoner Section
                    Settlement closestTown = Util.FindClosestTown(patrol);
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
                                if (!patrol.MapFaction.IsAtWarWith(patrol.TargetParty.MapFaction))
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
                            else if(patrol.Party.TotalStrength * 1.1 > possibleEnemy.Party.TotalStrength && possibleEnemy.Party.NumberOfHealthyMembers != 0)
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
                settlementPatrolProperties[settlementID] = patrolProperties;
                
            }
            
        }

        public void PatrolDailyAi()
        {
            //KillUnknownBehaviorParties();
            PatrolProperties patrolProperties;
            foreach (string settlementID in settlementPatrolProperties.Keys.ToList())
            {
                settlementPatrolProperties.TryGetValue(settlementID, out patrolProperties);
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
                settlementPatrolProperties[settlementID] = patrolProperties;
                
            }
        }

        public void PayPatrols()
        {
            int totalWages = 0;
            PatrolProperties patrolProperties;
            foreach (string settlementID in settlementPatrolProperties.Keys.ToList())
            {
                settlementPatrolProperties.TryGetValue(settlementID, out patrolProperties);
                if (patrolProperties.getSettlement().OwnerClan == Clan.PlayerClan)
                {
                    foreach (MobileParty patrol in patrolProperties.patrols.ToList())
                    {
                        GiveGoldAction.ApplyForCharacterToParty(Hero.MainHero, patrol.Party, (int)(patrol.GetTotalWage() * DailyPatrolWageModifier), true);
                        totalWages += (int)(patrol.GetTotalWage() * DailyPatrolWageModifier);
                    }
                    settlementPatrolProperties[settlementID] = patrolProperties;
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
            foreach (string settlementID in settlementPatrolProperties.Keys.ToList())
            {
                settlementPatrolProperties.TryGetValue(settlementID, out patrolProperties);
                if (patrolProperties.getSettlement().IsVillage || patrolProperties.getSettlement().IsTown)
                {
                    if (patrolProperties.patrols.Count > 0 && patrolProperties.getSettlement().OwnerClan == Clan.PlayerClan)
                    {
                        foreach (Hero notable in patrolProperties.getSettlement().Notables)
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
                    SellPrisonersAction.ApplyForAllPrisoners(mobileParty, mobileParty.PrisonRoster, settlement, false);
                    //settlement.Party.PrisonRoster.Add(mobileParty.PrisonRoster);
                    //mobileParty.Party.PrisonRoster.Clear();
                    mobileParty.SetMoveGoToPoint(mobileParty.FindReachablePointAroundPosition(mobileParty.HomeSettlement.GatePosition,10));
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
                            if (patrol.MapEvent != null)
                            {
                                patrol.MapEvent.FinishBattle();
                                //remaining++;
                                properties.patrols.Remove(patrol);
                                allPatrols.Remove(patrol);
                                if (patrol.HomeSettlement.OwnerClan == Clan.PlayerClan)
                                {
                                    playerPatrols.Remove(patrol);
                                }
                                patrol.RemoveParty();
                                destroyed++;
                            }
                            else
                            {
                                properties.patrols.Remove(patrol);
                                allPatrols.Remove(patrol);
                                if(patrol.HomeSettlement.OwnerClan == Clan.PlayerClan)
                                {
                                    playerPatrols.Remove(patrol);
                                }
                                patrol.RemoveParty();
                                destroyed++;
                            }
                        }
                    }
                }
            }
            TextObject removed = new TextObject("{=modbp003}{PATROLSREMOVED} patrols have been removed today.");
            removed.SetTextVariable("PATROLSREMOVED", destroyed);
            //TextObject remainingText = new TextObject("{=modbp005}There are {PATROLSREMAINING} patrols remaining and are currently in engagement.");
            //remainingText.SetTextVariable("PATROLSREMAINING", remaining);
            InformationManager.DisplayMessage(new InformationMessage(removed.ToString(), Colors.Red));
            //InformationManager.DisplayMessage(new InformationMessage(remainingText.ToString(), Colors.Cyan));
        }

        public bool ConditionalCheckOnSettlementMenuOption(Settlement settlement, int cost, PatrolProperties patrolProperties)
        {
            if (settlement.IsVillage)
            {
                //if (Hero.MainHero.IsFactionLeader && settlement.MapFaction == Hero.MainHero.MapFaction && cost <= Hero.MainHero.Gold && patrolProperties.getPatrolCount() < MaxPatrolCountPerCastle && playerPatrols.Count < MaxTotalPatrols)
                //{
                //    return true;
                //}
                if (cost > Hero.MainHero.Gold || patrolProperties.getPatrolCount() + Util.GetDelayedPatrolsOfSettlement(settlement, delayedPatrols) >= MaxPatrolCountPerVillage || settlement.OwnerClan != Clan.PlayerClan || playerPatrols.Count + delayedPatrols.Count >= MaxTotalPatrols)
                {
                    return false;
                }
                return true;
            }
            else if (settlement.IsTown)
            {
                //if (Hero.MainHero.IsFactionLeader && settlement.MapFaction == Hero.MainHero.MapFaction && cost <= Hero.MainHero.Gold && patrolProperties.getPatrolCount() < MaxPatrolCountPerCastle && playerPatrols.Count < MaxTotalPatrols)
                //{
                //    return true;
                //}
                if (cost > Hero.MainHero.Gold || patrolProperties.getPatrolCount() + Util.GetDelayedPatrolsOfSettlement(settlement, delayedPatrols) >= MaxPatrolCountPerTown || settlement.OwnerClan != Clan.PlayerClan || playerPatrols.Count + delayedPatrols.Count >= MaxTotalPatrols)
                {
                    return false;
                }
                return true;
            }
            else
            {
                //if (Hero.MainHero.IsFactionLeader && settlement.MapFaction == Hero.MainHero.MapFaction && cost <= Hero.MainHero.Gold && patrolProperties.getPatrolCount() < MaxPatrolCountPerCastle && playerPatrols.Count < MaxTotalPatrols)
                //{
                //    return true;
                //}
                if (cost > Hero.MainHero.Gold || patrolProperties.getPatrolCount() + Util.GetDelayedPatrolsOfSettlement(settlement, delayedPatrols) >= MaxPatrolCountPerCastle || settlement.OwnerClan != Clan.PlayerClan || playerPatrols.Count + delayedPatrols.Count >= MaxTotalPatrols)
                {
                    return false;
                }
                return true;
            }
        }

        public void UnknownBehaviorChecker()
        {
            //int count = 0;
            foreach (MobileParty party in allPatrols)
            {
                if (party.Name.ToString().EndsWith(patrolWord.ToString()) || party.Name.Contains("Patrol")) 
                {
                    if (party.Ai.AiState == AIState.Undefined) 
                    {
                        if(party.HomeSettlement.OwnerClan == Clan.PlayerClan)
                            InformationManager.DisplayMessage(new InformationMessage("UKP: " + party.Name, Colors.White));
                        party.IsVisible = false;
                        party.Party.Visuals.OnPartyRemoved();
                        party.Ai.RethinkAtNextHourlyTick = true;
                        //party.Ai.SetAIState(AIState.PatrollingAroundLocation);
                        party.SetMovePatrolAroundSettlement(party.HomeSettlement);
                        //party.Party.Visuals.SetMapIconAsDirty();
                    }
                    else if (party.Ai.AiState == AIState.Undefined && party.IsDisbanding)
                    {
                        if(party.MapEvent != null)
                        {
                            party.MapEvent.FinishBattle();
                        }
                        party.RemoveParty();
                    }
                }
            }
            //InformationManager.DisplayMessage(new InformationMessage("Unknown behavior patrols: " + count, Colors.White));
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
                            TextObject text = new TextObject("{=modbp002}{PATROLNAME} has been wiped out by {DESTROYERNAME}.");
                            text.SetTextVariable("PATROLNAME", patrol.Name);
                            text.SetTextVariable("DESTROYERNAME", destroyerParty.Name);
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
            foreach (Settlement settlement in Settlement.All)
            {
                if (settlement.OwnerClan != Clan.PlayerClan )
                {
                    if(settlement.IsVillage && settlement.Village.Hearth <= Settings.Instance.HearthMaximum && Settings.Instance.AiVillagePatrols)
                    {
                        AiPatrolGeneration(settlement);
                    }
                    else if (settlement.IsCastle && settlement.Prosperity <= Settings.Instance.AICastleProsperity && Settings.Instance.AiCastlePatrols)
                    {
                        AiPatrolGeneration(settlement);
                    }
                    else if (settlement.IsTown && settlement.Prosperity <= Settings.Instance.AITownProsperity && Settings.Instance.AiTownPatrols)
                    {
                        AiPatrolGeneration(settlement);
                    }
                }
            }
        }

        public void AiPatrolGeneration(Settlement settlement)
        {
            PatrolProperties properties;
            if (settlementPatrolProperties.ContainsKey(settlement.StringId))
            {
                settlementPatrolProperties.TryGetValue(settlement.StringId, out properties);
                if (properties != null && properties.patrols.Count < Settings.Instance.AiMaxPatrolPerSettlement && 
                    settlement.OwnerClan.Gold > (BaseCost + properties.getPatrolCost()) * 3 &&
                    Util.GetNumPatrolsOfClan(settlement.OwnerClan, settlementPatrolProperties) <= Settings.Instance.AiAdditionalPatrolToClan + settlement.OwnerClan.Tier)
                {
                    if (rand.Next(0, 100) <= Settings.Instance.AiGenerationChance)
                    {
                        MobileParty party;
                        if (rand.Next(0, 100) < 60)
                        {
                            GiveGoldAction.ApplyForCharacterToSettlement(settlement.OwnerClan.Leader, settlement, (BaseCost + properties.getPatrolCost()) * 3, true);
                            party = spawnPatrol(settlement, TroopsPerPatrol * 3);
                        }
                        else if (rand.Next(0, 100) < 85)
                        {
                            GiveGoldAction.ApplyForCharacterToSettlement(settlement.OwnerClan.Leader, settlement, (BaseCost + properties.getPatrolCost()) * 2, true);
                            party = spawnPatrol(settlement, TroopsPerPatrol * 2);
                        }
                        else
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
                //if (newOwner == Hero.MainHero || capturerHero == Hero.MainHero || oldOwner == Hero.MainHero)
                //    InformationManager.DisplayMessage(new InformationMessage("Before: " + (MaxTotalPatrols - playerPatrols.Count).ToString(), Colors.White));
                settlementPatrolProperties[settlement.StringId] = ChangePlayerPatrols(props, newOwner, capturerHero, oldOwner);
                if (settlement.BoundVillages.Any())
                {
                    foreach(Village village in settlement.BoundVillages)
                    {
                        settlementPatrolProperties.TryGetValue(village.Settlement.StringId, out props);
                        settlementPatrolProperties[village.Settlement.StringId] = ChangePlayerPatrols(props, newOwner, capturerHero, oldOwner);
                    }
                }
                if ((newOwner == Hero.MainHero || capturerHero == Hero.MainHero || oldOwner == Hero.MainHero) && MaxTotalPatrols - playerPatrols.Count - delayedPatrols.Count < 0)
                {
                    InformationManager.DisplayMessage(new InformationMessage("You are over the total patrol limit by "+ Math.Abs(MaxTotalPatrols-playerPatrols.Count - delayedPatrols.Count) + "! You must disband some patrols in order to hire new ones.", Colors.Yellow));
                    //   InformationManager.DisplayMessage(new InformationMessage("After: " + (MaxTotalPatrols - playerPatrols.Count).ToString(), Colors.Yellow));
                }
            }
        }

        public PatrolProperties ChangePlayerPatrols(PatrolProperties props, Hero newOwner, Hero capturerHero, Hero oldOwner)
        {
            foreach (MobileParty patrol in props.patrols)
            {
                patrol.Party.Owner = newOwner;
                if (newOwner == Hero.MainHero || capturerHero == Hero.MainHero)
                {
                    playerPatrols.Add(patrol);
                }
                else if (oldOwner == Hero.MainHero)
                {
                    playerPatrols.Remove(patrol);
                }
            }
            return props;
        }

        public void RemoveDuplicates()
        {
            //Dictionary<string,MobileParty> nonduplicates = new Dictionary<string, MobileParty>();
            //int count = 0;
            foreach(MobileParty party in MobileParty.All.ToList())
            {
                foreach (var troop in party.MemberRoster.Where(troop => troop.Character.IsHero))
                {
                    if(troop.Character.HeroObject.PartyBelongedTo != party && troop.Character.HeroObject == party.LeaderHero)
                    {
                        if (party.MapEvent == null)
                        {
                            InformationManager.DisplayMessage(new InformationMessage("Removed duplicate lord party: " + party.Name, Colors.Red));
                            party.RemoveParty();
                        }
                        else
                        {
                            party.MapEvent.FinishBattle();
                            InformationManager.DisplayMessage(new InformationMessage("Removed duplicate lord party: " + party.Name, Colors.Red));
                            party.RemoveParty();
                        }
                        break;
                    }
                }
            }
            //InformationManager.DisplayMessage(new InformationMessage("Duplicate Parties Left: " + count, Colors.Red));
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("settlementPatrolProperties", ref settlementPatrolProperties);
            dataStore.SyncData("allPatrols", ref allPatrols);
            dataStore.SyncData("playerPatrols", ref playerPatrols);
            //dataStore.SyncData("delayedPatrols", ref delayedPatrols);
        }
        
        public class BannerlordPatrolSaveDefiner : SaveableTypeDefiner
        {
            public BannerlordPatrolSaveDefiner() : base(91115129)
            {
            }

            protected override void DefineClassTypes()
            {
                AddClassDefinition(typeof(PatrolProperties), 1);
                AddClassDefinition(typeof(DelayedProperties), 2);
            }

            protected override void DefineContainerDefinitions()
            {
                ConstructContainerDefinition(typeof(Dictionary<string, PatrolProperties>));
                ConstructContainerDefinition(typeof(List<DelayedProperties>));
            }
        }

    }
    
}
