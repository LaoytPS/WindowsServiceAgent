using System.ComponentModel;

namespace WindowsServiceAgentManager
{
    public class ServiceInfo: INotifyPropertyChanged
    {
        public string ServiceName { get; set; }
        public string DisplayName { get; set; }
        
        private string status;
        public string Status
        {
            get { return status; }
            set { status = value; OnPropertyChanged(nameof(Status)); }
        }
        
        private int? pid;
        public int? PID
        {
            get { return pid; }
            set { pid = value; OnPropertyChanged(nameof(PID)); }
        }
        
        private string ports;
        public string Ports
        {
            get { return ports; }
            set { ports = value; OnPropertyChanged(nameof(Ports)); }
        }
        
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
