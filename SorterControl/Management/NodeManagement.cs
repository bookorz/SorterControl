using SorterControl.UI.JobState;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SorterControl.Management
{
    public static class NodeManagement
    {
        private static ConcurrentDictionary<string, Node> NodeList = new ConcurrentDictionary<string, Node>();
        private static ConcurrentDictionary<string, Node> NodeListByCtrl = new ConcurrentDictionary<string, Node>();

        public static void InitialNodes()
        {
            foreach (Node each in NodeList.Values.ToList())
            {
                each.CurrentLoadPort = "";
                each.CurrentPosition = "";
                each.CurrentWaitNode = "";
                //each.InitialComplete = false;
                each.JobList.Clear();
                each.LockByNode = "";
                each.Phase = "";
                each.Prepare = false;
                each.TransferQueue.Clear();

            }
        }

        public static void UpdatePortToUI(Job job)
        {
            foreach (Node each in NodeList.Values.ToList())
            {
                if(!job.Position.Equals(each.Name) && !job.LastNode.Equals(each.Name))
                {
                    continue;
                }
                if (each.Type.Equals("LoadPort"))
                {
                    List<Job> tmp = new List<Job>();
                    for(int i = 0; i < 25; i++)
                    {
                        if (each.JobList.ContainsKey((i + 1).ToString()))
                        {
                            Job j;
                            if(each.JobList.TryGetValue((i + 1).ToString(), out j))
                            {
                                tmp.Add(j);
                            }

                        }
                        else
                        {
                            Job j = new Job();
                            j.Slot = (i + 1).ToString();
                            tmp.Add(j);
                        }
                    }

                    JobStateUpdate.UpdatePort(each.Name, tmp);

                }else if(each.Type.Equals("Aligner") || each.Type.Equals("Robot"))
                {
                    JobStateUpdate.UpdatePort(each.Name, each.JobList.Values.ToList());
                }
            }
        }

        public static void UpdatePortToUI()
        {
            foreach (Node each in NodeList.Values.ToList())
            {
                
                if (each.Type.Equals("LoadPort"))
                {
                    List<Job> tmp = new List<Job>();
                    for (int i = 0; i < 25; i++)
                    {
                        if (each.JobList.ContainsKey((i + 1).ToString()))
                        {
                            Job j;
                            if (each.JobList.TryGetValue((i + 1).ToString(), out j))
                            {
                                tmp.Add(j);
                            }

                        }
                        else
                        {
                            Job j = new Job();
                            j.Slot = (i + 1).ToString();
                            tmp.Add(j);
                        }
                    }

                    JobStateUpdate.UpdatePort(each.Name, tmp);

                }
                else if (each.Type.Equals("Aligner") || each.Type.Equals("Robot"))
                {
                    JobStateUpdate.UpdatePort(each.Name, each.JobList.Values.ToList());
                }
            }
        }

        public static List<Node> GetList()
        {
            List<Node> result = NodeList.Values.ToList();

            result.Sort((x, y) => { return y.Name.CompareTo(x.Name); });

            return result;
        }

        public static Node Get(string Name)
        {
            Node result = null;

            NodeList.TryGetValue(Name, out result);
            
            return result;
        }

        public static Node GetRobotByPosition(string Position ,string filtName)
        {
            Node result = null;

            foreach(Node each in NodeList.Values.ToList())
            {
                
                if (each.CurrentPosition.Equals(Position) && !each.Name.Equals(filtName) && each.Type.Equals("Robot"))
                {
                    result = each;
                }
            }

            return result;
        }

        public static Node GetByController(string DeviceName, string NodeAdr)
        {
            Node result = null;

            NodeListByCtrl.TryGetValue(DeviceName + NodeAdr, out result);

            return result;
        }

        public static List<Node> GetByController(string DeviceName)
        {
            List<Node> result = null;

            
                var find = from node in NodeListByCtrl.Values.ToList()
                           where node.Controller.Equals(DeviceName)
                           select node;
            result = find.ToList();
            return result;
        }

        public static Node GetNextRobot(Node ProcessNode, Job Job)
        {
            Node result = null;

            foreach (Node.Route eachRt in ProcessNode.RouteTable)
            {
                Node tmp;
                if (eachRt.NodeType.Equals("Robot"))
                {
                    if (NodeList.TryGetValue(eachRt.NodeName, out tmp))
                    {
                        foreach (Node.Route eachtmpRt in tmp.RouteTable)
                        {
                            if (Job.Destination.Equals(eachtmpRt.NodeName))//尋找能搬送到目的地的Robot
                            {
                                result = tmp;
                            }
                        }
                    }
                }
            }

            return result;
        }

        public static Node GetNextRobot(string Destination)
        {
            Node result = null;

            Node Dest = Get(Destination);
            if(Dest == null)
            {
                return null;
            }

            foreach (Node.Route eachRt in Dest.RouteTable)
            {
                
                if (eachRt.NodeType.Equals("Robot"))
                {
                    if (NodeList.TryGetValue(eachRt.NodeName, out result))
                    {
                        break;
                    }
                }
            }

            return result;
        }

        public static Node GetOCRByAligner(Node Aligner)
        {
            Node result = null;

            foreach (Node.Route eachRt in Aligner.RouteTable)
            {
                if (eachRt.NodeType.Equals("OCR"))
                {
                    if (NodeList.TryGetValue(eachRt.NodeName, out result))
                    {
                        break;
                    }
                }
            }
            return result;
        }

        public static Node GetAlignerByOCR(Node OCR)
        {
            Node result = null;

            foreach (Node.Route eachRt in OCR.RouteTable)
            {
                if (eachRt.NodeType.Equals("Aligner"))
                {
                    if (NodeList.TryGetValue(eachRt.NodeName, out result))
                    {
                        break;
                    }
                }
            }
            return result;
        }

        public static Node GetNotReservAligner()
        {
            Node result = null;
           
            foreach (Node each in NodeList.Values.ToList())
            {
                if (each.Type.Equals("Aligner") && each.LockByNode.Equals(""))
                {
                    result = each;
                }
            }
           
            return result;
        }

        public static Node GetReservAligner(string FromPort)
        {
            Node result = null;
            Node alternative = null;
            foreach (Node each in NodeList.Values.ToList())
            {
                if (each.Type.Equals("Aligner") && each.LockByNode.Equals(FromPort))//優先尋找預約的
                {
                    result = each;
                }
                else if (each.Type.Equals("Aligner") && each.LockByNode.Equals(""))//同時尋找沒有被預約的替代目標
                {
                    alternative = each;
                }
            }
            if (result == null)
            {
                result = alternative;
            }
            return result;
        }

        public static bool Add(string Name, Node Node)
        {
            bool result = false;


            if (!NodeList.ContainsKey(Name))
            {
                NodeList.TryAdd(Name, Node);
                NodeListByCtrl.TryAdd(Node.Controller + Node.AdrNo, Node);
                result = true;
            }



            return result;
        }

    }
}
