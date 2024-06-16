namespace TgBot0
{
    public class PlayerChatStatistic
    {
        public PlayerChatStatistic(long userId, long chatId)
        {
            UserId = userId;
            ChatId = chatId;
            SpinsLost = SpinsWon = ScoreWon = TotalScore = 0;
        }
        public long UserId { get; set; }
        public long ChatId { get; set; }
        public int SpinsLost { get; set; }
        public int SpinsWon { get; set; }
        public int ScoreWon { get; set; }
        public int TotalScore { get; set; }

        public virtual ChatData Chat { get; set; }
    }
}
