using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TgBot0
{
    public class ChatData
    {
        public long ChatId { get; set; }

        public List<ChatPlayer> TopWinners { get; set; }
        public int WinnerTopEntryBound { get; set; }

        public List<ChatPlayer> TopLosers { get; set; }
        public int LoserTopEntryBound { get; set; }

        public int CasinoBalance { get; set; }

        public List<ChatPlayer> PlayerBalances { get; set; }
    }
}
