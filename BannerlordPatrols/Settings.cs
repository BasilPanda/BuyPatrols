using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;
using TaleWorlds.Library;
using System.Windows.Forms;
using ModLib;
using ModLib.Attributes;
using System.Xml.Serialization;

namespace BuyPatrols
{
    public class Settings : SettingsBase
    {
        public const string InstanceID = "BuyPatrolsSettings";
        public override string ModName => "Buy Patrols";
        public override string ModuleFolderName => "zzzBuyPatrols";
        
        [XmlElement]
        public override string ID { get; set; } = InstanceID;

        public static Settings Instance
        {
            get
            {
                return (Settings)SettingsDatabase.GetSettings(InstanceID);
            }
        }

        #region Main Settings

        [XmlElement]
        [SettingProperty("Target Caravans", "When enabled, patrols will target caravans.")]
        [SettingPropertyGroup("Patrol Settings")]
        public bool TargetCaravans { get; set; } = false;

        [XmlElement]
        [SettingProperty("Target Villagers", "When enabled, patrols will target villagers.")]
        [SettingPropertyGroup("Patrol Settings")]
        public bool TargetVillagers { get; set; } = false;
        
        [XmlElement]
        [SettingProperty("Enable Bonus Speed to Patrols", "When enabled, patrols will gain bonus speed from the add patrol speed option.")]
        [SettingPropertyGroup("Patrol Settings")]
        public bool AddPatrolSpeedEnabled { get; set; } = true;

        [XmlElement]
        [SettingProperty("Patrol Wages Hint Box", "When enabled, daily patrols wages will be included the clan expenses. When it is disabled, patrol wages will be notified on the bottom left.")]
        [SettingPropertyGroup("Patrol Settings")]
        public bool PatrolWagesHintBox { get; set; } = true;

        [XmlElement]
        [SettingProperty("Personal Patrol Bonus Speed", 0, 10, "The bonus speed that will be added to patrols within your clan.")]
        [SettingPropertyGroup("Patrol Settings")]
        public float AddPatrolSpeed { get; set; } = 3.5f;

        [XmlElement]
        [SettingProperty("Base Cost to Patrols", 0, 50000, "The base cost to patrols. Base Cost + Hearth * 3 for small. Base Cost * 2 + Hearth * 6 for medium. Base Cost * 3 + Hearth * 9 for large.")]
        [SettingPropertyGroup("Patrol Settings")]
        public int BaseCost { get; set; } = 1250;

        [XmlElement]
        [SettingProperty("Patrol Wage Modifier", 0, 4, "Modifies the daily wage amount. 0 makes it so there are no patrol wages to pay.")]
        [SettingPropertyGroup("Patrol Settings")]
        public float DailyPatrolWageModifier { get; set; } = 0.75f;

        [XmlElement]
        [SettingProperty("Patrols per Village", 1, 10, "Modifies the max amount of patrols per village.")]
        [SettingPropertyGroup("Patrol Settings")]
        public int MaxPatrolCountPerVillage { get; set; } = 3;

        [XmlElement]
        [SettingProperty("Base Patrol Size", 4, 64, "Modifies the base average men per patrol. This affects all sizes. Medium is 2x and large is 3x this number")]
        [SettingPropertyGroup("Patrol Settings")]
        public int TroopsPerPatrol { get; set; } = 16;
        
        #endregion

        #region Relation Settings

        [XmlElement]
        [SettingProperty("Allow Relation Gain", "When enabled, a daily 10% chance to gain relation with notables that have patrols.")]
        [SettingPropertyGroup("Relation Settings")]
        public bool IncreaseNotableRelations { get; set; } = true;

        [XmlElement]
        [SettingProperty("Notify Relation Change", "When enabled, will send a notification on each relation change due to having patrols.")]
        [SettingPropertyGroup("Relation Settings")]
        public bool NotifyNotableRelations { get; set; } = false;

        [XmlElement]
        [SettingProperty("Notable Relation Cap", -100, 100, "The relation cap for having patrols per notable. Relation gain will stop after reaching this value.")]
        [SettingPropertyGroup("Relation Settings")]
        public int RelationCap { get; set; } = 25;

        #endregion

        #region Misc Settings
        
        [XmlElement]
        [SettingProperty("Force Troop Capacity", "When enabled, forces patrols to be capped at large patrol size. Will randomly remove troops over the large patrol size.")]
        [SettingPropertyGroup("Miscellaneous")]
        public bool ForceTroopCapEnabled { get; set; } = false;

        [XmlElement]
        [SettingProperty("Patrol Regeneration", "When enabled, allows patrols to slowly regenerate on a daily basis up to large size.")]
        [SettingPropertyGroup("Miscellaneous")]
        public bool ForceRegenPatrol { get; set; } = false;

        [XmlElement]
        [SettingProperty("Patrol Tether Range", 5, 50, "The maxiumum distance a patrol will go before being pulled back to their home settlement.")]
        [SettingPropertyGroup("Miscellaneous")]
        public int PatrolTetherRange { get; set; } = 15;

        #endregion

        #region Lord Patrols
        [XmlElement]
        [SettingProperty("Other Lords Hire", "When enabled, other lords will try to hire patrols for their villages.")]
        [SettingPropertyGroup("Ai Hiring Settings")]
        public bool AiHirePatrols { get; set; } = true;

        [XmlElement]
        [SettingProperty("AU Patrol Hiring Chance", 1, 100, "The daily chance that another lord will hire a patrol per village.")]
        [SettingPropertyGroup("Ai Hiring Settings")]
        public int AiGenerationChance { get; set; } = 5;

        [XmlElement]
        [SettingProperty("AI Patrol Bonus Speed", 0, 10, "The bonus speed that will be added to AI hired patrols.")]
        [SettingPropertyGroup("Ai Hiring Settings")]
        public float AddPatrolSpeedForAi { get; set; } = 0.5f;
        #endregion
    }

    /*
    public static class Settings
    {

        public static string LoadSetting(string name)
        {
            XmlDocument settings = new XmlDocument();
            try
            {
                settings.Load(BasePath.Name + "Modules/zzzBuyPatrols/ModuleData/config.xml");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            XmlNode root = settings.FirstChild;
            return root.SelectSingleNode(name).InnerText;
        }
    }
    */
}
