using log4net;
using SorterControl.Management;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SorterControl.UI.PortSetting
{
    class PortsettingUpdate
    {
        static ILog logger = LogManager.GetLogger(typeof(PortsettingUpdate));
        delegate void Update();
        delegate string Get(string name);
        delegate bool GetBool(string name);

        public static string GetCBText(string name)
        {
            string result = "";
            Form form = Application.OpenForms["Form1"];
            if (form == null)
                return "";
            ComboBox cb = form.Controls.Find(name, true).FirstOrDefault() as ComboBox;
            if (cb == null)
                return "";
            if (cb.InvokeRequired)
            {
                Get ph = new Get(GetCBText);
                result = (string)cb.Invoke(ph, name);
            }
            else
            {
                result = cb.Text;
            }
            
            return result;
        }

        public static bool GetActive(string name)
        {
            bool result = false;
            Form form = Application.OpenForms["Form1"];
            if (form == null)
                return false;
            CheckBox cb = form.Controls.Find(name, true).FirstOrDefault() as CheckBox;
            if (cb == null)
                return false;
            if (cb.InvokeRequired)
            {
                GetBool ph = new GetBool(GetActive);
                result = (bool)cb.Invoke(ph, name);
            }
            else
            {
                result = cb.Checked;
            }

            return result;
        }

        public static void ReverseSetting()
        {
            try
            {
                Form form = Application.OpenForms["Form1"];
                ComboBox PortSetting1Name;
                ComboBox PortSetting1Dest;
                ComboBox PortSetting2Name;
                ComboBox PortSetting2Dest;
                if (form == null)
                    return;


                PortSetting1Name = form.Controls.Find("PortSetting1Name_cb", true).FirstOrDefault() as ComboBox;
                PortSetting1Dest = form.Controls.Find("PortSetting1Dest_cb", true).FirstOrDefault() as ComboBox;
                PortSetting2Name = form.Controls.Find("PortSetting2Name_cb", true).FirstOrDefault() as ComboBox;
                PortSetting2Dest = form.Controls.Find("PortSetting2Dest_cb", true).FirstOrDefault() as ComboBox;
                if (PortSetting1Name == null || PortSetting1Dest == null|| PortSetting2Name == null || PortSetting2Dest == null)
                    return;

                if (PortSetting1Name.InvokeRequired)
                {
                    Update ph = new Update(ReverseSetting);
                    PortSetting1Name.Invoke(ph);
                }
                else
                {
                    string swap = PortSetting1Dest.Text;
                    PortSetting1Dest.Text = PortSetting1Name.Text;
                    PortSetting1Name.Text = swap;

                    swap = PortSetting2Dest.Text;
                    PortSetting2Dest.Text = PortSetting2Name.Text;
                    PortSetting2Name.Text = swap;
                }


            }
            catch (Exception e)
            {
                logger.Error("ReverseSetting1: Update fail." + e.Message + "\n" + e.StackTrace);
            }

        }

    }
}
