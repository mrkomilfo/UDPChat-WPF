using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lab1.Classes
{
    [Serializable]
    class Message
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public int SenderPort { get; set; }
        public bool IsDelivered { get; set; }
        public Type Type{ get; set; }
    }
    public enum Type { 
        Join,
        Leave,
        Message,
        Confirm,
        ServerShutDown
    }
}
