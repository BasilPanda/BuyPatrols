using MBOptionScreen.Settings;
using MBOptionScreen.Attributes;

namespace BuyPatrols
{
    public class Settings : AttributeSettings<Settings>
    {
        public override string Id { get; set; } = "BuyPatrolsSettings_v2";
        public override string ModName => "Buy Patrols";
        public override string ModuleFolderName => "zzzBuyPatrols";

        #region Main Settings

        [SettingProperty("REMOVE ALL PATROLS", false, hintText: "When enabled, will attempt to delete ALL patrols that aren't engaging a party. This might take several days. Will notify the number of patrols left and destroyed.")]
        [SettingPropertyGroup(" Removal Settings")]
        public bool NukeAllPatrols { get; set; } = false;

        [SettingProperty("Target Caravans", false, hintText: "When enabled, all patrols will target caravans.")]
        [SettingPropertyGroup("Patrol Settings")]
        public bool TargetCaravans { get; set; } = false;


        [SettingProperty("Target Villagers", false, hintText: "When enabled, all patrols will target villagers.")]
        [SettingPropertyGroup("Patrol Settings")]
        public bool TargetVillagers { get; set; } = false;

        [SettingProperty("Use Militia", false, hintText: "When enabled, all patrols will be militia instead of standard troops.")]
        [SettingPropertyGroup("Patrol Settings")]
        public bool UseMilitia { get; set; } = false;

        [SettingProperty("Enable Bonus Speed to Patrols", hintText: "When enabled, all patrols will gain bonus speed from the add patrol speed option.")]
        [SettingPropertyGroup("Patrol Settings")]
        public bool AddPatrolSpeedEnabled { get; set; } = true;


        [SettingProperty("Patrol Wages Hint Box", hintText: "When enabled, all daily patrols wages will be included the clan expenses. When it is disabled, patrol wages will be notified on the bottom left.")]
        [SettingPropertyGroup("Patrol Settings")]
        public bool PatrolWagesHintBox { get; set; } = true;
        
        
        [SettingProperty("Notify Patrol Destroyed", hintText:"When enabled, will send a notification on the bottom left when a personal patrol has been destroyed. Only applies for new patrols.")]
        [SettingPropertyGroup("Patrol Settings")]
        public bool NotifyDestroyedPatrol { get; set; } = true;

        /* for now impossible
        [SettingProperty("Auto Buy on Patrol Destroy", "When enabled, will automatically hire a new medium or small patrol if you have the money.")]
        [SettingPropertyGroup("Patrol Settings")]
        public bool AutoBuyDestroyedPatrol { get; set; } = true;
        */

        [SettingProperty("Personal Patrol Bonus Speed", 0, 10, false, hintText: "The bonus speed that will be added to patrols within your clan.")]
        [SettingPropertyGroup("Patrol Settings")]
        public float AddPatrolSpeed { get; set; } = 3.5f;


        [SettingProperty("Base Cost to Patrols", 0, 50000, false, hintText: "The base cost to patrols. The total cost of hiring a patrol is a combination of base cost + hearth or base cost + prosperity.")]
        [SettingPropertyGroup("Patrol Settings")]
        public int BaseCost { get; set; } = 1250;


        [SettingProperty("Patrol Wage Modifier", 0, 4, false, hintText: "Modifies the daily wage amount. 0 makes it so there are no patrol wages to pay.")]
        [SettingPropertyGroup("Patrol Settings")]
        public float DailyPatrolWageModifier { get; set; } = 0.75f;


        [SettingProperty("Max Total Patrols", 0, 255, false, hintText: "Modifies the max amount of patrols that players can have at one time. The more patrols in your map can cause unforeseen bugs and issues.")]
        [SettingPropertyGroup("Patrol Settings")]
        public int MaxTotalPatrols { get; set; } = 12;


        [SettingProperty("Patrols per Village", 0, 30, false, hintText: "Modifies the max amount of patrols per village.")]
        [SettingPropertyGroup("Patrol Settings")]
        public int MaxPatrolCountPerVillage { get; set; } = 3;


        [SettingProperty("Patrols per Castle", 0, 30, false, hintText: "Modifies the max amount of patrols per castle.")]
        [SettingPropertyGroup("Patrol Settings")]
        public int MaxPatrolCountPerCastle { get; set; } = 3;

