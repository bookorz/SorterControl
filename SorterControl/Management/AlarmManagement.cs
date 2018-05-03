using SorterControl.UI.Alarm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SorterControl.Management
{
    class AlarmManagement
    {
        private static List<AlarmInfo> AlarmList = new List<AlarmInfo>();
        private static List<AlarmInfo> AlarmHistory = new List<AlarmInfo>();

        public static void Add(AlarmInfo Alm)
        {
            AlarmList.Add(Alm);
            AlarmHistory.Add(Alm);
            AlarmUpdate.UpdateStatusSignal(Alm.NodeName, "Red");
            AlarmUpdate.UpdateMessage(Alm.NodeName + " Alarm Happen " + Alm.AlarmCode + ":" + Alm.Desc);
            AlarmUpdate.UpdateAlarmList(AlarmList.ToList());
            AlarmUpdate.UpdateAlarmHistory(AlarmHistory.ToList());
        }

        public static void Clear()
        {
            AlarmList.Clear();
        }

        public static List<AlarmInfo> GetAll()
        {
            List<AlarmInfo> result = null;
            result = AlarmList.ToList();
            return result;
        }

        public static void Remove(string NodeName)
        {
            var find = from Alm in AlarmList.ToList()
                       where Alm.NodeName.Equals(NodeName)
                       select Alm;

            if (find.Count() != 0)
            {
                foreach(AlarmInfo each in find)
                {
                    AlarmList.Remove(each);
                }
            }
            AlarmUpdate.UpdateAlarmList(GetAll());
            if (NodeManagement.Get(NodeName).InitialComplete)
            {
                AlarmUpdate.UpdateStatusSignal(NodeName, "Green");
            }
        }
    }
}
