using log4net;
using SANWA.Utility;
using SorterControl.Type;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SorterControl.Management
{
    public class Node
    {

        SANWA.Utility.Encoder Encoder;


        ILog logger = LogManager.GetLogger(typeof(Node));
        public string Name { get; set; }
        public string Controller { get; set; }
        public string AdrNo { get; set; }
        public string Type { get; set; }
        public string Brand { get; set; }
        public string Phase { get; set; }
        public string CurrentLoadPort { get; set; }
        public string LockByNode { get; set; }
        public string CurrentPosition { get; set; }
        public string CurrentWaitNode { get; set; }
        public int WaitForCarryCount { get; set; }
        public bool PreReady = false;
        public bool Reserve = false;
        public bool Prepare = false;
        public bool InitialComplete { get; set; }


        public ConcurrentDictionary<string, Job> JobList { get; set; }
        public List<Route> RouteTable { get; set; }
        public List<TransferRequest> TransferQueue { get; set; }


        public class Route
        {
            public string NodeName { get; set; }
            public string NodeType { get; set; }
            public string Point { get; set; }
        }

        public Node()
        {
            JobList = new ConcurrentDictionary<string, Job>();
            Phase = "1";
            CurrentLoadPort = "";
            LockByNode = "";
            CurrentWaitNode = "";
            CurrentPosition = "";


            TransferQueue = new List<TransferRequest>();
        }

        public void Initial()
        {
            JobList = new ConcurrentDictionary<string, Job>();
            Phase = "1";
            CurrentLoadPort = "";
            LockByNode = "";
            CurrentWaitNode = "";
            CurrentPosition = "";
            WaitForCarryCount = 0;
            PreReady = false;
            Prepare = false;
        }

        public bool SendCommand(Transaction txn)
        {
            //var watch = System.Diagnostics.Stopwatch.StartNew();


            bool result = false;
            try
            {
                if (Encoder == null)
                {
                    Encoder = new SANWA.Utility.Encoder(Brand);

                }
                txn.AdrNo = AdrNo;
                txn.NodeType = Type;
                foreach (Route each in RouteTable)
                {
                    if (txn.Position.Equals(each.NodeName))
                    {
                        txn.Point = each.Point;
                        break;
                    }
                }
                switch (this.Type)
                {
                    case "LoadPort":
                        switch (txn.Method)
                        {
                            case Transaction.Command.LoadPortType.GetLED:
                                txn.CommandEncodeStr = Encoder.LoadPort.LEDIndicatorStatus();
                                break;
                            case Transaction.Command.LoadPortType.GetMapping:
                                txn.CommandEncodeStr = Encoder.LoadPort.WaferSorting(EncoderLoadPort.MappingSortingType.Asc);
                                break;
                            case Transaction.Command.LoadPortType.GetStatus:
                                txn.CommandEncodeStr = Encoder.LoadPort.Status();
                                break;
                            case Transaction.Command.LoadPortType.InitialPos:
                                txn.CommandEncodeStr = Encoder.LoadPort.InitialPosition(EncoderLoadPort.CommandType.Normal);
                                break;
                            case Transaction.Command.LoadPortType.Load:
                                txn.CommandEncodeStr = Encoder.LoadPort.Load(EncoderLoadPort.CommandType.Normal);
                                break;
                            case Transaction.Command.LoadPortType.MappingLoad:
                                txn.CommandEncodeStr = Encoder.LoadPort.MappingLoad(EncoderLoadPort.CommandType.Normal);
                                break;
                            case Transaction.Command.LoadPortType.MappingUnload:
                                txn.CommandEncodeStr = Encoder.LoadPort.MapAndUnload( EncoderLoadPort.CommandType.Normal);
                                break;
                            case Transaction.Command.LoadPortType.Reset:
                                txn.CommandEncodeStr = Encoder.LoadPort.Reset( EncoderLoadPort.CommandType.Normal);
                                break;
                            case Transaction.Command.LoadPortType.Unload:
                                txn.CommandEncodeStr = Encoder.LoadPort.Unload( EncoderLoadPort.CommandType.Normal);
                                break;
              case Transaction.Command.LoadPortType.Mapping:
                txn.CommandEncodeStr = Encoder.LoadPort.MappingInLoad(EncoderLoadPort.CommandType.Normal);
                break;
              case Transaction.Command.LoadPortType.GetCount:
                txn.CommandEncodeStr = Encoder.LoadPort.WaferQuantity();
                break;
            }
                        break;
                    case "Robot":
                        switch (txn.Method)
                        {

                            case Transaction.Command.RobotType.GetStatus:
                                txn.CommandEncodeStr = Encoder.Robot.Status(AdrNo, "");
                                break;
                            case Transaction.Command.RobotType.Get:
                                txn.CommandEncodeStr = Encoder.Robot.GetWafer(AdrNo, "", txn.Arm, txn.Point, "0", txn.Slot);
                                break;
                            case Transaction.Command.RobotType.Put:
                                txn.CommandEncodeStr = Encoder.Robot.PutWafer(AdrNo, "", txn.Arm, txn.Point, txn.Slot);
                                break;
                            case Transaction.Command.RobotType.DoubleGet:
                                txn.CommandEncodeStr = Encoder.Robot.GetWafer(AdrNo, "", "3", txn.Point, "0", txn.Slot);
                                break;
                            case Transaction.Command.RobotType.DoublePut:
                                txn.CommandEncodeStr = Encoder.Robot.PutWafer(AdrNo, "", "3", txn.Point, txn.Slot);
                                break;
                            case Transaction.Command.RobotType.WaitBeforeGet:
                                txn.CommandEncodeStr = Encoder.Robot.GetWaferToStandBy(AdrNo, "", txn.Arm, txn.Point, "0", txn.Slot);
                                break;
                            case Transaction.Command.RobotType.GetAfterWait:
                                txn.CommandEncodeStr = Encoder.Robot.GetWaferToContinue(AdrNo, "", txn.Arm, txn.Point, "0", txn.Slot);
                                break;
                            case Transaction.Command.RobotType.PutWithoutBack:
                                txn.CommandEncodeStr = Encoder.Robot.PutWaferToDown(AdrNo, "", txn.Arm, txn.Point, txn.Slot);
                                break;
                            case Transaction.Command.RobotType.PutBack:
                                txn.CommandEncodeStr = Encoder.Robot.PutWaferToContinue(AdrNo, "", txn.Arm, txn.Point, txn.Slot);
                                break;
                            case Transaction.Command.RobotType.GetWait:
                                txn.CommandEncodeStr = Encoder.Robot.GetWaferToReady(AdrNo, "", txn.Arm, txn.Point, txn.Slot);
                                break;
                            case Transaction.Command.RobotType.PutWait:
                                txn.CommandEncodeStr = Encoder.Robot.PutWaferToReady(AdrNo, "", txn.Arm, txn.Point, txn.Slot);
                                break;
                            case Transaction.Command.RobotType.RobotHome:
                                txn.CommandEncodeStr = Encoder.Robot.Home(AdrNo, "");
                                break;
                            case Transaction.Command.RobotType.WaferRelease:
                                txn.CommandEncodeStr = Encoder.Aligner.WaferReleaseHold(AdrNo, "", txn.Arm);
                                break;
                            case Transaction.Command.RobotType.WaferHold:
                                txn.CommandEncodeStr = Encoder.Aligner.WaferHold(AdrNo, "");
                                break;
                            case Transaction.Command.RobotType.RobotServo:
                                txn.CommandEncodeStr = Encoder.Robot.ServoOn(AdrNo, "", txn.Arm);
                                break;
                            case Transaction.Command.RobotType.RobotMode:
                                txn.CommandEncodeStr = Encoder.Robot.Mode(AdrNo, "", txn.Arm);
                                break;
                            case Transaction.Command.RobotType.Reset:
                                txn.CommandEncodeStr = Encoder.Robot.ErrorReset(AdrNo, "");
                                break;
                            case Transaction.Command.RobotType.RobotSpeed:
                                txn.CommandEncodeStr = Encoder.Robot.setSpeed(AdrNo, "", txn.Arm);
                                break;
                        }
                        break;
                    case "Aligner":
                        switch (txn.Method)
                        {
                            case Transaction.Command.AlignerType.AlignerHome:
                                txn.CommandEncodeStr = Encoder.Aligner.Home(AdrNo, "");
                                break;
                            case Transaction.Command.AlignerType.Align:
                                txn.CommandEncodeStr = Encoder.Aligner.Align(AdrNo, "", txn.Angle);
                                break;
                            case Transaction.Command.AlignerType.Retract:
                                txn.CommandEncodeStr = Encoder.Aligner.Retract(AdrNo, "");
                                break;
                            case Transaction.Command.AlignerType.WaferRelease:
                                txn.CommandEncodeStr = Encoder.Aligner.WaferReleaseHold(AdrNo, "", txn.Arm);
                                break;
                            case Transaction.Command.AlignerType.WaferHold:
                                txn.CommandEncodeStr = Encoder.Aligner.WaferHold(AdrNo, "");
                                break;
                            case Transaction.Command.AlignerType.AlignerServo:
                                txn.CommandEncodeStr = Encoder.Aligner.ServoOn(AdrNo, "", txn.Arm);
                                break;
                            case Transaction.Command.AlignerType.AlignerMode:
                                txn.CommandEncodeStr = Encoder.Aligner.Mode(AdrNo, "", txn.Arm);
                                break;
                            case Transaction.Command.AlignerType.AlignerOrigin:
                                txn.CommandEncodeStr = Encoder.Aligner.OrginSearch(AdrNo, "");
                                break;
                            case Transaction.Command.AlignerType.Reset:
                                txn.CommandEncodeStr = Encoder.Aligner.ErrorReset(AdrNo, "");
                                break;
                            case Transaction.Command.AlignerType.AlignerSpeed:
                                txn.CommandEncodeStr = Encoder.Aligner.setSpeed(AdrNo, "", txn.Arm);
                                break;

                        }
                        break;
                }


                if (txn.CommandEncodeStr == null)
                {
                    string tt = "";
                }
                if (!txn.CommandEncodeStr.Equals(""))
                {
                    if (txn.CommandEncodeStr.IndexOf("CMD:") != -1 || txn.CommandEncodeStr.IndexOf("MOV:") != -1)
                    {
                        txn.CommandType = "CMD";
                    }
                    else if (txn.CommandEncodeStr.IndexOf("GET:") != -1)
                    {
                        txn.CommandType = "GET";
                    }
                    else if (txn.CommandEncodeStr.IndexOf("SET:") != -1)
                    {
                        txn.CommandType = "SET";
                    }
                }

                if (ControllerManagement.Get(Controller).DoWork(txn))
                {
                    result = true;
                }
                else
                {
                    logger.Debug("SendCommand fail.");
                    result = false;
                }


            }
            catch (Exception e)
            {
                logger.Error("SendCommand " + e.Message + "\n" + e.StackTrace);
            }
            //watch.Stop();
            //var elapsedMs = watch.ElapsedMilliseconds;
            //logger.Info("SendCommand ProcessTime:"+ elapsedMs.ToString());
            return result;
        }

        public Job GetJob(string Slot)
        {
            Job result = null;

            //lock (JobList)
            //{
            JobList.TryGetValue(Slot, out result);
            //}

            return result;
        }
        public bool AddJob(string Slot, Job Job)
        {
            bool result = false;
            //lock (JobList)
            //{
            if (!JobList.ContainsKey(Slot))
            {
                JobList.TryAdd(Slot, Job);
                result = true;
            }
            //}
            return result;
        }





    }
}