        [SettingProperty("Patrols per Town", 0, 30, false, hintText: "Modifies the max amount of patrols per town.")]
        [SettingPropertyGroup("Patrol Settings")]
        public int MaxPatrolCountPerTown{ get; set; } = 1;


        [SettingProperty("Base Patrol Size", 4, 64, false, hintText: "Modifies the base average men per patrol. This affects all sizes. Medium is 2x and large is 3x this number")]
        [SettingPropertyGroup("Patrol Settings")]
        public int TroopsPerPatrol { get; set; } = 16;

        #endregion

        #region Relation Settings


        [SettingProperty("Allow Relation Gain", false, hintText: "When enabled, a daily 10% chance to gain relation with notables that have patrols.")]
        [SettingPropertyGroup("Relation Settings")]
        public bool IncreaseNotableRelations { get; set; } = true;


        [SettingProperty("Notify Relation Change", false, hintText: "When enabled, will send a notification on each relation change due to having patrols.")]
        [SettingPropertyGroup("Relation Settings")]
        public bool NotifyNotableRelations { get; set; } = false;


        [SettingProperty("Notable Relation Cap", -100, 100, false, hintText: "The relation cap for having patrols per notable. Relation gain will stop after reaching this value.")]
        [SettingPropertyGroup("Relation Settings")]
        public int RelationCap { get; set; } = 25;

        #endregion

        #region Misc Settings


        [SettingProperty("Force Troop Capacity", hintText: "When enabled, forces patrols to be capped at large patrol size. Will randomly remove troops over the large patrol size.")]
        [SettingPropertyGroup("Miscellaneous")]
        public bool ForceTroopCapEnabled { get; set; } = false;


        [SettingProperty("Patrol Regeneration", hintText: "When enabled, allows patrols to slowly regenerate on a daily basis up to large size.")]
        [SettingPropertyGroup("Miscellaneous")]
        public bool ForceRegenPatrol { get; set; } = false;


        [SettingProperty("Patrol Tether Range", 5, 50, false, hintText: "The maximum distance a patrol will go before being pulled back to their home settlement.")]
        [SettingPropertyGroup("Miscellaneous")]
        public int PatrolTetherRange { get; set; } = 15;

        #endregion

        #region Lord Patrols

        [SettingProperty("Other Lords Hire", false, hintText: "When enabled, other lords will try to hire patrols for their villages.")]
        [SettingPropertyGroup("Ai Hiring Settings")]
        public bool AiHirePatrols { get; set; } = true;


        [SettingProperty("Patrol Hiring Chance", 1, 100, false, hintText: "The daily chance that other lords will hire a patrol per village if it is below the hearth limit.")]
        [SettingPropertyGroup("Ai Hiring Settings")]
        public int AiGenerationChance { get; set; } = 5;


        [SettingProperty("Hearth Limit", 250, 2000, false, hintText: "If a village's hearth is below this number, the lord will run the chance to spawn a patrol for it.")]
        [SettingPropertyGroup("Ai Hiring Settings")]
        public int HearthMaximum { get; set; } = 500;

        [SettingProperty("Castle Prosperity Limit", 250, 10000, false, hintText: "If a castle's prosperity is below this number, the lord will run the chance to spawn a patrol for it.")]
        [SettingPropertyGroup("Ai Hiring Settings")]
        public int AICastleProsperity { get; set; } = 1000;

        [SettingProperty("Town Prosperity Limit", 250, 10000, false, hintText: "If a town's prosperity is below this number, the lord will run the chance to spawn a patrol for it.")]
        [SettingPropertyGroup("Ai Hiring Settings")]
        public int AITownProsperity { get; set; } = 2000;

        [SettingProperty("Patrol Bonus Speed", 0, 10, false, hintText: "The bonus speed that will be added to AI hired patrols.")]
        [SettingPropertyGroup("Ai Hiring Settings")]
        public float AddPatrolSpeedForAi { get; set; } = 0.5f;


        [SettingProperty("Max Patrols Per Settlement", 0, 10, false, hintText: "The maximum amount of patrols the AI will hire per settlement.")]
        [SettingPropertyGroup("Ai Hiring Settings")]
        public int AiMaxPatrolPerSettlement { get; set; } = 1;
        #endregion
    }

}
