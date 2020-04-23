using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.SandBox.GameComponents;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace BuyPatrols
{
    class CalculateClanExpensesForPatrols : DefaultClanFinanceModel
    {
        public override void CalculateClanExpenses(Clan clan, ref ExplainedNumber goldChange, bool applyWithdrawals = false)
        {
            base.CalculateClanExpenses(clan, ref goldChange, applyWithdrawals);
            int total = 0;
            int partyWage = 0;
            int payAmount = 0;
            foreach (MobileParty party in clan.Parties)
            {
                partyWage = 0;
                payAmount = 0;
                if (party.Name.ToString().EndsWith("{=modbp015}Patrol"))
                {
                    partyWage = (int)(party.GetTotalWage() * Settings.Instance.DailyPatrolWageModifier);
                    total += partyWage;
                    if (applyWithdrawals && party.LeaderHero != null)
                    {
                        float currentClanGold = (float)clan.Gold + goldChange.ResultNumber;
                        if(currentClanGold < partyWage)
                        {
                            payAmount = (int)currentClanGold;
                        }
                        else
                        {
                            GiveGoldAction.ApplyBetweenCharacters(null, party.LeaderHero, partyWage, true);
                            payAmount = partyWage;
                        } 
                        ApplyMoraleEffect(party, partyWage, payAmount);
                    }
                }
            }
            if(total > 0)
            {
                TextObject textObject = new TextObject("Total patrol wages", null);
                goldChange.Add((float)(-(float)total), textObject);
            }
        }

        private static void ApplyMoraleEffect(MobileParty mobileParty, int wage, int paymentAmount)
        {
            if (paymentAmount < wage)
            {
                float num = 1f - (float)paymentAmount / (float)wage;
                float num2 = (float)Campaign.Current.Models.PartyMoraleModel.GetDailyNoWageMoralePenalty(mobileParty) * num;
                if (mobileParty.HasUnpaidWages < num)
                {
                    num2 += (float)Campaign.Current.Models.PartyMoraleModel.GetDailyNoWageMoralePenalty(mobileParty) * (num - mobileParty.HasUnpaidWages);
                }
                mobileParty.RecentEventsMorale += num2;
                mobileParty.HasUnpaidWages = num;
                MBTextManager.SetTextVariable("reg1", (float)Math.Round((double)Math.Abs(num2), 1), false);
                if (mobileParty == MobileParty.MainParty)
                {
                    InformationManager.AddQuickInformation(GameTexts.FindText("str_party_loses_moral_due_to_insufficent_funds", null), 0, null, "");
                    return;
                }
            }
            else
            {
                mobileParty.HasUnpaidWages = 0f;
            }
        }
    }
}
