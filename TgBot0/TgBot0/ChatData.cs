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

        public List<PlayerChatStatistic> TopWinners { get; set; }
        public int WinnerTopEntryBound { get; set; }

        public List<PlayerChatStatistic> TopLosers { get; set; }
        public int LoserTopEntryBound { get; set; }

        public int CasinoBalance { get; set; }

        public virtual List<PlayerChatStatistic> PlayerStats { get; set; }
    }
}
