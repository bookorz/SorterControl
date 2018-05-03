using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SorterControl.Management
{
    public class Job
    {
        public string Slot { get; set; }
        public string Job_Id { get; set; }
        public bool ProcessFlag { get; set; }
        public int Piority { get; set; }
        public bool AlignerFlag { get; set; }
        public bool OCRFlag { get; set; }
        public string Position { get; set; }  
        public string FromPort { get; set; }
        public string Destination { get; set; }
        public string DestinationSlot { get; set; }
        public string LastNode { get; set; }
        public State CurrentState { get; set; }

        public enum State
        {
            WAIT_PUT,     
            WAIT_WHLD,
            WAIT_ALIGN,
            WAIT_OCR,
            WAIT_WRLS,
            WAIT_GET,
            WAIT_RET,
            WAIT_UNLOAD
        }
    }
}
