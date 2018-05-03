using log4net;
using SorterControl.Management;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SorterControl.UI.Alarm
{
    class AlarmUpdate
    {
        static ILog logger = LogManager.GetLogger(typeof(AlarmUpdate));
        delegate void UpdateAlarm(List<AlarmInfo> AlarmList);
        delegate void UpdateSignal(string Name, string Signal);
        delegate void UpdateMsg(string Msg);

        public static void UpdateMessage(string Msg)
        {
            try
            {
                Form form = Application.OpenForms["Form1"];
                RichTextBox rt;
                if (form == null)
                    return;


                rt = form.Controls.Find("Message_rt", true).FirstOrDefault() as RichTextBox;
                if (rt == null)
                    return;

                if (rt.InvokeRequired)
                {
                    UpdateMsg ph = new UpdateMsg(UpdateMessage);
                    rt.BeginInvoke(ph, Msg);
                }
                else
                {
                    rt.AppendText(Msg+"\n`");

                }


            }
            catch (Exception e)
            {
                logger.Error("UpdateStatusSignal: Update fail." + e.Message + "\n" + e.StackTrace);
            }
        }

        public static void UpdateStatusSignal(string Name,string Signal)
        {
            try
            {
                Form form = Application.OpenForms["Form1"];
                Label SignalS;
                if (form == null)
                    return;


                SignalS = form.Controls.Find(Name+"S", true).FirstOrDefault() as Label;
                if (SignalS == null)
                    return;

                if (SignalS.InvokeRequired)
                {
                    UpdateSignal ph = new UpdateSignal(UpdateStatusSignal);
                    SignalS.BeginInvoke(ph, Name, Signal);
                }
                else
                {
                    switch (Signal)
                    {
                        case "Red":
                            SignalS.ForeColor = Color.Red;
                            break;
                        case "Green":
                            SignalS.ForeColor = Color.Green;
                            break;
                    }
                    
                }


            }
            catch (Exception e)
            {
                logger.Error("UpdateStatusSignal: Update fail." + e.Message + "\n" + e.StackTrace);
            }
        }

        public static void UpdateAlarmList(List<AlarmInfo> AlarmList)
        {
            try
            {

                Form form = Application.OpenForms["AlarmFrom"];
                DataGridView AlarmList_gv;
                
                   
                
                if (form == null)
                    return;


                AlarmList_gv = form.Controls.Find("AlarmList_gv", true).FirstOrDefault() as DataGridView;
                if (AlarmList_gv == null)
                    return;

                if (AlarmList_gv.InvokeRequired)
                {
                    UpdateAlarm ph = new UpdateAlarm(UpdateAlarmList);
                   
                    AlarmList_gv.BeginInvoke(ph, AlarmList);
                   
                }
                else
                {

                    //JobList_gv.DataSource = null;
                    AlarmList_gv.DataSource = AlarmList;
                    
                    //Conn_gv.Refresh();
                    AlarmList_gv.ClearSelection();
                    AlarmList_gv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
                    if (AlarmList.Count != 0)
                    {
                        form.Visible = true;
                    }
                    else
                    {
                        form.Visible = false;
                    }
                }


            }
            catch (Exception e)
            {
                logger.Error("UpdateAlarmList: Update fail." + e.Message + "\n" + e.StackTrace);
            }

        }

        public static void UpdateAlarmHistory(List<AlarmInfo> AlarmList)
        { 
            try
            {
                Form form = Application.OpenForms["Form1"];
                DataGridView AlarmList_gv;
                
                if (form == null)
                    return;


                AlarmList_gv = form.Controls.Find("AlarmHistory_gv", true).FirstOrDefault() as DataGridView;
                if (AlarmList_gv == null)
                    return;

                if (AlarmList_gv.InvokeRequired)
                {
                    UpdateAlarm ph = new UpdateAlarm(UpdateAlarmHistory);
                    AlarmList_gv.BeginInvoke(ph, AlarmList);
                }
                else
                {

                    //JobList_gv.DataSource = null;
                    AlarmList_gv.DataSource = AlarmList;

                    //Conn_gv.Refresh();
                    AlarmList_gv.ClearSelection();
                    AlarmList_gv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
                }


            }
            catch (Exception e)
            {
                logger.Error("UpdateAlarmHistory: Update fail." + e.Message + "\n" + e.StackTrace);
            }

        }
    }
}
