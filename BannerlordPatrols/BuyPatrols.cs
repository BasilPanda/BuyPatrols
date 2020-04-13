using System;
using System.Reflection;
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
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors;
using System.Windows.Forms;
using TaleWorlds.CampaignSystem.SandBox;
using TaleWorlds.CampaignSystem.SandBox.GameComponents.Map;
using TaleWorlds.CampaignSystem.SandBox.GameComponents;

namespace BuyPatrols
{
    public class BuyPatrols : CampaignBehaviorBase
    {
        Dictionary<string, PatrolProperties> settlementPatrolProperties = new Dictionary<string, PatrolProperties>();
        List<MobileParty> allPatrols = new List<MobileParty>();
        Random rand = new Random();
        MobilePartiesAroundPositionList partiesAroundPosition = new MobilePartiesAroundPositionList(32);
        DefaultMapDistanceModel d = new DefaultMapDistanceModel();
        #region Settings
        public bool ForceRegenPatrol = bool.Parse(Settings.LoadSetting("ForceRegenPatrol"));
        public bool ForceTroopCapEnabled = bool.Parse(Settings.LoadSetting("ForceTroopCapEnabled"));
        public int TroopsPerPatrol = int.Parse(Settings.LoadSetting("TroopsPerPatrol"));
        public int BaseCost = int.Parse(Settings.LoadSetting("BaseCost"));
        public float DailyPatrolWageModifier = float.Parse(Settings.LoadSetting("DailyPatrolWageModifier"));
        public int PatrolTetherRange = int.Parse(Settings.LoadSetting("PatrolTetherRange"));
        public int MaxPatrolCountPerVillage = int.Parse(Settings.LoadSetting("MaxPatrolCountPerVillage"));
        public int MaxPatrolCountPerCastle = 0;//int.Parse(Settings.LoadSetting("MaxPatrolCountPerCastle"));
        #endregion

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
                //AddCastleDialog(obj);
            } catch (Exception e)
            {
                MessageBox.Show("Something screwed up in adding patrol dialog. " + e.ToString());
            }
            
        }

        private void OnDailyTick()
        {
            PatrolDailyAi();
            if (!bool.Parse(Settings.LoadSetting("PatrolWagesHintBox")))
            {
                PayPatrols();
            }
            if (bool.Parse(Settings.LoadSetting("IncreaseNotableRelations")))
            {
                increaseRelations();
            }
        }
        
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionLaunched));
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, new Action(this.PatrolHourlyAi));
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, new Action(this.OnDailyTick));
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, new Action<MobileParty, Settlement, Hero>(this.OnSettlementEntered));
            //CampaignEvents.DailyTickClanEvent.AddNonSerializedListener(this, new Action<Clan>(this.OnDailyTickClan));

        }

        public void AddPatrolMenu(CampaignGameStarter obj)
        {
            try
            {
                obj.AddGameMenuOption("village", "basilpatrol_manage_patrol", "Manage patrols",
                    (MenuCallbackArgs args) =>
                    {
                        args.optionLeaveType = GameMenuOption.LeaveType.Recruit;
                        /*
                        if (Settlement.CurrentSettlement.OwnerClan != Clan.PlayerClan)
                        {
                            return false;
                        }
                        */
                        return true;
                    },
                    (MenuCallbackArgs args) => { GameMenu.SwitchToMenu("basilpatrol_pay_menu"); }, false, 4);

                obj.AddGameMenu("basilpatrol_pay_menu", "The {BASILPATROL_SETTLEMENT_TYPE} says they have the manpower but not the gear to " +
                    "send men on patrols. He tells he can send up to {BASILPATROL_MAX_PATROL_AMOUNT} patrols if you are willing to pay their gear. Your clan must own the village to hire patrols. You can disband patrols from villages even if you no longer own it anymore.",
                    (MenuCallbackArgs args) =>
                    {
                        if (Settlement.CurrentSettlement.IsVillage)
                        {
                            MBTextManager.SetTextVariable("BASILPATROL_SETTLEMENT_TYPE", "village spokesman", false);
                            MBTextManager.SetTextVariable("BASILPATROL_MAX_PATROL_AMOUNT", MaxPatrolCountPerVillage, false);
                        }
                        else
                        {
                            MBTextManager.SetTextVariable("BASILPATROL_SETTLEMENT_TYPE", "castle sergeant", false);
                            MBTextManager.SetTextVariable("BASILPATROL_MAX_PATROL_AMOUNT", MaxPatrolCountPerCastle, false);
                        }
                    });

                #region Hiring

                obj.AddGameMenuOption("basilpatrol_pay_menu", "basilpatrol_pay_small", "Pay for a small patrol ({BASILPATROL_SMALL_COST}{GOLD_ICON})",
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
                            if (Settlement.CurrentSettlement.IsVillage)
                            {
                                if (cost >= Hero.MainHero.Gold || patrolProperties.getPatrolCount() >= MaxPatrolCountPerVillage || Settlement.CurrentSettlement.OwnerClan != Clan.PlayerClan)
                                {
                                    return false;
                                }
                                return true;
                            }
                            else
                            {
                                if (cost >= Hero.MainHero.Gold || patrolProperties.getPatrolCount() >= MaxPatrolCountPerCastle || Settlement.CurrentSettlement.OwnerClan != Clan.PlayerClan)
                                {
                                    return false;
                                }
                                return true;
                            }
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
                            if (cost <= Hero.MainHero.Gold)
                            {
                                GiveGoldAction.ApplyForCharacterToSettlement(Hero.MainHero, Settlement.CurrentSettlement, cost);
                                MobileParty party = spawnPatrol(Settlement.CurrentSettlement, TroopsPerPatrol);
                                patrolProperties.patrols.Add(party);
                                settlementPatrolProperties[settlementID] = patrolProperties;
                                allPatrols.Add(party);
                            }
                            if (Settlement.CurrentSettlement.IsVillage)
                            {
                                GameMenu.SwitchToMenu("village");
                            }
                            else
                            {
                                GameMenu.SwitchToMenu("castle");
                            }
                        } catch(Exception e)
                        {
                            MessageBox.Show("Error in small purchase..." + e.ToString());
                        }
                        
                    });

                obj.AddGameMenuOption("basilpatrol_pay_menu", "basilpatrol_pay_medium", "Pay for a medium patrol ({BASILPATROL_MEDIUM_COST}{GOLD_ICON})",
                    (MenuCallbackArgs args) =>
                    {
                        try
                        {
                            args.optionLeaveType = GameMenuOption.LeaveType.Recruit;
                            PatrolProperties patrolProperties;
                            string settlementID = Settlement.CurrentSettlement.StringId;
                            settlementPatrolProperties.TryGetValue(settlementID, out patrolProperties);
                            int cost = BaseCost * 2 + patrolProperties.getPatrolCost() * 2;
                            MBTextManager.SetTextVariable("BASILPATROL_MEDIUM_COST", cost, false);
                            MBTextManager.SetTextVariable("GOLD_ICON", "<img src=\"Icons\\Coin@2x\">");
                            //MBTextManager.SetTextVariable("BASILPATROL_COST", patrolProperties.getPatrolCost, false);
                            if (Settlement.CurrentSettlement.IsVillage)
                            {
                                if (cost >= Hero.MainHero.Gold || patrolProperties.getPatrolCount() >= MaxPatrolCountPerVillage || Settlement.CurrentSettlement.OwnerClan != Clan.PlayerClan)
                                {
                                    return false;
                                }
                                return true;
                            }
                            else
                            {
                                if (cost >= Hero.MainHero.Gold || patrolProperties.getPatrolCount() >= MaxPatrolCountPerCastle || Settlement.CurrentSettlement.OwnerClan != Clan.PlayerClan)
                                {
                                    return false;
                                }
                                return true;
                            }
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
                            int cost = BaseCost * 2 + patrolProperties.getPatrolCost() * 2;
                            if (cost <= Hero.MainHero.Gold)
                            {
                                GiveGoldAction.ApplyForCharacterToSettlement(Hero.MainHero, Settlement.CurrentSettlement, cost);
                                MobileParty party = spawnPatrol(Settlement.CurrentSettlement, TroopsPerPatrol * 2);
                                patrolProperties.patrols.Add(party);
                                settlementPatrolProperties[settlementID] = patrolProperties;
                                allPatrols.Add(party);
                            }
                            if (Settlement.CurrentSettlement.IsVillage)
                            {
                                GameMenu.SwitchToMenu("village");
                            }
                            else
                            {
                                GameMenu.SwitchToMenu("castle");
                            }
                        } catch (Exception e)
                        {
                            MessageBox.Show("Error in medium purchase... " + e.ToString());
                        }
                        
                    });

                obj.AddGameMenuOption("basilpatrol_pay_menu", "basilpatrol_pay_large", "Pay for a large patrol ({BASILPATROL_LARGE_COST}{GOLD_ICON})",
                    (MenuCallbackArgs args) =>
                    {
                        try
                        {
                            args.optionLeaveType = GameMenuOption.LeaveType.Recruit;
                            PatrolProperties patrolProperties;
                            string settlementID = Settlement.CurrentSettlement.StringId;
                            settlementPatrolProperties.TryGetValue(settlementID, out patrolProperties);
                            int cost = BaseCost * 3 + patrolProperties.getPatrolCost() * 3;
                            MBTextManager.SetTextVariable("BASILPATROL_LARGE_COST", cost, false);
                            MBTextManager.SetTextVariable("GOLD_ICON", "<img src=\"Icons\\Coin@2x\">");
                            //MBTextManager.SetTextVariable("BASILPATROL_COST", patrolProperties.getPatrolCost, false);
                            if (Settlement.CurrentSettlement.IsVillage)
                            {
                                if (cost >= Hero.MainHero.Gold || patrolProperties.getPatrolCount() >= MaxPatrolCountPerVillage || Settlement.CurrentSettlement.OwnerClan != Clan.PlayerClan)
                                {
                                    return false;
                                }
                                return true;
                            }
                            else
                            {
                                if (cost >= Hero.MainHero.Gold || patrolProperties.getPatrolCount() >= MaxPatrolCountPerCastle || Settlement.CurrentSettlement.OwnerClan != Clan.PlayerClan)
                                {
                                    return false;
                                }
                                return true;
                            }
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
                            int cost = BaseCost * 3 + patrolProperties.getPatrolCost() * 3;
                            if (cost <= Hero.MainHero.Gold)
                            {
                                GiveGoldAction.ApplyForCharacterToSettlement(Hero.MainHero, Settlement.CurrentSettlement, cost);
                                MobileParty party = spawnPatrol(Settlement.CurrentSettlement, TroopsPerPatrol * 3);
                                patrolProperties.patrols.Add(party);
                                settlementPatrolProperties[settlementID] = patrolProperties;
                                allPatrols.Add(party);
                            }
                            if (Settlement.CurrentSettlement.IsVillage)
                            {
                                GameMenu.SwitchToMenu("village");
                            }
                            else
                            {
                                GameMenu.SwitchToMenu("castle");
                            }
                        }
                        catch(Exception e)
                        {
                            MessageBox.Show("Error in large purchase..." + e.ToString());
                        }
                        
                    });
                #endregion

                obj.AddGameMenuOption("basilpatrol_pay_menu", "basilpatrol_disband_all", "Disband all patrols",
                    (MenuCallbackArgs args) =>
                    {
                        try
                        {
                            args.optionLeaveType = GameMenuOption.LeaveType.Manage;
                            PatrolProperties patrolProperties;
                            if (Settlement.CurrentSettlement.IsVillage)
                            {
                                if (settlementPatrolProperties.TryGetValue(Settlement.CurrentSettlement.StringId, out patrolProperties))
                                {
                                    if (patrolProperties.patrols.Count > 0)
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
                            if (Settlement.CurrentSettlement.IsVillage)
                            {
                                if (settlementPatrolProperties.TryGetValue(Settlement.CurrentSettlement.StringId, out patrolProperties))
                                {
                                    if (patrolProperties.patrols.Count > 0)
                                    {

                                        foreach (MobileParty patrol in patrolProperties.patrols.ToList())
                                        {
                                            allPatrols.Remove(patrol);
                                            patrol.RemoveParty();
                                        }
                                        patrolProperties.patrols.Clear();
                                        settlementPatrolProperties[Settlement.CurrentSettlement.StringId] = patrolProperties;
                                    }
                                }
                            }
                            if (Settlement.CurrentSettlement.IsVillage)
                            {
                                GameMenu.SwitchToMenu("village");
                            }
                            else
                            {
                                GameMenu.SwitchToMenu("castle");
                            }
                        }
                        catch(Exception e)
                        {
                            MessageBox.Show("Error in disbanding all patrols..." + e.ToString());
                        }
                        
                    });

                obj.AddGameMenuOption("basilpatrol_pay_menu", "basilpatrol_leave", "Leave", game_menu_just_add_leave_conditional, game_menu_switch_to_village_menu);
            } catch(Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }

        public void AddCastleDialog(CampaignGameStarter obj)
        {
            obj.AddGameMenuOption("castle", "basilpatrol_castle_patrol", "Hire a patrol", (MenuCallbackArgs args) =>
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Recruit;
                if (Settlement.CurrentSettlement.OwnerClan != Clan.PlayerClan)
                {
                    return false;
                }
                return true;
            },
            (MenuCallbackArgs args) => { GameMenu.SwitchToMenu("basilpatrol_pay_menu"); }, false, 4);
        }

        public void AddPatrolDialog(CampaignGameStarter obj)
        {
            obj.AddDialogLine("mod_buypatrols_talk_start", "start", "mod_buypatrols_talk", "Hello my lord. What do you need us to do?", new ConversationSentence.OnConditionDelegate(this.patrol_talk_start_on_conditional), null, 100, null);
            obj.AddPlayerLine("mod_buypatrols_donate_troops", "mod_buypatrols_talk", "mod_buypatrols_after_donate", "Donate Troops", null, new ConversationSentence.OnConsequenceDelegate(this.conversation_patrol_donate_troops_on_consequence), 100, null, null);
            obj.AddPlayerLine("mod_buypatrols_disband", "mod_buypatrols_talk", "close_window", "Disband.", null, new ConversationSentence.OnConsequenceDelegate(this.conversation_patrol_disband_on_consequence), 100, null, null);
            obj.AddPlayerLine("mod_buypatrols_leave", "mod_buypatrols_talk", "close_window", "Carry on, then. Farewell.", null, new ConversationSentence.OnConsequenceDelegate(this.conversation_patrol_leave_on_consequence), 100, null, null);
            obj.AddDialogLine("mod_buypatrols_after_donate", "mod_buypatrols_after_donate", "mod_buypatrols_talk", "Anything else?", null, null, 100, null);

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
                    encounteredParty.IsMobile && encounteredParty.Name.Contains("Patrol") && encounteredParty.IsActive)
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

        #endregion

        #region Consequences

        private void game_menu_switch_to_village_menu(MenuCallbackArgs args)
        {
            if(Settlement.CurrentSettlement.IsVillage) 
            {
                GameMenu.SwitchToMenu("village");
            }else
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

        #endregion

        private void trackPatrols()
        {
            foreach(Settlement settlement in Settlement.All)
            {
                if(settlement.IsVillage)
                {
                    if(!settlementPatrolProperties.ContainsKey(settlement.StringId))
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

        public void PatrolHourlyAi()
        {
            
            PatrolProperties patrolProperties;
            foreach(Settlement settlement in Settlement.All)
            {
                if (settlement.IsVillage)
                {
                    settlementPatrolProperties.TryGetValue(settlement.StringId, out patrolProperties);
                    patrolProperties.patrols.RemoveAll(x => x.MemberRoster.Count == 0);
                    allPatrols.Clear();
                    bool flag = true;
                    foreach (MobileParty patrol in patrolProperties.patrols.ToList())
                    {
                        flag = true;
                        if (patrol.PrisonRoster.Count > patrol.MemberRoster.Count / 4)
                        {
                            IEnumerable<Settlement> playerSettlements = Clan.PlayerClan.Settlements;
                            if (playerSettlements != null)
                            {
                                Settlement quickestTown = null;
                                float shortestDistance = 999999;
                                foreach (Settlement s in playerSettlements)
                                {
                                    if (s.OwnerClan == Clan.PlayerClan && (s.IsTown || s.IsCastle) && d.GetDistance(patrol, s) <= 30)
                                    {
                                        if(d.GetDistance(patrol, s) < shortestDistance)
                                        {
                                            quickestTown = s;
                                        }
                                    }
                                }
                                if(quickestTown != null)
                                {
                                    patrol.SetMoveGoToSettlement(quickestTown);
                                }
                            }
                        }
                        if (patrol.DefaultBehavior == AiBehavior.EngageParty || patrol.DefaultBehavior == AiBehavior.FleeToPoint || patrol.DefaultBehavior == AiBehavior.GoToSettlement)
                        {
                            if(patrol.DefaultBehavior == AiBehavior.EngageParty)
                            {

                                if(d.GetDistance(patrol, patrol.HomeSettlement) > PatrolTetherRange)
                                {
                                    patrol.SetMoveGoToSettlement(patrol.HomeSettlement);
                                }
                            }
                            continue;
                        }
                        List<MobileParty> parties = partiesAroundPosition.GetPartiesAroundPosition(patrol.Position2D, 10f);
                        foreach(MobileParty possibleEnemy in parties.ToList())
                        {
                            if(patrol.IsActive && possibleEnemy.MapFaction.IsAtWarWith(patrol.MapFaction) && possibleEnemy.IsActive && 
                                !possibleEnemy.IsGarrison && (possibleEnemy.IsMoving || possibleEnemy.IsEngaging  || possibleEnemy.IsRaiding))

                            {
                                if(patrol.Party.TotalStrength > possibleEnemy.Party.TotalStrength)
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
                                patrol.SetMoveGoToSettlement(patrol.HomeSettlement);
                            } else
                            {
                                patrol.SetMovePatrolAroundSettlement(patrol.HomeSettlement);
                            }
                        }
                    }
                    allPatrols.AddRange(patrolProperties.patrols);
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

                    foreach (MobileParty patrol in patrolProperties.patrols.ToList())
                    {
                        if (patrol.Food <= 3)
                        {
                            generateFood(patrol);
                        }
                        if (patrol.MemberRoster.Count > TroopsPerPatrol * 3 * 1.1 && ForceTroopCapEnabled)
                        {
                            patrol.MemberRoster.KillNumberOfMenRandomly((int)Math.Floor(patrol.MemberRoster.Count - TroopsPerPatrol * 3 * 1.1), false);
                        }
                        if (patrol.MemberRoster.Count < TroopsPerPatrol * 3 * 1.1 && ForceRegenPatrol)
                        {
                            patrol.AddElementToMemberRoster(patrol.Party.Culture.BasicTroop, 1, false);
                        }
                    }
                    allPatrols.AddRange(patrolProperties.patrols);
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
                if (settlement.IsVillage)
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

        public void InitPatrolParty(MobileParty patrolParty, TextObject name, Clan faction, Settlement homeSettlement)
        {
            patrolParty.Name = name;
            patrolParty.IsMilitia = true;
            patrolParty.HomeSettlement = homeSettlement;
            patrolParty.Party.Owner = faction.Leader;
            patrolParty.SetInititave(1f, 0f, 100000000f);
            patrolParty.Party.Visuals.SetMapIconAsDirty();
            generateFood(patrolParty);
        }

        public void generateFood(MobileParty patrolParty)
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

        public void increaseRelations()
        {
            PatrolProperties patrolProperties;
            bool flag = false;
            foreach (Settlement settlement in Settlement.All)
            {
                if (settlement.IsVillage)
                {
                    settlementPatrolProperties.TryGetValue(settlement.StringId, out patrolProperties);
                    if (patrolProperties.patrols.Count > 0)
                    {
                        foreach (Hero notable in settlement.Notables)
                        {
                            // 10% chance of apply increase in relations
                            if (rand.Next(0, 100) < 10)
                            {
                                ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, notable, rand.Next(1, patrolProperties.patrols.Count * 2), true);
                                flag = true;
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
            if(mobileParty != null && mobileParty.IsActive && mobileParty.Name.Contains("Patrol"))
            {
                if(settlement.IsTown || settlement.IsCastle)
                {
                    MobileParty garrisonParty = settlement.Town.GarrisonParty;
                    if (garrisonParty == null)
                    {
                        settlement.AddGarrisonParty(false);
                        garrisonParty = settlement.Town.GarrisonParty;
                    }
                    for (int i = 0; i < mobileParty.PrisonRoster.Count; i++)
                    {
                        TroopRosterElement prisoner = mobileParty.PrisonRoster.GetElementCopyAtIndex(i);
                        if (prisoner.Character.IsHero)
                        {
                            EnterSettlementAction.ApplyForPrisoner(prisoner.Character.HeroObject, settlement);
                        }
                        garrisonParty.AddPrisoner(prisoner.Character, prisoner.Number);
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
