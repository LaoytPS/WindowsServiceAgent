using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceLibrary
{
    public class ServiceStatus
    {
        public string ServiceName { get; set; }
        public bool IsRunning { get; set; }
        public int PID { get; set; }
        public List<int> Ports { get; set; }

    }
}
