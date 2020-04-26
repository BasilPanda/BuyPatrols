using MBOptionScreen.Settings;
using MBOptionScreen.Attributes;
using TaleWorlds.Localization;
using MBOptionScreen.Attributes.v2;

namespace BuyPatrols
{
    public class Settings : AttributeSettings<Settings>
    {
        public override string Id { get; set; } = "BuyPatrolsSettings_v3";
        public override string ModName => "Buy Patrols";
        public override string ModuleFolderName => "zzzBuyPatrols";

        #region Main Settings

        [SettingPropertyBool(displayName:"{=modbpoption002}REMOVE ALL PATROLS", Order = 0, RequireRestart = false, HintText = "{=modbpoption003}When enabled, will attempt to delete ALL patrols that aren't engaging a party. This might take several days. Will notify the number of patrols left and destroyed.")]
        [SettingPropertyGroup("{=modbpoption001} Removal Settings")]
        public bool NukeAllPatrols { get; set; } = false;

        [SettingPropertyBool("{=modbpoption004}Remove Duplicates", Order = 1, RequireRestart = false, HintText = "{=modbpoption005}When enabled, will attempt to remove duplicate lords and their parties.")]
        [SettingPropertyGroup("{=modbpoption001} Removal Settings")]
        public bool RemoveDuplicateLords { get; set; } = false;

        [SettingPropertyBool("{=modbpoption006}Target Caravans", Order = 0, RequireRestart = false, HintText = "{=modbpoption007}When enabled, all patrols will target caravans.")]
        [SettingPropertyGroup("{=modbpoptionPatrolSettings}Patrol Settings")]
        public bool TargetCaravans { get; set; } = false;


        [SettingPropertyBool("{=modbpoption008}Target Villagers", Order = 1, RequireRestart = false, HintText = "{=modbpoption009}When enabled, all patrols will target villagers.")]
        [SettingPropertyGroup("{=modbpoptionPatrolSettings}Patrol Settings")]
        public bool TargetVillagers { get; set; } = false;

        [SettingPropertyBool("{=modbpoption010}Use Militia", Order = 2, RequireRestart = false, HintText = "{=modbpoption011}When enabled, all patrols will be militia instead of standard troops.")]
        [SettingPropertyGroup("{=modbpoptionPatrolSettings}Patrol Settings")]
        public bool UseMilitia { get; set; } = false;

        [SettingPropertyBool("{=modbpoption012}Enable Bonus Speed to Patrols", Order = 3, HintText = "{=modbpoption013}When enabled, all patrols will gain bonus speed from the add patrol speed option.")]
        [SettingPropertyGroup("{=modbpoptionPatrolSettings}Patrol Settings")]
        public bool AddPatrolSpeedEnabled { get; set; } = true;


        [SettingPropertyBool("{=modbpoption014}Patrol Wages Hint Box", Order = 4, HintText = "{=modbpoption015}When enabled, all daily patrols wages will be included the clan expenses. When it is disabled, patrol wages will be notified on the bottom left.")]
        [SettingPropertyGroup("{=modbpoptionPatrolSettings}Patrol Settings")]
        public bool PatrolWagesHintBox { get; set; } = true;
        
        
        [SettingPropertyBool("{=modbpoption016}Notify Patrol Destroyed", Order = 5, HintText = "{=modbpoption017}When enabled, will send a notification on the bottom left when a personal patrol has been destroyed. Only applies for new patrols.")]
        [SettingPropertyGroup("{=modbpoptionPatrolSettings}Patrol Settings")]
        public bool NotifyDestroyedPatrol { get; set; } = true;

        /* for now impossible
        [SettingProperty("Auto Buy on Patrol Destroy", "When enabled, will automatically hire a new medium or small patrol if you have the money.")]
        [SettingPropertyGroup("Patrol Settings")]
        public bool AutoBuyDestroyedPatrol { get; set; } = true;
        */

        [SettingPropertyFloatingInteger("{=modbpoption018}Personal Patrol Bonus Speed", minValue:0, maxValue:10, Order = 6, RequireRestart = false, HintText = "{=modbpoption019}The bonus speed that will be added to patrols within your clan.")]
        [SettingPropertyGroup("{=modbpoptionPatrolSettings}Patrol Settings")]
        public float AddPatrolSpeed { get; set; } = 3.5f;


