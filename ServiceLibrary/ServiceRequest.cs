using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceLibrary
{
    public class ServiceRequest
    {
       public string Command { get; set; }
        public string ServiceName { get; set; }
        public string ExecutablePath { get; set; }
        public string Arguments { get; set; }
        public string WorkingDirectory { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string StartType { get; set; }
        public string Account { get; set; }

    }
}
