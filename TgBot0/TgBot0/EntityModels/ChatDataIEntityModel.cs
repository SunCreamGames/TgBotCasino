using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TgBot0.EntityModels
{
    public class ChatDataIEntityModel : IEntityTypeConfiguration<ChatData>
    {
        public void Configure(EntityTypeBuilder<ChatData> builder)
        {
            builder.Ignore(x => x.TopWinners);
            builder.Ignore(x => x.TopLosers);
            builder.Ignore(x => x.LoserTopEntryBound);
            builder.Ignore(x => x.WinnerTopEntryBound);
        }
    }
}
