using log4net.Config;
using SorterControl.Engine;
using SorterControl.Log4NetAppender;
using SorterControl.Management;
using SorterControl.Type;
using SorterControl.UI.Alarm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SorterControl
{
    public partial class Form1 : Form
    {
        RouteControl MainProcess = null;
        public Form1()
        {
            InitializeComponent();
            XmlConfigurator.Configure();
            AlarmFrom almFrm = new AlarmFrom();
            almFrm.Show();
            almFrm.Visible = false;
            ThreadPool.SetMinThreads(10, 10);

        }

        private void button1_Click(object sender, EventArgs e)
        {


        }

        private void button2_Click(object sender, EventArgs e)
        {
            button2.Enabled = false;
            button6.Enabled = true;
            MainProcess.ConnectAll();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            button2.Enabled = true;
            button6.Enabled = false;
            MainProcess.DisconnectAll();
        }

        private void button3_Click(object sender, EventArgs e)
        {




        }

        private void button4_Click(object sender, EventArgs e)
        {
            LockUI(true);
            if (!MainProcess._Mode.Equals("Stop") && !MainProcess._Mode.Equals(""))
            {
                AlarmUpdate.UpdateMessage("目前狀態為 " + MainProcess._Mode + " 模式，請先停止動作。");
                return;
            }

            NodeManagement.InitialNodes();
            JobManagement.Initial();

            if (PortSetting1Active_ck.Checked)
            {
                if (!PortSetting1Aligner_cb.Text.Equals(""))
                {
                    Node Aligner1 = NodeManagement.Get(PortSetting1Aligner_cb.Text);
                    if (Aligner1 != null)
                    {
                        Aligner1.LockByNode = PortSetting1Name_cb.Text;
                    }
                }
                Node P1 = NodeManagement.Get(PortSetting1Name_cb.Text);

                int SlotMode = 0;
                if (PortSetting1SlotMode_ck.Checked)
                {
                    SlotMode = 1;
                }
                else
                {
                    SlotMode = 2;
                }
                for (int i = Convert.ToInt16(PortSetting1StartSlot_tb.Text); i <= Convert.ToInt16(PortSetting1EndSlot_tb.Text); i = i + SlotMode)
                {
                    Job w = new Job();
                    w.Job_Id = "Wafer" + i.ToString("000");

                    w.AlignerFlag = true;
                    w.OCRFlag = false;
                    w.Position = P1.Name;
                    w.ProcessFlag = false;
                    w.FromPort = P1.Name;
                    w.Slot = i.ToString();
                    w.Destination = PortSetting1Dest_cb.Text;
                    w.DestinationSlot = i.ToString(); ;
                    JobManagement.Add(w.Job_Id, w);
                    P1.JobList.TryAdd(w.Slot, w);
                }
            }

            if (PortSetting2Active_ck.Checked)
            {
                if (!PortSetting2Aligner_cb.Text.Equals(""))
                {
                    Node Aligner2 = NodeManagement.Get(PortSetting2Aligner_cb.Text);
                    if (Aligner2 != null)
                    {
                        Aligner2.LockByNode = PortSetting2Name_cb.Text;
                    }
                }
                Node P2 = NodeManagement.Get(PortSetting2Name_cb.Text);

                int SlotMode = 0;
                if (PortSetting2SlotMode_ck.Checked)
                {
                    SlotMode = 1;
                }
                else
                {
                    SlotMode = 2;
                }
                if (Convert.ToInt16(PortSetting2StartSlot_tb.Text) != 0 && Convert.ToInt16(PortSetting2EndSlot_tb.Text) != 0)
                {
                    for (int i = Convert.ToInt16(PortSetting2StartSlot_tb.Text); i <= Convert.ToInt16(PortSetting2EndSlot_tb.Text); i = i + SlotMode)
                    {
                        Job w = new Job();
                        w.Job_Id = "Wafer" + (i + 25).ToString("000");

                        w.AlignerFlag = true;
                        w.OCRFlag = false;
                        w.Position = P2.Name;
                        w.ProcessFlag = false;
                        w.FromPort = P2.Name;
                        w.Slot = i.ToString();
                        w.Destination = PortSetting2Dest_cb.Text;
                        w.DestinationSlot = i.ToString(); ;
                        JobManagement.Add(w.Job_Id, w);
                        P2.JobList.TryAdd(w.Slot, w);
                    }
                }
            }
            NodeManagement.UpdatePortToUI();
            if (AutoIni_ck.Checked)
            {
                MainProcess.SetMode("AutoInitial");//initial
            }
            else
            {
                MainProcess.SetMode("Auto");
            }

        }


        private void PortSetting1Active_ck_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            MainProcess.Stop();
            LockUI(false);
        }

        private void LockUI(bool IsLock)
        {
            PortSetting1Active_ck.Enabled = !IsLock;
            PortSetting1SlotMode_ck.Enabled = !IsLock;
            PortSetting1Name_cb.Enabled = !IsLock;
            PortSetting1Aligner_cb.Enabled = !IsLock;
            PortSetting1StartSlot_tb.Enabled = !IsLock;
            PortSetting1EndSlot_tb.Enabled = !IsLock;
            PortSetting1Dest_cb.Enabled = !IsLock;

            PortSetting2Active_ck.Enabled = !IsLock;
            PortSetting2SlotMode_ck.Enabled = !IsLock;
            PortSetting2Name_cb.Enabled = !IsLock;
            PortSetting2Aligner_cb.Enabled = !IsLock;
            PortSetting2StartSlot_tb.Enabled = !IsLock;
            PortSetting2EndSlot_tb.Enabled = !IsLock;
            PortSetting2Dest_cb.Enabled = !IsLock;

            Reverse_ck.Enabled = !IsLock;
            AutoIni_ck.Enabled = !IsLock;
            button3.Enabled = !IsLock;
            button4.Enabled = !IsLock;
            LoadPort01Excute_bt.Enabled = !IsLock;
            LoadPort02Excute_bt.Enabled = !IsLock;
            LoadPort03Excute_bt.Enabled = !IsLock;
            LoadPort04Excute_bt.Enabled = !IsLock;
            LoadPort05Excute_bt.Enabled = !IsLock;
            LoadPort06Excute_bt.Enabled = !IsLock;
            LoadPort07Excute_bt.Enabled = !IsLock;
            LoadPort08Excute_bt.Enabled = !IsLock;
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            if (!MainProcess._Mode.Equals("Stop") && !MainProcess._Mode.Equals(""))
            {
                AlarmUpdate.UpdateMessage("目前狀態為 " + MainProcess._Mode + " 模式，請先停止動作。");
                return;
            }
            MainProcess.SetMode("Initial");
            LockUI(true);
        }

        private void button5_Click_2(object sender, EventArgs e)
        {
            Message_rt.Clear();
        }

        private void Conn_gv_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.ColumnIndex == 1)
            {
                switch (e.Value)
                {
                    case "Connected":
                        e.CellStyle.BackColor = Color.Green;
                        e.CellStyle.ForeColor = Color.White;
                        break;
                    case "Connecting":
                        e.CellStyle.BackColor = Color.Yellow;
                        e.CellStyle.ForeColor = Color.Black;
                        break;
                    default:
                        e.CellStyle.BackColor = Color.Red;
                        e.CellStyle.ForeColor = Color.White;
                        break;

                }
            }
        }



        private void AutoIni_ck_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void Reverse_ck_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox ck = (CheckBox)sender;
            MainProcess.AutoReverse = ck.Checked;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            MainProcess = new RouteControl();
        }

        private void ShowInfo_ck_CheckedChanged(object sender, EventArgs e)
        {
            if (((CheckBox)sender).Checked)
            {
                TextBoxAppender.ShowInfo = true;
            }
            else
            {
                TextBoxAppender.ShowInfo = false;
            }
        }

        private void ShowDebug_ck_CheckedChanged(object sender, EventArgs e)
        {

            if (((CheckBox)sender).Checked)
            {
                TextBoxAppender.ShowDebug = true;
            }
            else
            {
                TextBoxAppender.ShowDebug = false;
            }
        }

        private void label12_Click(object sender, EventArgs e)
        {

        }

        private void P4Cmd_cb_SelectedIndexChanged(object sender, EventArgs e)
        {

        }



        private void P4Value_tb_TextChanged(object sender, EventArgs e)
        {

        }

        private void ConvertMethod(string m, ref Transaction txn)
        {


        }


        private void Excute_bt_Click(object sender, EventArgs e)
        {
            ((Button)sender).Enabled = false;
            string PortName = ((Button)sender).Name.Replace("Excute_bt", "");


            ComboBox cmd = this.Controls.Find(PortName + "Cmd_cb", true).FirstOrDefault() as ComboBox;

            if (cmd.Text.Equals(""))
            {
                return;
            }
            Transaction txn = new Transaction(new List<Job>(), "", "", "", "", 30000);
            switch (cmd.Text)
            {
                case "Load":
                    txn.Method = Transaction.Command.LoadPortType.Load;
                    break;
                case "Mapping Load":
                    txn.Method = Transaction.Command.LoadPortType.MappingLoad;
                    break;
                case "Mapping":
                    txn.Method = Transaction.Command.LoadPortType.Mapping;
                    break;
                case "Unload":
                    txn.Method = Transaction.Command.LoadPortType.Unload;
                    break;
                case "Mapping Unload":
                    txn.Method = Transaction.Command.LoadPortType.MappingUnload;
                    break;
                case "GetMapping":
                    txn.Method = Transaction.Command.LoadPortType.GetMapping;
                    break;
                case "GetLED":
                    txn.Method = Transaction.Command.LoadPortType.GetLED;
                    break;
                case "GetStatus":
                    txn.Method = Transaction.Command.LoadPortType.GetStatus;
                    break;
                case "Reset":
                    txn.Method = Transaction.Command.LoadPortType.Reset;
                    break;
                case "Initial Pos":
                    txn.Method = Transaction.Command.LoadPortType.InitialPos;
                    break;
                case "GetCount":
                    txn.Method = Transaction.Command.LoadPortType.GetCount;
                    break;
            }

            NodeManagement.Get(PortName).SendCommand(txn);
        }



    }
}
