using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;
using TaleWorlds.Library;
using System.Windows.Forms;

namespace BuyPatrols
{
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
}
