using SorterControl.Type;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SorterControl.Management
{
    public class Transaction
    {

        public List<Job> TargetJobs { get; set; }
        public string AdrNo { get; set; }
        public string NodeType { get; set; }
        public string Position { get; set; }
        public string Point { get; set; }
        public string Position2 { get; set; }
        public string Point2 { get; set; }
        public string Slot { get; set; }
        public string Slot2 { get; set; }
        public string Method { get; set; }
        public string Arm { get; set; }
        public string Arm2 { get; set; }
        public string Angle { get; set; }
        public string Value { get; set; }
        public string CommandType { get; set; }
        public string CommandEncodeStr { get; set; }
        public bool IsInterrupt { get; set; }
        public int Piority { get; set; }



        //逾時
        private System.Timers.Timer timeOutTimer = new System.Timers.Timer();
        ITransactionReport TimeOutReport;

        public class Command
        {
            //LoadPort
            public class LoadPortType
            {
                public const string Load = "Load";
        public const string Mapping = "Mapping";
                public const string MappingLoad = "MappingLoad";
                public const string Unload = "Unload";
                public const string MappingUnload = "MappingUnload";
                public const string GetMapping = "GetMapping";
                public const string GetLED = "GetLED";
                public const string GetStatus = "GetStatus";
                public const string Reset = "Reset";
                public const string InitialPos = "InitialPos";
        public const string GetCount = "GetCount";
      }
            //Robot
            public class RobotType
            {
                public const string Get = "Get";
                public const string DoubleGet = "DoubleGet";
                public const string WaitBeforeGet = "WaitBeforeGet";
                public const string GetAfterWait = "GetAfterWait";
                public const string Put = "Put";
                public const string PutWithoutBack = "PutWithoutBack";
                public const string PutBack = "PutBack";
                public const string DoublePut = "DoublePut";
                public const string GetWait = "GetWait";
                public const string PutWait = "PutWait";
                public const string WaferHold = "WaferHold";
                public const string WaferRelease = "WaferRelease";
                public const string RobotHome = "RobotHome";
                public const string RobotServo = "RobotServo";
                public const string RobotMode = "RobotMode";
                public const string RobotWaferRelease = "RobotWaferRelease";
                public const string RobotSpeed = "RobotSpeed";
                public const string Reset = "Reset";
                public const string GetStatus = "GetStatus";
            }
            //Aligner
            public class AlignerType
            {
                public const string Align = "Align";
                public const string WaferHold = "WaferHold";
                public const string WaferRelease = "WaferRelease";
                public const string Retract = "Retract";
                public const string AlignerMode = "AlignerMode";
                public const string AlignerSpeed = "AlignerSpeed";
                public const string AlignerOrigin = "AlignerOrigin";
                public const string AlignerServo = "AlignerServo";
                public const string AlignerHome = "AlignerHome";
                public const string GetStatus = "GetStatus";
                public const string Reset = "Reset";
            }
            //OCR
            public class OCRType
            {
                public const string OCR = "OCR";
            }
        }

        public Transaction(List<Job> _TargetJobs, string _Position, string _Slot, string _Method, string _Arm, int Timeout)
        {
            Position = _Position;
            Slot = _Slot;
            Method = _Method;
            Arm = _Arm;
            Position2 = "";
            Slot2 = "";
            Arm2 = "";
            Angle = "";

            TargetJobs = _TargetJobs;

            timeOutTimer.Enabled = false;

            timeOutTimer.Interval = Timeout;

            timeOutTimer.Elapsed += new System.Timers.ElapsedEventHandler(TimeOutMonitor);

        }

        public void SetTimeOut(int Timeout)
        {
            timeOutTimer.Interval = Timeout;
        }

        public void SetTimeOutMonitor(bool Enabled)
        {
            if (Enabled)
            {
                timeOutTimer.Start();
            }
            else
            {
                timeOutTimer.Stop();
            }

        }

        public void SetTimeOutReport(ITransactionReport _TimeOutReport)
        {
            TimeOutReport = _TimeOutReport;
        }

        private void TimeOutMonitor(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (TimeOutReport != null)
            {
                TimeOutReport.On_Transaction_TimeOut(this);
            }
        }
    }
}