        [SettingPropertyInteger("{=modbpoption058}Base Cost to Patrols", 0, 50000, Order = 7, RequireRestart = false, HintText = "{=modbpoption059}The base cost to patrols. The total cost of hiring a patrol is a combination of base cost + hearth or base cost + prosperity.")]
        [SettingPropertyGroup("{=modbpoptionPatrolSettings}Patrol Settings")]
        public int BaseCost { get; set; } = 1250;


        [SettingPropertyFloatingInteger("{=modbpoption020}Patrol Wage Modifier", 0, 4, Order = 8, RequireRestart = false, HintText = "{=modbpoption021}Modifies the daily wage amount. 0 makes it so there are no patrol wages to pay.")]
        [SettingPropertyGroup("{=modbpoptionPatrolSettings}Patrol Settings")]
        public float DailyPatrolWageModifier { get; set; } = 0.75f;


        [SettingPropertyInteger("{=modbpoption022}Max Total Patrols", 0, 255, Order = 9, RequireRestart = false, HintText = "{=modbpoption023}Modifies the max amount of patrols that players can have at one time. The more patrols in your map can cause unforeseen bugs and issues.")]
        [SettingPropertyGroup("{=modbpoptionPatrolSettings}Patrol Settings")]
        public int MaxTotalPatrols { get; set; } = 12;


        [SettingPropertyInteger("{=modbpoption024}Patrols per Village", 0, 30, Order = 10, RequireRestart = false, HintText = "{=modbpoption025}Modifies the max amount of patrols per village.")]
        [SettingPropertyGroup("{=modbpoptionPatrolSettings}Patrol Settings")]
        public int MaxPatrolCountPerVillage { get; set; } = 3;


        [SettingPropertyInteger("{=modbpoption026}Patrols per Castle", 0, 30, Order = 11, RequireRestart = false, HintText = "{=modbpoption027}Modifies the max amount of patrols per castle.")]
        [SettingPropertyGroup("{=modbpoptionPatrolSettings}Patrol Settings")]
        public int MaxPatrolCountPerCastle { get; set; } = 3;

        [SettingPropertyInteger("{=modbpoption028}Patrols per Town", 0, 30, Order = 12, RequireRestart = false, HintText = "{=modbpoption029}Modifies the max amount of patrols per town.")]
        [SettingPropertyGroup("{=modbpoptionPatrolSettings}Patrol Settings")]
        public int MaxPatrolCountPerTown{ get; set; } = 1;


        [SettingPropertyInteger("{=modbpoption030}Base Patrol Size", 4, 64, Order = 13, RequireRestart = false, HintText = "{=modbpoption031}Modifies the base average men per patrol. This affects all sizes. Medium is 2x and large is 3x this number")]
        [SettingPropertyGroup("{=modbpoptionPatrolSettings}Patrol Settings")]
        public int TroopsPerPatrol { get; set; } = 16;

        #endregion

        #region Relation Settings


        [SettingPropertyBool("{=modbpoption032}Allow Relation Gain", Order = 0, RequireRestart = false, HintText = "{=modbpoption033}When enabled, a daily 10% chance to gain relation with notables that have patrols.")]
        [SettingPropertyGroup("{=modbpoptionRelationSettings}Relation Settings")]
        public bool IncreaseNotableRelations { get; set; } = true;


        [SettingPropertyBool("{=modbpoption034}Notify Relation Change", Order = 1, RequireRestart = false, HintText = "{=modbpoption035}When enabled, will send a notification on each relation change due to having patrols.")]
        [SettingPropertyGroup("{=modbpoptionRelationSettings}Relation Settings")]
        public bool NotifyNotableRelations { get; set; } = false;


        [SettingPropertyInteger("{=modbpoption036}Notable Relation Cap", -100, 100, Order = 2, RequireRestart = false, HintText =  "{=modbpoption037}The relation cap for having patrols per notable. Relation gain will stop after reaching this value.")]
        [SettingPropertyGroup("{=modbpoptionRelationSettings}Relation Settings")]
        public int RelationCap { get; set; } = 25;

        #endregion

        #region Misc Settings


        [SettingPropertyBool("{=modbpoption038}Force Troop Capacity", Order = 0, HintText =  "{=modbpoption039}When enabled, forces patrols to be capped at large patrol size. Will randomly remove troops over the large patrol size.")]
        [SettingPropertyGroup("{=modbpoptionMiscSettings}Miscellaneous")]
        public bool ForceTroopCapEnabled { get; set; } = false;


