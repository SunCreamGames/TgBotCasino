using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TgBot0.EntityModels
{
    public class PlayerDataEntityModel : IEntityTypeConfiguration<PlayerChatStatistic>
    {
        public void Configure(EntityTypeBuilder<PlayerChatStatistic> builder)
        {
            builder.HasOne(x => x.Chat).WithMany(chat => chat.PlayerStats).HasForeignKey(x => x.ChatId);
        }
    }
}
