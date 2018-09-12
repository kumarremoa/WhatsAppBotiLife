using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebWhatsappAPI
{
    public class Id
    {
        public bool fromMe { get; set; }
        public string remote { get; set; }
        public string id { get; set; }
        public string _serialized { get; set; }
    }


    public partial class Msg
    {
       
        public Id id { get; set; }
        public string body { get; set; }
        public string type { get; set; }
        public int t { get; set; }
        public string notifyName { get; set; }
        public string from { get; set; }
        public string to { get; set; }
        public string self { get; set; }
        public int ack { get; set; }
        public bool invis { get; set; }
        public bool isNewMsg { get; set; }
        public bool star { get; set; }
        public bool recvFresh { get; set; }
        public bool broadcast { get; set; }
        public List<object> labels { get; set; }
    }

}
