using log4net;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SorterControl.Comm
{
    class ComPortClient : IConnection
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(ComPortClient));
        private SerialPort port;
        IConnectionReport ConnReport;

        public ComPortClient(IConnectionReport _ConnReport)
        {
            ConnReport = _ConnReport;
            port = new SerialPort("COM1", 9600, Parity.None, 8, StopBits.One);
        }
        public void Close()
        {
            port.Close();
        }

        public void Connect()
        {

            Thread ComTd = new Thread(ConnectServer);
            ComTd.IsBackground = true;
            ComTd.Start();
        }

        public void Send(object Message)
        {
            try
            {
       
        //Message = Message.ToString().Replace("\r", "");
        //Message = "MOV:ORGSH;";
        ////chkSum
        //string needSckSumStr = Convert.ToChar(0) + "" + Convert.ToChar(Message.ToString().Length + 4) + "" + Convert.ToChar(48) + "" + Convert.ToChar(48) + Message.ToString();
        //byte[] t = new byte[Encoding.ASCII.GetByteCount(needSckSumStr)]; ;
        //int ttt = Encoding.ASCII.GetBytes(needSckSumStr, 0, Encoding.ASCII.GetByteCount(needSckSumStr), t, 0);
        //byte tt = 0;
        //for (int i = 0; i < t.Length; i++)
        //{
        //  tt += t[i];
        //}
        //string csHex = tt.ToString("X");
        ////chkSum

        //string cmd = Convert.ToChar(1) + "" + Convert.ToChar(0) + "" + Convert.ToChar(Message.ToString().Length + 4) + "" + Convert.ToChar(48) + "" + Convert.ToChar(48) + Message.ToString() + csHex + Convert.ToChar(3);
        port.Write(Message.ToString());
            }
            catch (Exception e)
            {
                //logger.Error("(ConnectServer " + RmIp + ":" + SPort + ")" + e.Message + "\n" + e.StackTrace);
                ConnReport.On_Connection_Error("(ConnectServer )" + e.Message + "\n" + e.StackTrace);
            }
        }

        private void ConnectServer()
        {

            try
            {
                ConnReport.On_Connection_Connecting("Connecting to ");
                port.Open();
        ConnReport.On_Connection_Connected("Connected! ");
        port.DataReceived += new SerialDataReceivedEventHandler(port_DataReceived);
                //Thread ComReceiveTd = new Thread(ComReceiveProc);
                //ComReceiveTd.IsBackground = true;
                //ComReceiveTd.Start();
            }
            catch (Exception e)
            {
                //logger.Error("(ConnectServer " + RmIp + ":" + SPort + ")" + e.Message + "\n" + e.StackTrace);
                ConnReport.On_Connection_Error("(ConnectServer )" + e.Message + "\n" + e.StackTrace);
            }
        }

        //private void ComReceiveProc()
        //{
        //    try
        //    {
        //        while (true)
        //        {
        //            string data = port.ReadExisting();
        //            ThreadPool.QueueUserWorkItem(new WaitCallback(ConnReport.On_Connection_Message), data);
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        //logger.Error("(ConnectServer " + RmIp + ":" + SPort + ")" + e.Message + "\n" + e.StackTrace);
        //        ConnReport.On_Connection_Error("(ConnectServer )" + e.Message + "\n" + e.StackTrace);
        //    }
        //}

        private void port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string data = port.ReadTo(((Char)3).ToString());
                ThreadPool.QueueUserWorkItem(new WaitCallback(ConnReport.On_Connection_Message), data);
            }
            catch (Exception e1)
            {
                //logger.Error("(ConnectServer " + RmIp + ":" + SPort + ")" + e.Message + "\n" + e.StackTrace);
                ConnReport.On_Connection_Error("(ConnectServer )" + e1.Message + "\n" + e1.StackTrace);
            }
        }
    }
}