        [SettingPropertyBool("{=modbpoption040}Patrol Regeneration", Order = 1, HintText =  "{=modbpoption041}When enabled, allows patrols to slowly regenerate on a daily basis up to large size.")]
        [SettingPropertyGroup("{=modbpoptionMiscSettings}Miscellaneous")]
        public bool ForceRegenPatrol { get; set; } = false;


        [SettingPropertyInteger("{=modbpoption042}Patrol Tether Range", 5, 50, Order = 2, RequireRestart = false, HintText =  "{=modbpoption043}The maximum distance a patrol will go before being pulled back to their home settlement.")]
        [SettingPropertyGroup("{=modbpoptionMiscSettings}Miscellaneous")]
        public int PatrolTetherRange { get; set; } = 15;

        #endregion

        #region Lord Patrols

        [SettingPropertyBool("{=modbpoption044}Other Lords Hire", Order = 0, RequireRestart = false, HintText =  "{=modbpoption045}When enabled, other lords will try to hire patrols for their villages.")]
        [SettingPropertyGroup("{=modbpoptionAISettings}Ai Hiring Settings")]
        public bool AiHirePatrols { get; set; } = true;


        [SettingPropertyInteger("{=modbpoption046}Patrol Hiring Chance", 1, 100, Order = 1, RequireRestart = false, HintText =  "{=modbpoption047}The daily chance that other lords will hire a patrol per village if it is below the settlement limit.")]
        [SettingPropertyGroup("{=modbpoptionAISettings}Ai Hiring Settings")]
        public int AiGenerationChance { get; set; } = 5;


        [SettingPropertyInteger("{=modbpoption048}Hearth Limit", 250, 2000, Order = 2, RequireRestart = false, HintText =  "{=modbpoption049}If a village's hearth is below this number, the lord will run the chance to spawn a patrol for it.")]
        [SettingPropertyGroup("{=modbpoptionAISettings}Ai Hiring Settings")]
        public int HearthMaximum { get; set; } = 500;

        [SettingPropertyInteger("{=modbpoption050}Castle Prosperity Limit", 250, 10000, Order = 3, RequireRestart = false, HintText =  "{=modbpoption051}If a castle's prosperity is below this number, the lord will run the chance to spawn a patrol for it.")]
        [SettingPropertyGroup("{=modbpoptionAISettings}Ai Hiring Settings")]
        public int AICastleProsperity { get; set; } = 1000;

        [SettingPropertyInteger("{=modbpoption052}Town Prosperity Limit", 250, 10000, Order = 4, RequireRestart = false, HintText =  "{=modbpoption053}If a town's prosperity is below this number, the lord will run the chance to spawn a patrol for it.")]
        [SettingPropertyGroup("{=modbpoptionAISettings}Ai Hiring Settings")]
        public int AITownProsperity { get; set; } = 2000;

        [SettingPropertyFloatingInteger("{=modbpoption054}Patrol Bonus Speed", 0, 10, Order = 5, RequireRestart = false, HintText =  "{=modbpoption055}The bonus speed that will be added to AI hired patrols.")]
        [SettingPropertyGroup("{=modbpoptionAISettings}Ai Hiring Settings")]
        public float AddPatrolSpeedForAi { get; set; } = 0.5f;


        [SettingPropertyInteger("{=modbpoption056}Max Patrols Per Settlement", 0, 10, Order = 6, RequireRestart = false, HintText =  "{=modbpoption057}The maximum amount of patrols the AI will hire per settlement.")]
        [SettingPropertyGroup("{=modbpoptionAISettings}Ai Hiring Settings")]
        public int AiMaxPatrolPerSettlement { get; set; } = 1;

        [SettingPropertyInteger("{=modbpoption060}Additional Patrols", 0, 15, Order = 7, RequireRestart = false, HintText =  "{=modbpoption061}The base amount of patrols the AI will hire under their clan. This is added to their current clan tier for the cap.")]
        [SettingPropertyGroup("{=modbpoptionAISettings}Ai Hiring Settings")]
        public int AiAdditionalPatrolToClan { get; set; } = 0;

        #endregion
    }

}
