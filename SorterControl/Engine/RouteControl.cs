using log4net;
using Newtonsoft.Json;
using SANWA.Utility;
using SorterControl.Config;
using SorterControl.Controller;
using SorterControl.Management;
using SorterControl.Type;
using SorterControl.UI.Alarm;
using SorterControl.UI.ConnectState;
using SorterControl.UI.JobState;
using SorterControl.UI.Port;
using SorterControl.UI.PortSetting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SorterControl.Engine
{
  class RouteControl : AlarmMapping, ICommandReport
  {
    private static readonly ILog logger = LogManager.GetLogger(typeof(RouteControl));
    public string _Mode = "";
    DateTime StartTime = new DateTime();
    public bool AutoReverse = false;

    public RouteControl()
    {
      ConfigTool<DeviceConfig> DeviceCfg = new ConfigTool<DeviceConfig>();
      foreach (DeviceConfig eachDevice in DeviceCfg.ReadFileByList("config/Controller/Controllers.json"))
      {
        ConnectStateUpdate.AddConnection(eachDevice.DeviceName);
        DeviceController ctrl = new DeviceController(eachDevice, this);
        //ctrl.Connect();
        ControllerManagement.Add(eachDevice.DeviceName, ctrl);
      }
      ConfigTool<Node> NodeCfg = new ConfigTool<Node>();

      foreach (Node eachNode in NodeCfg.ReadFileByList("config/Node/Nodes.json"))
      {

        NodeManagement.Add(eachNode.Name, eachNode);

      }
      this.AutoReverse = PortsettingUpdate.GetActive("Reverse_ck");
    }

    public void ConnectAll()
    {
      ControllerManagement.ConnectAll();
    }

    public void DisconnectAll()
    {
      ControllerManagement.DisconnectAll();
    }

    public void Stop()
    {
      lock (this)
      {
        _Mode = "Stop";
      }



    }

    public void SetMode(string Mode)
    {

      lock (this)
      {
        _Mode = Mode;
      }
      switch (Mode)
      {
        case "Auto":
          StartTime = DateTime.Now;
          if (!NodeManagement.Get("Robot01").InitialComplete || !NodeManagement.Get("Robot02").InitialComplete || !NodeManagement.Get("Aligner01").InitialComplete || !NodeManagement.Get("Aligner02").InitialComplete)
          {//檢查所有部件都正常才繼續

            return;
          }
          NodeManagement.Get("Robot01").Initial();
          NodeManagement.Get("Robot02").Initial();
          RobotFetchMode(NodeManagement.Get("Robot01"), new List<Job>());
          RobotFetchMode(NodeManagement.Get("Robot02"), new List<Job>());
          NodeManagement.Get("Robot01Status").SendCommand(new Transaction(new List<Job>(), "", "", Transaction.Command.RobotType.GetStatus, "1", 30000));
          NodeManagement.Get("Robot02Status").SendCommand(new Transaction(new List<Job>(), "", "", Transaction.Command.RobotType.GetStatus, "1", 30000));
          NodeManagement.Get("Aligner01Status").SendCommand(new Transaction(new List<Job>(), "", "", Transaction.Command.AlignerType.GetStatus, "1", 30000));
          NodeManagement.Get("Aligner02Status").SendCommand(new Transaction(new List<Job>(), "", "", Transaction.Command.AlignerType.GetStatus, "1", 30000));
          break;
        case "Initial":
        case "AutoInitial":
          NodeManagement.Get("Robot01").InitialComplete = false;
          NodeManagement.Get("Robot02").InitialComplete = false;
          NodeManagement.Get("Aligner01").InitialComplete = false;
          NodeManagement.Get("Aligner02").InitialComplete = false;
          AlarmUpdate.UpdateStatusSignal("Robot01", "Red");
          AlarmUpdate.UpdateStatusSignal("Robot02", "Red");
          AlarmUpdate.UpdateStatusSignal("Aligner01", "Red");
          AlarmUpdate.UpdateStatusSignal("Aligner02", "Red");


          NodeManagement.Get("Robot01").SendCommand(new Transaction(new List<Job>(), "", "", Transaction.Command.RobotType.Reset, "1", 30000));
          NodeManagement.Get("Robot02").SendCommand(new Transaction(new List<Job>(), "", "", Transaction.Command.RobotType.Reset, "1", 30000));
          NodeManagement.Get("Aligner01").SendCommand(new Transaction(new List<Job>(), "", "", Transaction.Command.AlignerType.Reset, "1", 30000));
          NodeManagement.Get("Aligner02").SendCommand(new Transaction(new List<Job>(), "", "", Transaction.Command.AlignerType.Reset, "1", 30000));
          break;
      }
    }

    public string GetMode()
    {
      string result = "";
      lock (this)
      {
        result = _Mode;
      }

      return result;
    }

    private void RobotFetchMode(Node RobotNode, List<Job> LastTargetJobs)
    {
      RobotNode.Phase = "1";
      if (RobotNode.JobList.Count == 0)//雙臂皆空
      {

        RobotNode.PreReady = false;

        List<Job> TargetJobs = new List<Job>();
        bool IsFound = false;
        foreach (Node.Route eachNode in RobotNode.RouteTable)
        {
          int FirstSlot = -1;
          bool ConsecutiveSlot = false;

          if (eachNode.NodeType.Equals("Port"))
          {//搜尋Port有沒有要處理的Wafer
            Node PortNode = NodeManagement.Get(eachNode.NodeName);
            List<Job> JobsSortBySlot = PortNode.JobList.Values.ToList();
            JobsSortBySlot.Sort((x, y) => { return Convert.ToInt16(x.Slot).CompareTo(Convert.ToInt16(y.Slot)); });
            foreach (Job eachJob in JobsSortBySlot)
            {
              if (!eachJob.ProcessFlag)
              {
                if (FirstSlot == -1)
                {
                  FirstSlot = Convert.ToInt16(eachJob.Slot);
                  TargetJobs.Add(eachJob);
                  RobotNode.CurrentLoadPort = PortNode.Name;
                  //找到第一片
                }
                else
                {
                  int diff = Convert.ToInt16(eachJob.Slot) - FirstSlot;
                  if (diff == 1)
                  {
                    ConsecutiveSlot = true;
                    TargetJobs.Add(eachJob);
                  }
                  else
                  {

                    ConsecutiveSlot = false;
                  }

                  break;//找到第二片
                }
              }
            }
            if (FirstSlot != -1)
            {
              if (ConsecutiveSlot)//雙臂同取
              {
                Transaction txn = new Transaction(TargetJobs, PortNode.Name, (FirstSlot + 1).ToString(), Transaction.Command.RobotType.DoubleGet, "", 10000);

                if (RobotNode.SendCommand(txn))
                {
                  Node NextRobot = NodeManagement.GetNextRobot(TargetJobs[0].Destination);


                  NextRobot.WaitForCarryCount += 2;
                  //logger.Debug(NextRobot.Name + " WaitForCarryCount:" + NextRobot.Status.WaitForCarryCount);

                }

              }
              else//單臂輪取 R軸
              {
                if (RobotNode.SendCommand(new Transaction(TargetJobs, PortNode.Name, FirstSlot.ToString(), Transaction.Command.RobotType.Get, "1", 10000)))
                {
                  Node NextRobot = NodeManagement.GetNextRobot(TargetJobs[0].Destination);

                  NextRobot.WaitForCarryCount += 1;
                  //logger.Debug(NextRobot.Name + " WaitForCarryCount:" + NextRobot.Status.WaitForCarryCount);

                }
              }
              IsFound = true;
              break;
            }
            else
            {



            }
          }
        }
        if (!IsFound)
        {

          logger.Debug("RobotFetchMode " + RobotNode.Name + " 找不到可以搬的Wafer");
          RobotNode.Phase = "2";
          RobotNode.PreReady = true;//標記目前Robot可以接受其他搬送命令

          TimeSpan diff = DateTime.Now - StartTime;
          logger.Info("Process Time: " + diff.TotalSeconds);
          var find = from Job in JobManagement.GetJobList()
                     where Job.ProcessFlag == false || !Job.Position.Equals(Job.Destination)
                     select Job;

          if (find.Count() == 0)
          {

            if (this.AutoReverse)
            {
              logger.Info("Start Reverse.");
              PortsettingUpdate.ReverseSetting();


              Node Aligner = NodeManagement.Get(PortsettingUpdate.GetCBText("PortSetting1Aligner_cb"));
              Node Port = NodeManagement.Get(PortsettingUpdate.GetCBText("PortSetting1Name_cb"));
              string Dest = PortsettingUpdate.GetCBText("PortSetting1Dest_cb");
              if (PortsettingUpdate.GetActive("PortSetting1Active_ck"))
              {
                Aligner.LockByNode = Port.Name;
                foreach (Job each in Port.JobList.Values.ToList())
                {
                  each.FromPort = Port.Name;
                  each.ProcessFlag = false;
                  each.Destination = Dest;
                }
              }

              Aligner = NodeManagement.Get(PortsettingUpdate.GetCBText("PortSetting2Aligner_cb"));
              Port = NodeManagement.Get(PortsettingUpdate.GetCBText("PortSetting2Name_cb"));
              Dest = PortsettingUpdate.GetCBText("PortSetting2Dest_cb");
              if (PortsettingUpdate.GetActive("PortSetting2Active_ck"))
              {
                Aligner.LockByNode = Port.Name;
                foreach (Job each in Port.JobList.Values.ToList())
                {
                  each.FromPort = Port.Name;
                  each.ProcessFlag = false;
                  each.Destination = Dest;
                }
              }
              SetMode("Auto");
            }
          }
        }
      }
      else if (RobotNode.JobList.Count == 1)//單臂有片
      {
        Node PortNode = NodeManagement.Get(RobotNode.CurrentLoadPort);
        int FirstSlot = -1;

        //搜尋Port有沒有要處理的Wafer

        List<Job> TargetJobs = new List<Job>();
        List<Job> JobsSortBySlot = new List<Job>();
        if (PortNode.JobList.Count != 0)
        {
          JobsSortBySlot = PortNode.JobList.Values.ToList();
        }
        JobsSortBySlot.Sort((x, y) => { return Convert.ToInt16(x.Slot).CompareTo(Convert.ToInt16(y.Slot)); });
        foreach (Job eachJob in JobsSortBySlot)
        {
          if (!eachJob.Destination.Equals(PortNode.Name))
          {
            if (FirstSlot == -1)
            {
              FirstSlot = Convert.ToInt16(eachJob.Slot);
              TargetJobs.Add(eachJob);
              break;//找到
            }

          }
        }
        if (FirstSlot != -1)
        {
          //單臂輪取 L軸

          if (RobotNode.SendCommand(new Transaction(TargetJobs, PortNode.Name, FirstSlot.ToString(), Transaction.Command.RobotType.Get, "2", 10000)))
          {
            Node NextRobot = NodeManagement.GetNextRobot(TargetJobs[0].Destination);

            NextRobot.WaitForCarryCount += 1;
            // logger.Debug(NextRobot.Name + " WaitForCarryCount:" + NextRobot.Status.WaitForCarryCount);

          }
        }
        else
        {
          //已沒有
          RobotNode.Phase = "2";//進入處理階段
          foreach (Job eachJob in RobotNode.JobList.Values.ToList())
          {
            eachJob.CurrentState = Job.State.WAIT_PUT;
          }
          Phase2Start(RobotNode, LastTargetJobs);
        }


      }
      else if (RobotNode.JobList.Count == 2)//雙臂有片
      {
        RobotNode.Phase = "2";//進入處理階段
        foreach (Job eachJob in RobotNode.JobList.Values.ToList())
        {
          eachJob.CurrentState = Job.State.WAIT_PUT;
        }
        Phase2Start(RobotNode, LastTargetJobs);
      }

    }

    private void Phase2Start(Node RobotNode, List<Job> LastTargetJobs)
    {
      Job ProcessJob = LastTargetJobs[0];
      List<Job> TargetJobs = new List<Job>();
      Node PutTarget = null;
      TransferRequest Request;


      TargetJobs.Add(ProcessJob);

      PutTarget = NodeManagement.GetReservAligner(ProcessJob.FromPort);

      if (PutTarget == null)
      {
        logger.Debug("Phase2Start 找不到可用的Aligner");
      }
      else
      {
        Node CurrentNode = NodeManagement.Get(RobotNode.CurrentPosition);
        if (CurrentNode != null)
        {
          if (CurrentNode.Type.Equals("LoadPort"))//目前為第一片
          {
            logger.Debug("處理第一片");
          }
          else
          {//目前為第二片
            logger.Debug("處理第二片");
            Node NotReservAligner = NodeManagement.GetNotReservAligner();
            if (NotReservAligner != null)
            {
              //logger.Debug("偵測為對傳模式");
              PutTarget = NotReservAligner;
            }
            else
            {
              //logger.Debug("偵測為交叉模式");
              if (CurrentNode.Type.Equals("Aligner"))//判斷Robot手臂目前所在位置是否為第二台Aligner
              {
                PutTarget = CurrentNode;
              }
            }
          }
        }




        //if (LastTargetJobs.Count == 2)
        //{
        //同時做兩支Robot動作 

        Transaction RobotTxn = new Transaction(TargetJobs, PutTarget.Name, "1", Transaction.Command.RobotType.PutWithoutBack, ProcessJob.Slot, 10000);
        if (PutTarget.JobList.Count != 0)
        {
          RobotTxn.Method = Transaction.Command.RobotType.PutWait;
        }
        if (!RobotNode.SendCommand(RobotTxn))
        {
          logger.Debug("Phase2Start " + RobotNode.Name + " Send Put Command fail.");
          //RobotNode.TodoList.Add(RobotTxn);
        }
        if (PutTarget.JobList.Count != 0)
        {
          return;
        }
        Node NextRobot = NodeManagement.GetNextRobot(PutTarget, ProcessJob);
        if (NextRobot == null || NextRobot.Name.Equals(RobotNode.Name))
        {
          logger.Debug("Phase2Start 找不到另一支Robot");
        }
        else
        {

          bool HasNeedPutWafer = false;
          foreach (Job each in NextRobot.JobList.Values.ToList())
          {
            if (!each.ProcessFlag)
            {
              HasNeedPutWafer = true;
            }
          }
          logger.Debug(NextRobot.PreReady + "  " + !HasNeedPutWafer + "  " + NextRobot.CurrentWaitNode + "   " + PutTarget.Name + "  " + NextRobot.CurrentWaitNode);
          if ((NextRobot.PreReady || !HasNeedPutWafer) && (NextRobot.CurrentWaitNode.Equals(PutTarget.Name) || NextRobot.CurrentWaitNode.Equals("")) && NextRobot.Phase.Equals("2"))
          {

            RobotTxn = new Transaction(TargetJobs, PutTarget.Name, "1", Transaction.Command.RobotType.GetWait, "1", 10000);
            lock (NextRobot)
            {
              if (!NextRobot.SendCommand(RobotTxn))
              {//Robot還沒放完片 加入佇列
                logger.Debug("命令失敗，加入佇列");
                Request = new TransferRequest();
                Request.ExcuteNode = NextRobot;
                Request.ExcuteCmd = Transaction.Command.RobotType.GetWait;
                Request.TargetNode = PutTarget;
                Request.TargetSlot = "1";
                Request.TargetJobs = TargetJobs;
                Request.Piority = 1;
                lock (NextRobot.TransferQueue)
                {
                  NextRobot.TransferQueue.Add(Request);
                }
                logger.Debug("Phase2Start " + NextRobot.Name + " Send GetW Command fail.");
                // NextRobot.TodoList.Add(RobotTxn);
              }
            }
          }
          else
          {//Robot還沒放完片 加入佇列

            Request = new TransferRequest();
            Request.ExcuteNode = NextRobot;
            Request.ExcuteCmd = Transaction.Command.RobotType.GetWait;
            Request.TargetNode = PutTarget;
            Request.TargetSlot = "1";
            Request.TargetJobs = TargetJobs;
            Request.Piority = 1;
            lock (NextRobot.TransferQueue)
            {
              NextRobot.TransferQueue.Add(Request);
            }

          }

        }

      }





    }

    private void RobotProcessMode(Node RobotNode, List<Job> LastTargetJobs, Transaction Txn)
    {
      try
      {
        TransferRequest Request;
        Job ProcessJob = LastTargetJobs[0];
        List<Job> TargetJobs = new List<Job>();
        switch (ProcessJob.CurrentState)
        {
          case Job.State.WAIT_PUT:
            if (Txn.Method != Transaction.Command.RobotType.PutWithoutBack)
            {
              break;
            }




            Node PutTarget = NodeManagement.Get(Txn.Position);
            if (ProcessJob.AlignerFlag)
            {
              ProcessJob.CurrentState = Job.State.WAIT_WHLD;



              if (PutTarget == null)
              {
                logger.Debug("RobotProcessMode 找不到可用的Aligner");
              }
              else
              {//同時做Robot&Aligner動作



                if (!PutTarget.SendCommand(new Transaction(LastTargetJobs, PutTarget.Name, "1", Transaction.Command.AlignerType.WaferHold, ProcessJob.Slot, 10000)))
                {
                  logger.Debug("RobotProcessMode " + PutTarget.Name + " Send WaferHold Command fail, Controller not ready.");
                }

                Node NextRobot = NodeManagement.GetNextRobot(PutTarget, ProcessJob);
                if (NextRobot != null)
                {
                  if (!NextRobot.Name.Equals(RobotNode.Name))
                  {
                    Transaction RobotTxn = new Transaction(LastTargetJobs, PutTarget.Name, "1", Transaction.Command.RobotType.PutBack, Txn.Arm, 10000);
                    RobotNode.SendCommand(RobotTxn);
                  }
                }



              }
            }
            else
            {//不做Align
              ProcessJob.CurrentState = Job.State.WAIT_GET;
              ProcessJob.ProcessFlag = true;
              if (PutTarget == null)
              {
                logger.Debug("RobotProcessMode 找不到可用的Aligner");
              }
              else
              {
                Node NextRobot = NodeManagement.GetNextRobot(PutTarget, ProcessJob);
                if (NextRobot == null)
                {
                  logger.Debug("RobotProcessMode 找不到另一支Robot");
                }
                else
                {

                  bool HasNeedPutWafer = false;
                  foreach (Job each in NextRobot.JobList.Values.ToList())
                  {
                    if (!each.ProcessFlag)
                    {
                      HasNeedPutWafer = true;
                    }
                  }
                  if ((NextRobot.PreReady || !HasNeedPutWafer) && (NextRobot.CurrentWaitNode.Equals(PutTarget.Name) || NextRobot.CurrentWaitNode.Equals("")))
                  {//已經可以下命令
                    string emptyArm = "";
                    if (!NextRobot.JobList.ContainsKey("1"))
                    {
                      emptyArm = "1";
                    }
                    else if (!NextRobot.JobList.ContainsKey("2"))
                    {
                      emptyArm = "2";
                    }
                    else
                    {
                      logger.Debug("AlignerAction State.WAIT_PUT:兩隻手臂都有東西，無法再拿.加入佇列");
                      Request = new TransferRequest();
                      Request.ExcuteNode = NextRobot;
                      Request.ExcuteCmd = Transaction.Command.RobotType.Get;
                      Request.TargetNode = PutTarget;
                      Request.TargetSlot = "1";
                      Request.TargetJobs = LastTargetJobs;
                      Request.Piority = 3;
                      lock (NextRobot.TransferQueue)
                      {
                        NextRobot.TransferQueue.Add(Request);
                      }
                      break;
                    }
                    Transaction RobotTxn = new Transaction(LastTargetJobs, PutTarget.Name, "1", Transaction.Command.RobotType.Get, emptyArm, 10000);
                    if (!NextRobot.SendCommand(RobotTxn))
                    {
                      logger.Debug("RobotProcessMode " + NextRobot.Name + " Send Get Command fail, Controller not ready.");
                      //NextRobot.TodoList.Add(RobotTxn);
                    }
                  }
                  else
                  {//還在做事 加入佇列
                    Request = new TransferRequest();
                    Request.ExcuteNode = NextRobot;
                    Request.ExcuteCmd = Transaction.Command.RobotType.Get;
                    Request.TargetNode = PutTarget;
                    Request.TargetSlot = "1";
                    Request.TargetJobs = LastTargetJobs;
                    Request.Piority = 3;
                    lock (NextRobot.TransferQueue)
                    {
                      NextRobot.TransferQueue.Add(Request);
                    }
                  }


                }


              }
            }

            break;
          case Job.State.WAIT_GET:
            if (Txn.Method != Transaction.Command.RobotType.Get && Txn.Method != Transaction.Command.RobotType.GetAfterWait)
            {
              break;
            }

            if (RobotNode.JobList.Count == 2)
            {
              if (RobotNode.WaitForCarryCount == 0)
              {
                RobotNode.Phase = "3";//都做完了進入放片階段
                RobotPutMode(RobotNode, new List<Job>());
              }
              else if (RobotNode.JobList["1"].ProcessFlag && RobotNode.JobList["2"].ProcessFlag)
              {
                RobotNode.Phase = "3";
                //都做完了進入放片階段
                RobotPutMode(RobotNode, new List<Job>());
              }
            }
            else if (RobotNode.JobList.Count == 1)
            {
              if (RobotNode.WaitForCarryCount == 0 && RobotNode.JobList["1"].ProcessFlag)
              {
                RobotNode.Phase = "3";//都做完了進入放片階段
                RobotPutMode(RobotNode, new List<Job>());
              }
              
            }



            if (ProcessJob.AlignerFlag)
            {
              ProcessJob.CurrentState = Job.State.WAIT_RET;



              Node LastAligner = NodeManagement.Get(ProcessJob.LastNode);



              if (LastAligner == null)
              {
                logger.Debug("RobotProcessMode 找不到上一站的Aligner");
              }
              else
              {
                Transaction AlignerTxn = new Transaction(LastTargetJobs, LastAligner.Name, "1", Transaction.Command.AlignerType.Retract, ProcessJob.Slot, 10000);
                AlignerTxn.Angle = "0";
                if (!LastAligner.SendCommand(AlignerTxn))
                {
                  logger.Debug("RobotProcessMode " + LastAligner.Name + " Send Retract Command fail, Controller not ready.");
                }
              }
            }
            else
            {//不做Align
              ProcessJob.CurrentState = Job.State.WAIT_UNLOAD;
              bool found = false;
              //尋找還未做的
              foreach (Job each in RobotNode.JobList.Values.ToList())
              {
                if (!each.ProcessFlag)
                {
                  List<Job> tmp = new List<Job>();
                  tmp.Add(each);
                  logger.Debug("找到還未Align的Wafer");
                  Phase2Start(RobotNode, tmp);
                  found = true;
                }
              }
              if (!found)
              {
                RobotNode.Phase = "3";//都做完了進入放片階段
                RobotPutMode(RobotNode, new List<Job>());
              }
            }
            break;

        }

      }
      catch (Exception e)
      {
        logger.Error(RobotNode.Name + "(RobotProcessMode)" + e.Message + "\n" + e.StackTrace);
      }
    }

    public void AlignerAction(Node AlignerNode, List<Job> LastTargetJobs, string FinMethod, bool IsWakeUp)
    {
      try
      {
        TransferRequest Request;
        Job ProcessJob = LastTargetJobs[0];

        switch (ProcessJob.CurrentState)
        {
          case Job.State.WAIT_WHLD:
            if (FinMethod != Transaction.Command.AlignerType.WaferHold)
            {
              break;
            }
            ProcessJob.CurrentState = Job.State.WAIT_ALIGN;
            Transaction txn = new Transaction(LastTargetJobs, AlignerNode.Name, "1", Transaction.Command.AlignerType.Align, ProcessJob.Slot, 10000);
            txn.Angle = "30";
            if (!AlignerNode.SendCommand(txn))
            {
              logger.Debug("AlignerAction " + AlignerNode.Name + " Send Align Command fail, Controller not ready.");
            }
            break;
          case Job.State.WAIT_ALIGN:
            if (FinMethod != Transaction.Command.AlignerType.Align)
            {
              break;
            }
            if (ProcessJob.OCRFlag)
            {
              ProcessJob.CurrentState = Job.State.WAIT_OCR;
              ProcessJob.ProcessFlag = true;
              Node OCRNode = NodeManagement.GetOCRByAligner(AlignerNode);
              if (!OCRNode.SendCommand(new Transaction(LastTargetJobs, OCRNode.Name, "1", Transaction.Command.OCRType.OCR, ProcessJob.Slot, 10000)))
              {
                logger.Debug("AlignerAction " + OCRNode.Name + " Send OCR Command fail, Controller not ready.");
              }
            }
            else
            {
              ProcessJob.CurrentState = Job.State.WAIT_WRLS;
              ProcessJob.ProcessFlag = true;

              if (!AlignerNode.SendCommand(new Transaction(LastTargetJobs, AlignerNode.Name, "1", Transaction.Command.AlignerType.WaferRelease, ProcessJob.Slot, 10000)))
              {
                logger.Debug("AlignerAction " + AlignerNode.Name + " Send WaferRelease Command fail, Controller not ready.");
              }
            }
            break;
          case Job.State.WAIT_WRLS:
            if (FinMethod != Transaction.Command.AlignerType.WaferRelease)
            {
              break;
            }



            ProcessJob.CurrentState = Job.State.WAIT_GET;
            Node NextRobot = NodeManagement.GetNextRobot(AlignerNode, ProcessJob);
            if (NextRobot == null)
            {
              logger.Debug("RobotProcessMode 找不到另一支Robot");
            }
            else
            {

              Request = new TransferRequest();
              //logger.Debug(NextRobot.Status.PreReady + "      "+ NextRobot.CurrentWaitNode + "     "+ AlignerNode.Name);
              bool HasNeedPutWafer = false;
              foreach (Job each in NextRobot.JobList.Values.ToList())
              {
                if (!each.ProcessFlag)
                {
                  HasNeedPutWafer = true;
                }
              }
              if ((NextRobot.PreReady || !HasNeedPutWafer) && (NextRobot.CurrentWaitNode.Equals(AlignerNode.Name) || NextRobot.CurrentWaitNode.Equals("")))
              {
                Transaction RobotTxn;

                if (NextRobot.Prepare)//判斷目前Robot是否為等待取片狀態
                {
                  RobotTxn = new Transaction(LastTargetJobs, AlignerNode.Name, "1", Transaction.Command.RobotType.GetAfterWait, "", 10000);
                  Request.ExcuteCmd = Transaction.Command.RobotType.GetAfterWait;
                }
                else
                {
                  RobotTxn = new Transaction(LastTargetJobs, AlignerNode.Name, "1", Transaction.Command.RobotType.Get, "", 10000);
                  Request.ExcuteCmd = Transaction.Command.RobotType.Get;
                }

                if (!NextRobot.JobList.ContainsKey("1"))
                {
                  RobotTxn.Arm = "1";
                }
                else if (!NextRobot.JobList.ContainsKey("2"))
                {
                  RobotTxn.Arm = "2";
                }
                else
                {
                  logger.Debug("AlignerAction State.WAIT_WRLS:兩隻手臂都有東西，無法再拿:" + NextRobot.Name);
                  Request.ExcuteNode = NextRobot;

                  Request.TargetNode = AlignerNode;
                  Request.TargetSlot = "1";
                  Request.TargetJobs = LastTargetJobs;
                  Request.Piority = 3;
                  lock (NextRobot.TransferQueue)
                  {
                    NextRobot.TransferQueue.Add(Request);
                  }
                  break;
                }
                lock (NextRobot)
                {
                  if (!NextRobot.SendCommand(RobotTxn))
                  {//目前還不行接受命令，加入佇列

                    Request.ExcuteNode = NextRobot;
                    Request.ExcuteCmd = Transaction.Command.RobotType.Get;
                    Request.TargetNode = AlignerNode;
                    Request.TargetSlot = "1";
                    Request.TargetJobs = LastTargetJobs;
                    Request.Piority = 3;
                    lock (NextRobot.TransferQueue)
                    {
                      NextRobot.TransferQueue.Add(Request);
                      logger.Debug("加入佇列:" + Request.ExcuteCmd);
                    }
                    logger.Debug("RobotProcessMode State.WAIT_WRLS " + NextRobot.Name + " Send GetAfterWait Command fail, Controller not ready.");
                    // NextRobot.TodoList.Add(RobotTxn);
                  }
                }
              }
              else
              {

                Request.ExcuteNode = NextRobot;
                Request.ExcuteCmd = Transaction.Command.RobotType.Get;
                Request.TargetNode = AlignerNode;
                Request.TargetSlot = "1";
                Request.TargetJobs = LastTargetJobs;
                Request.Piority = 3;
                lock (NextRobot.TransferQueue)
                {
                  NextRobot.TransferQueue.Add(Request);
                  logger.Debug("加入佇列:" + Request.ExcuteCmd);
                }
              }

            }

            break;
          case Job.State.WAIT_RET:
            if (FinMethod != Transaction.Command.AlignerType.Retract)
            {
              break;
            }
            ProcessJob.CurrentState = Job.State.WAIT_UNLOAD;
            Node RobotNode = NodeManagement.Get(ProcessJob.Position);
            if (RobotNode == null)
            {
              logger.Debug("AlignerAction State.WAIT_RET:找不到Robot.");
            }
            else
            {


              bool found = false;
              List<Job> WaitProcess = new List<Job>();

              //尋找還未做的
              foreach (Job each in RobotNode.JobList.Values.ToList())
              {
                if (!each.ProcessFlag)
                {

                  WaitProcess.Add(each);
                  logger.Debug("找到還未Align的Wafer");

                  found = true;
                }
              }



              if (!found)
              {

                //if ((RobotNode.JobList.Count == 2 || RobotNode.WaitForCarryCount == 0))
                //{
                //    RobotNode.Phase = "3";//都做完了進入放片階段
                //    RobotPutMode(RobotNode, new List<Job>());
                //}
                //else
                //{
                //    logger.Debug("還有待搬送的Wafer，等待中:" + RobotNode.WaitForCarryCount);
                //}

              }
              else
              {
                Phase2Start(RobotNode, WaitProcess);
              }


            }
            break;
        }
      }
      catch (Exception e)
      {
        logger.Error(AlignerNode.Name + "(AlignerAction)" + e.Message + "\n" + e.StackTrace);
      }
    }

    public void OCRAction(Node OCRNode, List<Job> LastTargetJobs, string FinMethod)
    {
      try
      {

        Job ProcessJob = LastTargetJobs[0];

        switch (ProcessJob.CurrentState)
        {
          case Job.State.WAIT_OCR:
            if (FinMethod != Transaction.Command.OCRType.OCR)
            {
              break;
            }
            ProcessJob.CurrentState = Job.State.WAIT_WRLS;
            Node AlignerNode = NodeManagement.GetAlignerByOCR(OCRNode);
            if (!AlignerNode.SendCommand(new Transaction(LastTargetJobs, AlignerNode.Name, "1", Transaction.Command.AlignerType.WaferRelease, ProcessJob.Slot, 10000)))
            {
              logger.Debug("OCRAction " + AlignerNode.Name + " Send WaferRelease Command fail, Controller not ready.");
            }
            break;

        }
      }
      catch (Exception e)
      {
        logger.Error(OCRNode.Name + "(OCRAction)" + e.Message + "\n" + e.StackTrace);
      }
    }

    private void RobotPutMode(Node RobotNode, List<Job> LastTargetJobs)
    {
      Job Wafer;
      List<Job> TargetJobs = new List<Job>();
      if (RobotNode.JobList.Count == 0)//雙臂皆空
      {
        Node UnPort = NodeManagement.Get(LastTargetJobs[0].Destination);


        RobotNode.Phase = "1";//進入取片階段
        RobotFetchMode(RobotNode, LastTargetJobs);
      }
      else if (RobotNode.JobList.Count == 1)//單臂有片
      {
        //單臂放片
        Wafer = RobotNode.JobList.Values.ToList()[0];
        TargetJobs.Add(Wafer);
        RobotNode.SendCommand(new Transaction(TargetJobs, Wafer.Destination, Wafer.DestinationSlot, Transaction.Command.RobotType.Put, Wafer.Slot, 10000));
      }
      else if (RobotNode.JobList.Count == 2)//雙臂有片
      {
        List<Job> Jobs = RobotNode.JobList.Values.ToList();
        Jobs.Sort((x, y) => { return Convert.ToInt16(x.DestinationSlot).CompareTo(Convert.ToInt16(y.DestinationSlot)); });
        if (Jobs[0].Destination.Equals(Jobs[1].Destination))
        {
          int SlotDiff = Convert.ToInt16(Jobs[1].DestinationSlot) - Convert.ToInt16(Jobs[0].DestinationSlot);
          if (SlotDiff == 1)
          {//雙臂同放
            Wafer = Jobs[1];
            Transaction txn = new Transaction(Jobs, Wafer.Destination, (Convert.ToInt16(Wafer.DestinationSlot)).ToString(), Transaction.Command.RobotType.DoublePut, "", 10000);

            RobotNode.SendCommand(txn);
          }
          else
          {//單臂輪放
            Wafer = RobotNode.JobList.Values.ToList()[0];
            TargetJobs.Add(Wafer);
            RobotNode.SendCommand(new Transaction(TargetJobs, Wafer.Destination, Wafer.DestinationSlot, Transaction.Command.RobotType.Put, Wafer.Slot, 10000));
          }
        }


      }
    }
    private void RobotAction(Node RobotNode, List<Job> LastTargetJobs, Transaction Txn)
    {
      switch (RobotNode.Phase)
      {
        case "1":
          RobotFetchMode(RobotNode, LastTargetJobs);
          break;
        case "2":
          RobotProcessMode(RobotNode, LastTargetJobs, Txn);
          break;
        case "3":
          RobotPutMode(RobotNode, LastTargetJobs);
          break;
      }
    }




    public void On_Command_Excuted(Node Node, Transaction Txn, ReturnMessage Msg)
    {
      try
      {
        logger.Debug("On_Command_Excuted");



        if (Txn.Method.Equals(Transaction.Command.RobotType.Reset))
        {
          AlarmManagement.Remove(Node.Name);
        }
        if (GetMode().Equals("Auto"))
        {
          if (Txn.Method.Equals(Transaction.Command.RobotType.GetStatus))
          {
            SpinWait.SpinUntil(() => false, 500);
            Node.SendCommand(new Transaction(new List<Job>(), "", "", Transaction.Command.RobotType.GetStatus, "1", 30000));
            return;
          }
          if (!NodeManagement.Get("Robot01").InitialComplete || !NodeManagement.Get("Robot02").InitialComplete || !NodeManagement.Get("Aligner01").InitialComplete || !NodeManagement.Get("Aligner02").InitialComplete)
          {//檢查所有部件都正常才繼續
            return;
          }
          switch (Node.Type)
          {
            
            case "Aligner":
              switch (Txn.Method)
              {
                case Transaction.Command.AlignerType.WaferHold:



                  break;
                case Transaction.Command.AlignerType.WaferRelease:


                  break;
              }
              break;
            case "Robot":
              switch (Txn.Method)
              {
                case Transaction.Command.RobotType.GetWait:
                  Node.CurrentWaitNode = Txn.Position;//Queue先處理目前鎖定的Node

                  break;
                case Transaction.Command.RobotType.PutWait:
                  Node.CurrentPosition = Txn.Position;

                  break;
                case Transaction.Command.RobotType.WaitBeforeGet://標記手臂已經處於伸出狀態

                  Node.Prepare = true;

                  break;
                case Transaction.Command.RobotType.Get:
                case Transaction.Command.RobotType.GetAfterWait:
                  //關閉Robot保護，暫不執行命令佇列
                  Node.PreReady = false;
                  Node.Prepare = false;
                  Node.CurrentWaitNode = "";//解除鎖定
                  break;
                case Transaction.Command.RobotType.Put:

                  break;
                case Transaction.Command.RobotType.PutWithoutBack:

                  Node.Prepare = true;

                  Node PutTarget = NodeManagement.Get(Txn.Position);
                  if (!PutTarget.SendCommand(new Transaction(Txn.TargetJobs, PutTarget.Name, "1", Transaction.Command.AlignerType.WaferHold, "", 10000)))
                  {
                    logger.Debug("RobotProcessMode " + PutTarget.Name + " Send WaferHold Command fail, Controller not ready.");
                  }

                  break;
                case Transaction.Command.RobotType.PutBack:
                  Node.Prepare = false;
                  PutTarget = NodeManagement.Get(Txn.Position);

                  Node NextRobot = NodeManagement.GetNextRobot(PutTarget, Txn.TargetJobs[0]);
                  if (NextRobot == null)
                  {
                    logger.Debug("RobotProcessMode 找不到另一支Robot");
                  }
                  else
                  {

                    TransferRequest Request;
                    bool HasNeedPutWafer = false;
                    foreach (Job each in NextRobot.JobList.Values.ToList())
                    {
                      if (!each.ProcessFlag)
                      {
                        HasNeedPutWafer = true;
                      }
                    }

                    if ((NextRobot.PreReady || !HasNeedPutWafer) && (NextRobot.CurrentWaitNode.Equals(PutTarget.Name) || NextRobot.CurrentWaitNode.Equals("")) && NextRobot.Phase.Equals("2"))
                    {
                      string emptyArm = "";
                      if (!NextRobot.JobList.ContainsKey("1"))
                      {
                        emptyArm = "1";
                      }
                      else if (!NextRobot.JobList.ContainsKey("2"))
                      {
                        emptyArm = "2";
                      }
                      else
                      {
                        logger.Debug("AlignerAction State.WAIT_PUT:兩隻手臂都有東西，加入佇列.");
                        Request = new TransferRequest();
                        Request.ExcuteNode = NextRobot;
                        Request.ExcuteCmd = Transaction.Command.RobotType.WaitBeforeGet;
                        Request.TargetNode = PutTarget;
                        Request.TargetSlot = "1";
                        Request.TargetJobs = Txn.TargetJobs;
                        Request.Piority = 2;
                        lock (NextRobot.TransferQueue)
                        {
                          NextRobot.TransferQueue.Add(Request);
                        }
                        break;
                      }
                      Transaction RobotTxn = new Transaction(Txn.TargetJobs, PutTarget.Name, "1", Transaction.Command.RobotType.WaitBeforeGet, emptyArm, 10000);
                      lock (NextRobot)
                      {
                        if (!NextRobot.SendCommand(RobotTxn))
                        {

                          Request = new TransferRequest();
                          Request.ExcuteNode = NextRobot;
                          Request.ExcuteCmd = Transaction.Command.RobotType.WaitBeforeGet;
                          Request.TargetNode = PutTarget;
                          Request.TargetSlot = "1";
                          Request.TargetJobs = Txn.TargetJobs;
                          Request.Piority = 2;
                          lock (NextRobot.TransferQueue)
                          {
                            NextRobot.TransferQueue.Add(Request);
                            logger.Debug("加入佇列:" + Request.ExcuteCmd);
                          }
                        }
                      }

                    }
                    else
                    {

                      Request = new TransferRequest();
                      Request.ExcuteNode = NextRobot;
                      Request.ExcuteCmd = Transaction.Command.RobotType.WaitBeforeGet;
                      Request.TargetNode = PutTarget;
                      Request.TargetSlot = "1";
                      Request.TargetJobs = Txn.TargetJobs;
                      Request.Piority = 2;
                      lock (NextRobot.TransferQueue)
                      {
                        NextRobot.TransferQueue.Add(Request);

                        logger.Debug("加入佇列:" + Request.ExcuteCmd);
                      }
                    }



                  }
                  break;
              }
              break;
          }
        }
        else if (GetMode().Equals("Initial") || GetMode().Equals("AutoInitial"))
        {//Initial mode
          switch (Node.Type)
          {
            case "Robot":
              switch (Txn.Method)
              {
                case Transaction.Command.RobotType.Reset:
                  Node.SendCommand(new Transaction(new List<Job>(), "", "", Transaction.Command.RobotType.RobotServo, "1", 30000));
                  break;
                case Transaction.Command.RobotType.RobotServo:
                  Node.SendCommand(new Transaction(new List<Job>(), "", "", Transaction.Command.RobotType.RobotMode, "1", 30000));
                  break;
                case Transaction.Command.RobotType.RobotMode:
                  Node.SendCommand(new Transaction(new List<Job>(), "", "", Transaction.Command.RobotType.WaferRelease, "1", 30000));
                  break;

                case Transaction.Command.RobotType.RobotSpeed:
                  Node.SendCommand(new Transaction(new List<Job>(), "", "", Transaction.Command.RobotType.RobotHome, "", 30000));
                  break;
              }
              break;
            case "Aligner":
              switch (Txn.Method)
              {
                case Transaction.Command.AlignerType.Reset:
                  Node.SendCommand(new Transaction(new List<Job>(), "", "", Transaction.Command.AlignerType.AlignerServo, "1", 30000));
                  break;
                case Transaction.Command.AlignerType.AlignerServo:
                  Node.SendCommand(new Transaction(new List<Job>(), "", "", Transaction.Command.AlignerType.AlignerMode, "1", 30000));
                  break;
                case Transaction.Command.AlignerType.AlignerMode:
                  Node.SendCommand(new Transaction(new List<Job>(), "", "", Transaction.Command.AlignerType.AlignerSpeed, "0", 30000));
                  break;
                case Transaction.Command.AlignerType.AlignerSpeed:
                  Node.SendCommand(new Transaction(new List<Job>(), "", "", Transaction.Command.AlignerType.AlignerOrigin, "0", 30000));
                  break;

              }
              break;
          }
        }
        else
        {
          switch (Node.Type)
          {
            case "LoadPort":
              switch (Txn.Method)
              {
                case Transaction.Command.LoadPortType.GetLED:
                  PortStatus.UpdatePortStatus(Node.Name, Msg.Value);
                  break;
                case Transaction.Command.LoadPortType.GetStatus:
                  PortStatus.UpdatePortStatus(Node.Name, Msg.Value);
                  break;
                case Transaction.Command.LoadPortType.GetCount:
                  PortStatus.UpdatePortStatus(Node.Name, Msg.Value);
                  break;
                case Transaction.Command.LoadPortType.GetMapping:
                  PortStatus.UpdatePort(Node.Name, Msg.Value);
                  break;
              }
              break;
          }
          }
      }
      catch (Exception e)
      {
        logger.Error(Node.Controller + "-" + Node.AdrNo + "(On_Command_Excuted)" + e.Message + "\n" + e.StackTrace);
      }
    }

    public void On_Command_Finished(Node Node, Transaction Txn, ReturnMessage Msg)
    {


      var watch = System.Diagnostics.Stopwatch.StartNew();
      try
      {
        Node NextRobot;
        logger.Debug("On_Command_Finished:" + Msg.Command);

        //紀錄Robot最後位置
        Node.CurrentPosition = Txn.Position;
        //logger.Debug(JsonConvert.SerializeObject(Txn));


        if (GetMode().Equals("Auto"))
        {
          logger.Debug("Robot01 " + NodeManagement.Get("Robot01").InitialComplete + " Robot02 " + NodeManagement.Get("Robot02").InitialComplete + " Aligner01 " + NodeManagement.Get("Aligner01").InitialComplete + " Aligner02 " + NodeManagement.Get("Aligner02").InitialComplete);

          if (!NodeManagement.Get("Robot01").InitialComplete || !NodeManagement.Get("Robot02").InitialComplete || !NodeManagement.Get("Aligner01").InitialComplete || !NodeManagement.Get("Aligner02").InitialComplete)
          {//檢查所有部件都正常才繼續
            logger.Debug("Detected node error ,Stop all action.");
            return;
          }
          switch (Node.Type)
          {
            case "Robot":
              switch (Txn.Method)
              {
                case Transaction.Command.RobotType.DoubleGet:
                  for (int i = 0; i < Txn.TargetJobs.Count; i++)
                  {
                    Node TargetNode5 = NodeManagement.Get(Txn.TargetJobs[i].Position);
                    Job tmp;
                    TargetNode5.JobList.TryRemove(Txn.TargetJobs[i].Slot, out tmp);
                    Txn.TargetJobs[i].LastNode = Txn.TargetJobs[i].Position;
                    Txn.TargetJobs[i].Slot = (i + 1).ToString();
                    Txn.TargetJobs[i].Position = Node.Name;
                    Node.JobList.TryAdd(Txn.TargetJobs[i].Slot, Txn.TargetJobs[i]);
                    NodeManagement.UpdatePortToUI(Txn.TargetJobs[i]);
                  }



                  break;
                case Transaction.Command.RobotType.DoublePut:
                  for (int i = 0; i < Txn.TargetJobs.Count; i++)
                  {
                    Node TargetNode6 = NodeManagement.Get(Txn.Position);
                    Job tmp;
                    Node.JobList.TryRemove(Txn.TargetJobs[i].Slot, out tmp);
                    Txn.TargetJobs[i].LastNode = Txn.TargetJobs[i].Position;
                    switch (i)
                    {
                      case 0:
                        Txn.TargetJobs[i].Slot = (Convert.ToInt16(Txn.Slot) - 1).ToString();

                        break;
                      case 1:
                        Txn.TargetJobs[i].Slot = Txn.Slot;

                        break;
                    }


                    Txn.TargetJobs[i].Position = Txn.Position;
                    TargetNode6.JobList.TryAdd(Txn.TargetJobs[i].Slot, Txn.TargetJobs[i]);
                    NodeManagement.UpdatePortToUI(Txn.TargetJobs[i]);
                  }

                  break;
                case Transaction.Command.RobotType.Get://更新Wafer位置
                case Transaction.Command.RobotType.GetAfterWait:

                  //logger.Debug(Txn.TargetJobs.Count.ToString());
                  for (int i = 0; i < Txn.TargetJobs.Count; i++)
                  {
                    Node TargetNode4 = NodeManagement.Get(Txn.TargetJobs[i].Position);
                    Job tmp;
                    TargetNode4.JobList.TryRemove(Txn.TargetJobs[i].Slot, out tmp);
                    Txn.TargetJobs[i].LastNode = Txn.TargetJobs[i].Position;
                    Txn.TargetJobs[i].Position = Node.Name;
                    switch (i)
                    {
                      case 0:
                        Txn.TargetJobs[i].Slot = Txn.Arm;

                        break;
                      case 1:
                        Txn.TargetJobs[i].Slot = Txn.Arm2;

                        break;
                    }

                    Node.JobList.TryAdd(Txn.TargetJobs[i].Slot, Txn.TargetJobs[i]);

                    NextRobot = NodeManagement.GetNextRobot(Txn.TargetJobs[i].Destination);
                    if (NextRobot != null) //當目前的Robot 同時也是搬送到目的地的Robot
                    {
                      if (Txn.TargetJobs[i].ProcessFlag)
                      {
                        //扣掉待搬送數量
                        NextRobot.WaitForCarryCount--;
                        //logger.Debug(NextRobot.Name + " WaitForCarryCount:" + NextRobot.Status.WaitForCarryCount);

                      }
                    }
                    else
                    {
                      logger.Error(Txn.TargetJobs[i].Job_Id + "找不到目的地搬送Robot");
                    }
                    // logger.Debug(JsonConvert.SerializeObject(Txn.TargetJobs[i]));
                    NodeManagement.UpdatePortToUI(Txn.TargetJobs[i]);
                  }

                  //logger.Debug(JsonConvert.SerializeObject(Txn));
                  break;
                case Transaction.Command.RobotType.Put:
                case Transaction.Command.RobotType.PutWithoutBack:
                  //logger.Debug(Txn.TargetJobs.Count.ToString());

                  Node TargetNode1 = NodeManagement.Get(Txn.Position);
                  if (TargetNode1 != null)
                  {
                    TargetNode1.Prepare = true;
                  }

                  Node.PreReady = true;


                  for (int i = 0; i < Txn.TargetJobs.Count; i++)
                  {
                    Job tmp;
                    Node.JobList.TryRemove(Txn.TargetJobs[i].Slot, out tmp);
                    Txn.TargetJobs[i].LastNode = Txn.TargetJobs[i].Position;
                    Txn.TargetJobs[i].Position = Txn.Position;
                    switch (i)
                    {
                      case 0:
                        Txn.TargetJobs[i].Slot = Txn.Slot;

                        break;
                      case 1:
                        Txn.TargetJobs[i].Slot = Txn.Slot2;
                        Txn.TargetJobs[i].Position = Txn.Position2;
                        break;
                    }
                    Node TargetNode3 = NodeManagement.Get(Txn.TargetJobs[i].Position);
                    TargetNode3.JobList.TryAdd(Txn.TargetJobs[i].Slot, Txn.TargetJobs[i]);

                    // logger.Debug(JsonConvert.SerializeObject(Txn.TargetJobs[i]));
                    NodeManagement.UpdatePortToUI(Txn.TargetJobs[i]);
                  }

                  //logger.Debug(JsonConvert.SerializeObject(Txn));
                  break;


                case Transaction.Command.RobotType.PutWait:
                  Node PutNode = NodeManagement.Get(Txn.Position);
                  if (PutNode != null)
                  {
                    //while (PutNode.JobList.Count != 0)
                    //{
                    //    Thread.Sleep(100);
                    //}
                    SpinWait.SpinUntil(() => PutNode.JobList.Count == 0, 30000);
                    if (PutNode.JobList.Count != 0)
                    {
                      logger.Error("");
                    }

                    foreach (Job WaitingJob in Node.JobList.Values.ToList())
                    {
                      if (!WaitingJob.ProcessFlag)
                      {
                        logger.Debug("2");
                        List<Job> tmpJobs = new List<Job>();
                        tmpJobs.Add(WaitingJob);
                        Transaction RobotTxn = new Transaction(tmpJobs, PutNode.Name, "1", Transaction.Command.RobotType.PutWithoutBack, WaitingJob.Slot, 10000);

                        if (!Node.SendCommand(RobotTxn))
                        {
                          logger.Debug("Phase2Start " + Node.Name + " Send Put Command fail.");

                        }
                        break;
                      }
                      else
                      {
                        logger.Debug("1");
                      }

                    }
                  }

                  break;
                case Transaction.Command.RobotType.PutBack:

                  //---------------對傳模式
                  int WaitForCarryCount = 0;


                  WaitForCarryCount = Node.WaitForCarryCount;

                  if (WaitForCarryCount == 0)
                  {
                    List<Job> WaitProcess = new List<Job>();

                    //尋找還未做的
                    foreach (Job each in Node.JobList.Values.ToList())
                    {
                      if (!each.ProcessFlag)
                      {

                        WaitProcess.Add(each);
                        logger.Debug("找到還未Align的Wafer");


                      }
                    }

                    if (WaitProcess.Count != 0)
                    {
                      Phase2Start(Node, WaitProcess);
                    }
                    else
                    {
                      if (Node.JobList.Count != 0)
                      {
                        Node.Phase = "3";//都做完了進入放片階段
                        RobotPutMode(Node, new List<Job>());
                      }
                      else
                      {
                        Node.Phase = "1";//進入取片階段
                        RobotFetchMode(Node, new List<Job>());
                      }
                    }
                  }
                  else
                  {

                  }
                  //---------------對傳模式

                  if (Txn.TargetJobs[0].CurrentState != Job.State.WAIT_GET)
                  {

                  }
                  break;
              }
              if (Txn.Method != Transaction.Command.RobotType.GetWait)
              {
                RobotAction(Node, Txn.TargetJobs, Txn);
              }
              break;
            case "Aligner":
              AlignerAction(Node, Txn.TargetJobs, Txn.Method, false);
              break;
            case "OCR":
              OCRAction(Node, Txn.TargetJobs, Txn.Method);
              break;
          }


          bool HasNeedPutWafer = false;
          foreach (Job each in Node.JobList.Values.ToList())
          {
            if (!each.ProcessFlag)
            {
              HasNeedPutWafer = true;
            }
          }
          logger.Debug("PreReady:" + Node.PreReady + " HasNeedPutWafer:" + HasNeedPutWafer + " Phase:" + Node.Phase);
          if ((Node.PreReady || !HasNeedPutWafer) && Node.Phase != "3")
          {
            lock (Node.TransferQueue)
            {
              if (Node.TransferQueue.Count != 0)
              {

                Node.TransferQueue.Sort((x, y) => { return y.Piority.CompareTo(x.Piority); });//排序大在前

                foreach (TransferRequest Request in Node.TransferQueue)
                {

                  //to do 
                  if (Request.TargetNode.Name.Equals(Node.CurrentWaitNode) || Node.CurrentWaitNode.Equals(""))
                  {
                    //logger.Info("11111111");
                    if (Node.CurrentWaitNode.Equals(""))
                    {
                      Node.CurrentWaitNode = Request.TargetNode.Name;
                    }
                    string Arm = "";
                    switch (Request.ExcuteNode.Type)
                    {
                      case "Robot":
                        switch (Request.ExcuteCmd)
                        {
                          case Transaction.Command.RobotType.GetWait:
                            Arm = "1";
                            break;
                          case Transaction.Command.RobotType.WaitBeforeGet:
                          case Transaction.Command.RobotType.GetAfterWait:
                          case Transaction.Command.RobotType.Get:

                            if (!Request.ExcuteNode.JobList.ContainsKey("1"))
                            {
                              Arm = "1";
                            }
                            else if (!Request.ExcuteNode.JobList.ContainsKey("2"))
                            {
                              Arm = "2";
                            }
                            else
                            {
                              logger.Debug("兩隻手臂都有東西，無法再拿.");
                              break;
                            }
                            break;
                          case Transaction.Command.RobotType.Put:

                            break;

                        }
                        if (Request.ExcuteNode.Prepare && Request.ExcuteCmd == Transaction.Command.RobotType.Get)
                        {
                          Request.ExcuteCmd = Transaction.Command.RobotType.GetAfterWait;
                        }

                        if (Request.ExcuteCmd.Equals(Transaction.Command.RobotType.GetAfterWait) && !Request.ExcuteNode.Prepare)
                        {
                          Request.ExcuteCmd = Transaction.Command.RobotType.Get;
                        }

                        lock (Request.ExcuteNode)
                        {
                          if (Request.ExcuteNode.SendCommand(new Transaction(Request.TargetJobs, Request.TargetNode.Name, Request.TargetSlot, Request.ExcuteCmd, Arm, 30000)))
                          {
                            Node.TransferQueue.RemoveAll(r => r.TargetNode.Name.Equals(Node.CurrentWaitNode));//只做優先權最高的並刪除目前鎖定的Node相關指令
                          }
                        }
                        // logger.Info(Request.ExcuteCmd);

                        break;
                    }
                    break;
                  }
                }


              }
            }
          }

        }
        else if (GetMode().Equals("Initial") || GetMode().Equals("AutoInitial"))
        {//Initial mode
          switch (Node.Type)
          {
            case "Robot":
              switch (Txn.Method)
              {

                case Transaction.Command.RobotType.WaferRelease:
                  if (Txn.Arm.Equals("1"))
                  {
                    Node.SendCommand(new Transaction(new List<Job>(), "", "", Transaction.Command.RobotType.WaferRelease, "2", 30000));
                  }
                  else
                  {
                    Node.SendCommand(new Transaction(new List<Job>(), "", "", Transaction.Command.RobotType.RobotSpeed, "0", 30000));
                  }
                  break;

                case Transaction.Command.RobotType.RobotHome:
                  //end
                  NodeManagement.Get(Node.Name).InitialComplete = true;
                  AlarmUpdate.UpdateStatusSignal(Node.Name, "Green");
                  if (NodeManagement.Get("Robot01").InitialComplete && NodeManagement.Get("Robot02").InitialComplete && NodeManagement.Get("Aligner01").InitialComplete && NodeManagement.Get("Aligner02").InitialComplete)
                  {

                    if (GetMode().Equals("AutoInitial"))
                    {
                      SetMode("Auto");
                    }
                    else
                    {
                      _Mode = "";
                    }
                  }
                  break;
              }
              break;
            case "Aligner":
              switch (Txn.Method)
              {

                case Transaction.Command.AlignerType.AlignerOrigin:
                  Node.SendCommand(new Transaction(new List<Job>(), "", "", Transaction.Command.AlignerType.Retract, "0", 30000));
                  break;
                case Transaction.Command.AlignerType.Retract:
                  //end
                  NodeManagement.Get(Node.Name).InitialComplete = true;
                  AlarmUpdate.UpdateStatusSignal(Node.Name, "Green");
                  if (NodeManagement.Get("Robot01").InitialComplete && NodeManagement.Get("Robot02").InitialComplete && NodeManagement.Get("Aligner01").InitialComplete && NodeManagement.Get("Aligner02").InitialComplete)
                  {

                    if (GetMode().Equals("AutoInitial"))
                    {
                      SetMode("Auto");
                    }
                    else
                    {
                      _Mode = "";
                    }
                  }
                  break;
              }
              break;
          }
        }
        // logger.Debug(Node.Name + JsonConvert.SerializeObject(Node.JobList));

      }
      catch (Exception e)
      {
        logger.Error(Node.Controller + "-" + Node.AdrNo + "(On_Command_Finished)" + e.Message + "\n" + e.StackTrace);
      }
      watch.Stop();
      var elapsedMs = watch.ElapsedMilliseconds;
      logger.Info("On_Command_Finished ProcessTime:" + elapsedMs.ToString());
      JobStateUpdate.UpdateJobStatus(JobManagement.GetJobList());
      JobStateUpdate.UpdateNodeStatus(NodeManagement.GetList());

    }

    public void On_Command_TimeOut(Node Node, Transaction Txn)
    {
      logger.Debug("Transaction TimeOut:" + Txn.CommandEncodeStr);

      Node.InitialComplete = false;

      AlarmInfo CurrentAlarm = new AlarmInfo();
      CurrentAlarm.NodeName = Node.Name.Replace("Status", "");
      CurrentAlarm.AlarmCode = "00000001";
      CurrentAlarm.SystemAlarmCode = "FF00000001";
      CurrentAlarm.Desc = "命令逾時,連線異常";
      CurrentAlarm.TimeStamp = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss:fff");

      AlarmManagement.Add(CurrentAlarm);
      SetMode("Stop");
    }

    public void On_Event_Trigger(Node Node, ReturnMessage Msg)
    {
      try
      {
        logger.Debug("On_Event_Trigger");

        if (Msg.Command.Equals("ERROR"))
        {
          Node.InitialComplete = false;
          logger.Debug("On_Command_Error");
          AlarmInfo CurrentAlarm = new AlarmInfo();
          CurrentAlarm.NodeName = Node.Name;
          CurrentAlarm.AlarmCode = Msg.Value;
          try
          {

            AlarmMessage Detail = this.Get(Node.Brand, Node.Type, CurrentAlarm.AlarmCode);

            CurrentAlarm.SystemAlarmCode = Detail.CodeID;
            CurrentAlarm.Desc = Detail.Code_Cause;
            CurrentAlarm.EngDesc = Detail.Code_Cause_English;
          }
          catch
          {
            CurrentAlarm.Desc = "未定義";
          }
          CurrentAlarm.TimeStamp = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss:fff");

          AlarmManagement.Add(CurrentAlarm);
          SetMode("Stop");
        }
      }
      catch (Exception e)
      {
        logger.Error(Node.Controller + "-" + Node.AdrNo + "(On_Command_Finished)" + e.Message + "\n" + e.StackTrace);
      }
    }

    public void On_Controller_State_Changed(string Device_ID, string Status)
    {
      ConnectStateUpdate.UpdateControllerStatus(Device_ID, Status);
      logger.Debug(Device_ID + " " + Status);
      switch (Status)
      {
        case "Disconnected":
          foreach (Node eachNode in NodeManagement.GetByController(Device_ID))
          {
            eachNode.InitialComplete = false;

            AlarmInfo CurrentAlarm = new AlarmInfo();
            CurrentAlarm.NodeName = eachNode.Name;
            CurrentAlarm.AlarmCode = "00000000";
            CurrentAlarm.SystemAlarmCode = "FF00000000";
            CurrentAlarm.Desc = "設備異常斷線";
            CurrentAlarm.TimeStamp = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss:fff");

            AlarmManagement.Add(CurrentAlarm);
            SetMode("Stop");
          }
          break;
      }
    }



    public void On_Node_State_Changed(Node Node, string Status)
    {

    }

    public void On_Command_Error(Node Node, Transaction Txn, ReturnMessage Msg)
    {
      Node.InitialComplete = false;
      logger.Debug("On_Command_Error");
      AlarmInfo CurrentAlarm = new AlarmInfo();
      CurrentAlarm.NodeName = Node.Name;
      CurrentAlarm.AlarmCode = Msg.Value;
      try
      {

        AlarmMessage Detail = this.Get(Node.Brand, Node.Type, CurrentAlarm.AlarmCode);

        CurrentAlarm.SystemAlarmCode = Detail.CodeID;
        CurrentAlarm.Desc = Detail.Code_Cause;
        CurrentAlarm.EngDesc = Detail.Code_Cause_English;
      }
      catch (Exception e)
      {
        CurrentAlarm.Desc = "未定義";
        logger.Error(Node.Controller + "-" + Node.AdrNo + "(GetAlarmMessage)" + e.Message + "\n" + e.StackTrace);
      }
      CurrentAlarm.TimeStamp = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss:fff");

      AlarmManagement.Add(CurrentAlarm);
      SetMode("Stop");
    }

  }
}
