using Newtonsoft.Json;

namespace Demo.WS.Zeiss.Model
{
    public class wsMachineMessage
    {
        public string topic { get; set; }
        [JsonProperty("ref")]
        public object _ref { get; set; }
        public wsMachineInfo payload { get; set; }
        [JsonProperty("event")]
        public string _event { get; set; }
    }
}