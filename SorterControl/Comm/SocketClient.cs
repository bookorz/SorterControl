﻿using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SorterControl.Comm
{
    class SocketClient : IConnection
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(SocketClient));
        Socket SckSPort; // 先行宣告Socket

        string RmIp = "192.168.0.127";  // 其中 xxx.xxx.xxx.xxx 為Server端的IP

        int SPort = 23;

        int RDataLen = 100;  //固定長度傳送資料~ 可以針對自己的需要改長度 


        

        IConnectionReport ConnReport;

        public SocketClient(string IP, int Port, IConnectionReport _ConnReport)
        {
            RmIp = IP;
            SPort = Port;
            ConnReport = _ConnReport;


            
        }

        // 連線

        public void Connect()
        {
            if (SckSPort != null)
            {
                if (SckSPort.Connected)
                {
                    SckSPort.Close();
                }
            }
            Thread SckTd = new Thread(ConnectServer);
            SckTd.IsBackground = true;
            SckTd.Start();
        }

        private void ConnectServer()

        {

            try

            {
                ConnReport.On_Connection_Connecting("Connecting to " + RmIp + ":" + SPort);
                SckSPort = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                //SckSPort.ReceiveTimeout = 1000;
                SckSPort.Connect(new IPEndPoint(IPAddress.Parse(RmIp), SPort));
                //SckSPort.IOControl(IOControlCode.KeepAliveValues)
                // RmIp和SPort分別為string和int型態, 前者為Server端的IP, 後者為Server端的Port

                if (!SckSPort.Connected)
                {
                    ConnReport.On_Connection_Error("Connect to " + RmIp + ":" + SPort + " Fail!");
                    //logger.Error("Connect to " + RmIp + ":" + SPort + " Fail!");
                    return;
                }
                else
                {
                    //logger.Info("Connected! " + RmIp + ":" + SPort);
                    ConnReport.On_Connection_Connected("Connected! " + RmIp + ":" + SPort);
                }

                // 同 Server 端一樣要另外開一個執行緒用來等待接收來自 Server 端傳來的資料, 與Server概念同

                Thread SckSReceiveTd = new Thread(SckSReceiveProc);
                SckSReceiveTd.IsBackground = true;
                SckSReceiveTd.Start();

                //Thread CheckAvailableTd = new Thread(CheckAvailable);
                //CheckAvailableTd.IsBackground = true;
                //CheckAvailableTd.Start();

            }
            catch (Exception e)
            {
                //logger.Error("(ConnectServer " + RmIp + ":" + SPort + ")" + e.Message + "\n" + e.StackTrace);
                ConnReport.On_Connection_Error("(ConnectServer " + RmIp + ":" + SPort + ")" + e.Message + "\n" + e.StackTrace);
            }

        }

        //private void CheckAvailable()
        //{
        //    while (true)
        //    {
        //        bool CheckResult = SocketConnected(SckSPort);

        //        if (!CheckResult)
        //        {
        //            ConnReport.On_Connection_Disconnected("(" + RmIp + ":" + SPort + ") is disconnected.");
        //            break;
        //        }
        //        SpinWait.SpinUntil(() => false, 500);
        //    }
        //}

        //bool SocketConnected(Socket s)
        //{
        //    bool part1 = s.Poll(1000, SelectMode.SelectRead);
        //    bool part2 = (s.Available == 0);
        //    if (part1 && part2)
        //        return false;
        //    else
        //        return true;
        //    //try
        //    //{
        //    //    byte[] testByte = new byte[1];
        //    //    //使用Peek測試連線是否仍存在
        //    //    if (s.Connected && s.Poll(0, SelectMode.SelectRead))
        //    //    {
        //    //        return !(s.Receive(testByte, SocketFlags.Peek) == 0);
        //    //    }
        //    //    else
        //    //    {
        //    //        return true;
        //    //    }
        //    //}
        //    //catch (SocketException se)
        //    //{
        //    //    return false;
        //    //}
        //}

        private void SckSReceiveProc()
        {

            try
            {

                int IntAcceptData;

                byte[] clientData = new byte[RDataLen];

                while (true)
                {
                    if (!SckSPort.Connected)
                    {
                        //logger.Error(Desc + " (" + RmIp + ":" + SPort + ") is disconnected.");
                        ConnReport.On_Connection_Disconnected("(" + RmIp + ":" + SPort + ") is disconnected.");
                        break;
                    }
                    // 程式會被 hand 在此, 等待接收來自 Server 端傳來的資料

                    IntAcceptData = SckSPort.Receive(clientData);

                    // 往下就自己寫接收到來自Server端的資料後要做什麼事唄~^^”

                    string S = Encoding.Default.GetString(clientData, 0, IntAcceptData);
                    //Console.WriteLine(S);
                    //logger.Info("[Rev<--]" + S.Replace("\n", "") + "(From " + Desc + " " + RmIp + ":" + SPort + ")");
                    if (!S.Trim().Equals(""))
                    {

                        ThreadPool.QueueUserWorkItem(new WaitCallback(ConnReport.On_Connection_Message), S);
                       
                        //ConnReport.On_Connection_Message(S);
                        
                    }
                }

            }

            catch (Exception e)
            {
                //logger.Error("(From " + RmIp + ":" + SPort + ")" + e.Message + "\n" + e.StackTrace);
                ConnReport.On_Connection_Error("SckSReceiveProc (" + RmIp + ":" + SPort + ")" + e.Message + "\n" + e.StackTrace);
            }
        }
       

        // 當然 Client 端也可以傳送資料給Server端~ 和 Server 端的SckSSend一樣, 只差在Client端只有一個Socket

        public void Send(object Msg)

        {
            try

            {
                //SckSPort.Send(Msg);
                //logger.Info("[Snd-->]" + Msg.Replace("\r", "") + "(To " + Desc + " " + RmIp + ":" + SPort + ")");
                byte[] t = new byte[Encoding.ASCII.GetByteCount(Msg.ToString())]; ;
                int i = Encoding.ASCII.GetBytes(Msg.ToString(), 0, Encoding.ASCII.GetByteCount(Msg.ToString()), t, 0);
                if (SckSPort.Connected == true)
                {
                    
                    SckSPort.Send(t);
                }
            }

            catch (Exception e)
            {
                //logger.Error("(To " + Desc + " " + RmIp + ":" + SPort + ")" + e.Message + "\n" + e.StackTrace);
                ConnReport.On_Connection_Error("Send (" + RmIp + ":" + SPort + ")" + e.Message + "\n" + e.StackTrace);
            }





        }

        public void Close()
        {
            if (SckSPort != null)
            {

                SckSPort.Close();


            }
        }
    }
}
