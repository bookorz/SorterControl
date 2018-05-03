using SorterControl.Comm;
using SorterControl.Config;
using SorterControl.Management;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SorterControl.Type;
using log4net;
using System.Collections.Concurrent;
using System.Threading;
using Newtonsoft.Json;
using SANWA.Utility;

namespace SorterControl.Controller
{
    public class DeviceController : IController, IConnectionReport, ITransactionReport
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(DeviceController));
        ICommandReport _ReportTarget;
        IConnection conn;
        DeviceConfig _Config;
    SANWA.Utility.Decoder _Decoder;
        ConcurrentDictionary<string, Transaction> TransactionList = new ConcurrentDictionary<string, Transaction>();

        public DeviceController(DeviceConfig Config, ICommandReport ReportTarget)
        {
            _ReportTarget = ReportTarget;
            _Config = Config;

            switch (Config.ConnectionType)
            {
                case "Socket":
                    conn = new SocketClient(Config.IPAdress, Config.Port, this);
                    break;
                case "ComPort":
                    conn = new ComPortClient(this);
                    break;
            }
            _Decoder = new SANWA.Utility.Decoder(Config.DeviceType);

        }
        public void Close()
        {
            try
            {
                conn.Close();
            }
            catch (Exception e)
            {

                logger.Error(_Config.DeviceName + "(DisconnectServer " + _Config.IPAdress + ":" + _Config.Port.ToString() + ")" + e.Message + "\n" + e.StackTrace);

            }
        }

        public void Connect()
        {
            try
            {
                conn.Connect();
            }
            catch (Exception e)
            {
                logger.Error(_Config.DeviceName + "(ConnectToServer " + _Config.IPAdress + ":" + _Config.Port.ToString() + ")" + e.Message + "\n" + e.StackTrace);
            }
        }

        public bool DoWork(Transaction Txn)
        {

            bool result = false;
            // lock (TransactionList)




            if (TransactionList.TryAdd(Txn.AdrNo + Txn.IsInterrupt.ToString(), Txn))
            {
                Txn.SetTimeOutReport(this);
                if (Txn.Method.Equals(Transaction.Command.RobotType.Reset))
                {
                    Txn.SetTimeOut(30000);
                }
                else
                {
                    Txn.SetTimeOut(1000);
                }
                Txn.SetTimeOutMonitor(true);
                ThreadPool.QueueUserWorkItem(new WaitCallback(conn.Send), Txn.CommandEncodeStr);
               // conn.Send(Txn.CommandEncodeStr);
                logger.Debug(_Config.DeviceName + "Send:" + Txn.CommandEncodeStr.Replace("\r", ""));
                result = true;

            }
            else
            {

                logger.Debug(_Config.DeviceName + "(DoWork " + _Config.IPAdress + ":" + _Config.Port.ToString() + ":" + Txn.CommandEncodeStr + ") Same type command is already excuting.");

                result = false;
            }



            //}
            return result;
        }

        public void On_Connection_Message(object MsgObj)
        {
            try
            {
                List<ReturnMessage> ReturnMsgList;
                string Msg = MsgObj.ToString();
                
                logger.Debug(_Config.DeviceName + "Recieve:" + Msg.Replace("\r", ""));

                
                //lock (_Decoder)
                //{
                    ReturnMsgList = _Decoder.GetMessage(Msg);
                //}
                if (ReturnMsgList == null)
                {
                    logger.Debug(_Config.DeviceName + " Decode parse error:" + Msg.Replace("\r", ""));
          return;
                }
                if (ReturnMsgList.Count == 0)
                {
                    logger.Debug(_Config.DeviceName + " Decode parse error:" + Msg.Replace("\r", ""));
                }
                foreach (ReturnMessage ReturnMsg in ReturnMsgList)
                {
                    logger.Debug(_Config.DeviceName + "Each ReturnMsg:" + JsonConvert.SerializeObject(ReturnMsg));
                    try
                    {
                        Transaction Txn;
                        Node Node;
                        if (ReturnMsg != null)
                        {
                            Node = NodeManagement.GetByController(_Config.DeviceName, ReturnMsg.NodeAdr);

                            if(Node == null)
                            {
                                logger.Debug("Node not found!"+ _Config.DeviceName+" - "+ ReturnMsg.NodeAdr);
                return;
                            }
                            //lock (TransactionList)
                            //{
                            lock (Node)
                            {
                if (ReturnMsg.Type == ReturnMessage.ReturnType.Event)
                {
                  _ReportTarget.On_Event_Trigger(Node, ReturnMsg);
                }
                else if (TransactionList.TryRemove(ReturnMsg.NodeAdr + ReturnMsg.IsInterrupt.ToString(), out Txn))
                {
                  logger.Debug("Txn removed.");
                  switch (ReturnMsg.Type)
                  {
                    case ReturnMessage.ReturnType.Excuted:
                      if (!Txn.CommandType.Equals("CMD") && !Txn.CommandType.Equals("MOV"))
                      {
                        logger.Debug("Txn timmer stoped.");
                        Txn.SetTimeOutMonitor(false);
                      }
                      else
                      {
                        Txn.SetTimeOutMonitor(false);
                        Txn.SetTimeOut(15000);
                        Txn.SetTimeOutMonitor(true);
                        TransactionList.TryAdd(ReturnMsg.NodeAdr + ReturnMsg.IsInterrupt.ToString(), Txn);
                      }
                      _ReportTarget.On_Command_Excuted(Node, Txn, ReturnMsg);
                      break;
                    case ReturnMessage.ReturnType.Finished:
                      logger.Debug("Txn timmer stoped.");
                      Txn.SetTimeOutMonitor(false);
                      _ReportTarget.On_Command_Finished(Node, Txn, ReturnMsg);
                      break;
                    case ReturnMessage.ReturnType.Error:
                      Txn.SetTimeOutMonitor(false);
                      _ReportTarget.On_Command_Error(Node, Txn, ReturnMsg);
                      break;
                    case ReturnMessage.ReturnType.Information:
                      logger.Debug("Txn timmer stoped.");
                      Txn.SetTimeOutMonitor(false);

                      _ReportTarget.On_Command_Finished(Node, Txn, ReturnMsg);
                      //SpinWait.SpinUntil(() => false, 300);
                      ThreadPool.QueueUserWorkItem(new WaitCallback(conn.Send), ReturnMsg.FinCommand);
                      logger.Debug(_Config.DeviceName + "Send:" + ReturnMsg.FinCommand);
                      break;
                    
                  }
                }
                else
                {
                  logger.Debug(_Config.DeviceName + "(On_Connection_Message Txn is not found. msg:" + Msg);
                  switch (ReturnMsg.Type)
                  {
                    case ReturnMessage.ReturnType.Information:
                    case ReturnMessage.ReturnType.ReInformation:
                      ThreadPool.QueueUserWorkItem(new WaitCallback(conn.Send), ReturnMsg.FinCommand);
                      logger.Debug(_Config.DeviceName + "Send:" + ReturnMsg.FinCommand);
                      break;
                  }
                }
                            }
                            //}
                        }
                        else
                        {
                            logger.Debug(_Config.DeviceName + "(On_Connection_Message Message decode fail:" + Msg);
                        }
                    }
                    catch (Exception e)
                    {
                        logger.Error(_Config.DeviceName + "(On_Connection_Message " + _Config.IPAdress + ":" + _Config.Port.ToString() + ")" + e.Message + "\n" + e.StackTrace);
                    }
                }
            }
            catch (Exception e)
            {
                logger.Error(_Config.DeviceName + "(On_Connection_Message " + _Config.IPAdress + ":" + _Config.Port.ToString() + ")" + e.Message + "\n" + e.StackTrace);
            }
        }

        public void On_Connection_Connected(string Msg)
        {
            _ReportTarget.On_Controller_State_Changed(_Config.DeviceName, "Connected");
        }

        public void On_Connection_Connecting(string Msg)
        {
            _ReportTarget.On_Controller_State_Changed(_Config.DeviceName, "Connecting");
        }

        public void On_Connection_Disconnected(string Msg)
        {
            _ReportTarget.On_Controller_State_Changed(_Config.DeviceName, "Disconnected");           
        }

        public void On_Connection_Error(string Msg)
        {
            foreach (Transaction txn in TransactionList.Values.ToList())
            {
                txn.SetTimeOutMonitor(false);
            }
            TransactionList.Clear();
            _ReportTarget.On_Controller_State_Changed(_Config.DeviceName, "Connection_Error");
        }

        public void On_Transaction_TimeOut(Transaction Txn)
        {
            logger.Debug(_Config.DeviceName + "(On_Transaction_TimeOut Txn is timeout:" + Txn.CommandEncodeStr);
            Txn.SetTimeOutMonitor(false);
            if (TransactionList.TryRemove(Txn.AdrNo + Txn.IsInterrupt.ToString(), out Txn))
            {
                Node Node = NodeManagement.GetByController(_Config.DeviceName, Txn.AdrNo);
                if (Node != null)
                {
                    _ReportTarget.On_Command_TimeOut(Node, Txn);
                }
                else
                {
                    logger.Debug(_Config.DeviceName + "(On_Transaction_TimeOut Get Node fail.");
                }
            }
            else
            {
                logger.Debug(_Config.DeviceName + "(On_Transaction_TimeOut TryRemove Txn fail.");
            }
        }


    }
}
