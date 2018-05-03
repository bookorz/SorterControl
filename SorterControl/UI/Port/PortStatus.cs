using log4net;
using SorterControl.Management;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SorterControl.UI.Port
{
  class PortStatus
  {
    static ILog logger = LogManager.GetLogger(typeof(PortStatus));
    delegate void UpdateStatus(string PortName,string Value);
    delegate void UpdatePortJob(string name, string MappingList);

    public class SlotInfo
    {
      public string Slot { get; set; }
      public string ID { get; set; }
    }

    public static void UpdatePort(string name, string MappingList)
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
        JobList_gv.BeginInvoke(ph, name, MappingList);
      }
      else
      {

        List<SlotInfo> tmp = new List<SlotInfo>();
        
        for(int i = MappingList.Length-1; i >= 0; i--)
        {
          SlotInfo t = new SlotInfo();
          t.Slot = (i + 1).ToString();
          switch (MappingList[i])
          {
            case '0':
              t.ID = "No wafer";
              break;
            case '1':
              t.ID = "Wafer";
              break;
            case '2':
              t.ID = "Crossed";
              break;
            case '?':
              t.ID = "Undefined";
              break;
            case 'W':
              t.ID = "Overlapping";
              break;
          }
          tmp.Add(t);
        }

        //JobList_gv.DataSource = null;
        JobList_gv.DataSource = tmp;
       

        //Conn_gv.Refresh();
        // JobList_gv.ClearSelection();
      }
    }

    public static void UpdatePortStatus(string PortName, string Value)
    {
      try
      {
        Form form = Application.OpenForms["Form1"];
        TextBox Status_tb;
        if (form == null)
          return;


        Status_tb = form.Controls.Find(PortName+"Value_tb", true).FirstOrDefault() as TextBox;
        if (Status_tb == null)
          return;

        if (Status_tb.InvokeRequired)
        {
          UpdateStatus ph = new UpdateStatus(UpdatePortStatus);
          Status_tb.BeginInvoke(ph, PortName, Value);
        }
        else
        {
          Status_tb.Text = Value;
        }


      }
      catch (Exception e)
      {
        logger.Error("UpdatePortStatus: Update fail." + e.Message + "\n" + e.StackTrace);
      }

    }
  }
}
