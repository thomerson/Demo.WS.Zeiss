namespace Demo.WS.Zeiss.Model
{
    public class wsMachineInfo
    {
        public string machine_id { get; set; }
        public string id { get; set; }
        public DateTime timestamp { get; set; }
        public MachineStatus status { get; set; }
    }
}
