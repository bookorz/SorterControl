using log4net;
using SorterControl.Management;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SorterControl.UI.JobState
{
    class JobStateUpdate
    {
        static ILog logger = LogManager.GetLogger(typeof(JobStateUpdate));
        delegate void UpdateJobs(List<Job> JobList);
        delegate void UpdateNode(List<Node> JobList);
        delegate void UpdatePortJob(string name ,List<Job> JobList);

        public class SlotInfo {
            public string Slot { get; set; }
            public string ID { get; set; }
        }

        public static void UpdatePort(string name, List<Job> JobList)
        {
            Form form = Application.OpenForms["Form1"];
            DataGridView JobList_gv;
            if (form == null)
                return;
            

            JobList_gv = form.Controls.Find(name, true).FirstOrDefault() as DataGridView;
            if (JobList_gv == null)
                return;

            if (JobList_gv.InvokeRequired)
            {
                UpdatePortJob ph = new UpdatePortJob(UpdatePort);
                JobList_gv.BeginInvoke(ph, name,JobList);
            }
            else
            {
                
                List<SlotInfo> tmp = new List<SlotInfo>();
                if (JobList.Count != 0)
                {
                    JobList.Sort((x, y) => { return -Convert.ToInt16(x.Slot).CompareTo(Convert.ToInt16(y.Slot)); });
                }
                    
                //JobList_gv.DataSource = null;
                JobList_gv.DataSource = JobList;
                JobList_gv.Columns["ProcessFlag"].Visible = false;
                JobList_gv.Columns["Piority"].Visible = false;
                JobList_gv.Columns["OCRFlag"].Visible = false;
                JobList_gv.Columns["Position"].Visible = false;
                //JobList_gv.Columns["Slot"].Visible = false;
                JobList_gv.Columns["FromPort"].Visible = false;
                JobList_gv.Columns["Destination"].Visible = false;
                JobList_gv.Columns["DestinationSlot"].Visible = false;
                JobList_gv.Columns["LastNode"].Visible = false;
                JobList_gv.Columns["CurrentState"].Visible = false;
                JobList_gv.Columns["AlignerFlag"].Visible = false;

                //Conn_gv.Refresh();
               // JobList_gv.ClearSelection();
            }
        }

        public static void UpdateJobStatus(List<Job> JobList)
        {
            try
            {
                Form form = Application.OpenForms["Form1"];
                DataGridView JobList_gv;
                if (form == null)
                    return;
                

                JobList_gv = form.Controls.Find("JobList_gv", true).FirstOrDefault() as DataGridView;
                if (JobList_gv == null)
                    return;

                if (JobList_gv.InvokeRequired)
                {
                    UpdateJobs ph = new UpdateJobs(UpdateJobStatus);
                    JobList_gv.BeginInvoke(ph, JobList);
                }
                else
                {
                    //TabControl tab = form.Controls.Find("tabControl1", true).FirstOrDefault() as TabControl;
                    //if (tab.SelectedIndex != 1)
                    //{
                    //    return;
                    //}
                    //JobList_gv.DataSource = null;
                    JobList_gv.DataSource = JobList;
                    //Conn_gv.Refresh();
                    JobList_gv.ClearSelection();
                }


            }
            catch (Exception e)
            {
                logger.Error("UpdateJobStatus: Update fail." + e.Message + "\n" + e.StackTrace);
            }

        }

        public static void UpdateNodeStatus(List<Node> NodeList)
        {
            try
            {
                Form form = Application.OpenForms["Form1"];
                DataGridView Node_gv;
                if (form == null)
                    return;
                
                Node_gv = form.Controls.Find("Node_gv", true).FirstOrDefault() as DataGridView;
                if (Node_gv == null)
                    return;

                if (Node_gv.InvokeRequired)
                {
                    UpdateNode ph = new UpdateNode(UpdateNodeStatus);
                    Node_gv.BeginInvoke(ph, NodeList);
                }
                else
                {
                    //TabControl tab = form.Controls.Find("tabControl1", true).FirstOrDefault() as TabControl;
                    //if (tab.SelectedIndex != 2)
                    //{
                    //    return;
                    //}


                    Node_gv.DataSource = null;
                    Node_gv.DataSource = NodeList;
                    //Conn_gv.Refresh();
                    Node_gv.ClearSelection();
                }


            }
            catch (Exception e)
            {
                logger.Error("UpdateNodeStatus: Update fail." + e.Message + "\n" + e.StackTrace);
            }

        }
    }
}
