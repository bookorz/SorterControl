using log4net;
using SorterControl.Management;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SorterControl.UI.ConnectState
{
    class ConnectStateUpdate
    {
        static ILog logger = LogManager.GetLogger(typeof(ConnectStateUpdate));
        delegate void UpdateController(string Device_ID, string Detail);

        static List<ConnectState> ConnList = new List<ConnectState>();
        class ConnectState
        {
            public string Device_Id { get; set; }
            public string State { get; set; }
        }

        public static void AddConnection(string Device_Id)
        {


            lock (ConnList)
            {

                ConnectState eachState = new ConnectState();
                eachState.Device_Id = Device_Id;
                eachState.State = "Disconnected";
                ConnList.Add(eachState);

            }

        }

        public static void UpdateControllerStatus(string Device_ID, string State)
        {
            try
            {
                Form form = Application.OpenForms["Form1"];
                DataGridView Conn_gv;
                if (form == null)
                    return;

                Conn_gv = form.Controls.Find("Conn_gv", true).FirstOrDefault() as DataGridView;
                if (Conn_gv == null)
                    return;

                if (Conn_gv.InvokeRequired)
                {
                    UpdateController ph = new UpdateController(UpdateControllerStatus);
                    Conn_gv.BeginInvoke(ph, Device_ID, State);
                }
                else
                {
                    foreach (ConnectState each in ConnList)
                    {
                        if (each.Device_Id.Equals(Device_ID))
                        {
                            each.State = State;
                        }
                    }


                    Conn_gv.DataSource = null;
                    Conn_gv.DataSource = ConnList;
                    //Conn_gv.Refresh();
                    Conn_gv.ClearSelection();
                }


            }
            catch
            {
                logger.Error("UpdateControllerStatus: Update fail.");
            }
        }
    }
}
