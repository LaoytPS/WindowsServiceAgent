using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceManager
{
    public class ServiceConfig
    {
        public string ServiceName { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string ExecutablePath { get; set; }
        public string Arguments { get; set; }
        public string WorkingDirectory { get; set; }
        public string StartType { get; set; }
        public string Account { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }

}
