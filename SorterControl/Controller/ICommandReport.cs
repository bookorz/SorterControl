using SANWA.Utility;
using SorterControl.Management;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SorterControl.Controller
{
    public interface ICommandReport
    {
        void On_Command_Excuted(Node Node, Transaction Txn, ReturnMessage Msg);
        void On_Command_Error(Node Node, Transaction Txn, ReturnMessage Msg);
        void On_Command_Finished(Node Node, Transaction Txn, ReturnMessage Msg);
        void On_Command_TimeOut(Node Node, Transaction Txn);
        void On_Event_Trigger(Node Node, ReturnMessage Msg);
        void On_Node_State_Changed(Node Node, string Status);
        void On_Controller_State_Changed(string Device_ID, string Status);
    }
}
