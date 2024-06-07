using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TgBot0
{
    public class Player
    {
        public long UserId { get; set; }
        public Dictionary<long, int> ChatsScores { get; set; }
    }
}
