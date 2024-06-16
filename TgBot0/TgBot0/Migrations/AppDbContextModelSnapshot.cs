﻿// <auto-generated />
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using TgBot0;

#nullable disable

namespace TgBot0.Migrations
{
    [DbContext(typeof(AppDbContext))]
    partial class AppDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.6")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("TgBot0.ChatData", b =>
                {
                    b.Property<long>("ChatId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("ChatId"));

                    b.Property<int>("CasinoBalance")
                        .HasColumnType("integer");

                    b.HasKey("ChatId");

                    b.ToTable("ChatStats");
                });

            modelBuilder.Entity("TgBot0.PlayerChatStatistic", b =>
                {
                    b.Property<long>("ChatId")
                        .HasColumnType("bigint");

                    b.Property<long>("UserId")
                        .HasColumnType("bigint");

                    b.Property<int>("ScoreWon")
                        .HasColumnType("integer");

                    b.Property<int>("SpinsLost")
                        .HasColumnType("integer");

                    b.Property<int>("SpinsWon")
                        .HasColumnType("integer");

                    b.Property<int>("TotalScore")
                        .HasColumnType("integer");

                    b.HasKey("ChatId", "UserId");

                    b.ToTable("ChatPlayerStats");
                });

            modelBuilder.Entity("TgBot0.PlayerChatStatistic", b =>
                {
                    b.HasOne("TgBot0.ChatData", "Chat")
                        .WithMany("PlayerStats")
                        .HasForeignKey("ChatId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Chat");
                });

            modelBuilder.Entity("TgBot0.ChatData", b =>
                {
                    b.Navigation("PlayerStats");
                });
#pragma warning restore 612, 618
        }
    }
}
