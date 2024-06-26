﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TgBot0;

var botToken = Environment.GetEnvironmentVariable("BotToken");
var serviceProvider = ConfigureServices();

static IServiceProvider ConfigureServices()
{
    var connectionString = Environment.GetEnvironmentVariable("postgresConnString");
    var services = new ServiceCollection();
    services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

    return services.BuildServiceProvider();
}
var botClient = new TelegramBotClient(botToken);
using CancellationTokenSource cts = new();

var chatsData = new Dictionary<long, ChatData>();

var fileName = "casino121.txt";


//Dictionary<long, ChatData> LoadSavedData()
//{
//    var json = System.IO.File.ReadAllText(fileName);

//    var data = JsonConvert.DeserializeObject<Dictionary<long, ChatData>>(json);
//    return data;
//}

//void SaveData(Dictionary<long, ChatData> dataForSave)
//{
//    var json = JsonConvert.SerializeObject(dataForSave, Formatting.Indented);

//    System.IO.File.WriteAllText(fileName, json);
//}

chatsData = await LoadData();

async Task<Dictionary<long, ChatData>> LoadData()
{
    var res = new Dictionary<long, ChatData>();
    using (var dbContext = serviceProvider.GetRequiredService<AppDbContext>())
    {
        var list = await dbContext.ChatStats.Include(x => x.PlayerStats).ToListAsync();
        foreach (var chatData in list)
        {
            await RecalulateRatings(chatData);
            res[chatData.ChatId] = chatData;
        }
    }
    return res;
}

var botCommands = await botClient.GetMyCommandsAsync();

ReceiverOptions receiverOptions = new()
{
    AllowedUpdates = Array.Empty<UpdateType>()
};

botClient.StartReceiving(
    updateHandler: HandleUpdateAsync,
    pollingErrorHandler: HandlePollingErrorAsync,
    receiverOptions: receiverOptions,
    cancellationToken: cts.Token
);


StartSaveCron();

async Task StartSaveCron()
{

    while (true)
    {
        await Task.Delay(1000 * 60 * 10); // save every 10 minutes
        await SaveData(chatsData);
    }
}

async Task SaveData(Dictionary<long, ChatData> chatsData)
{
    using (var dbContext = serviceProvider.GetRequiredService<AppDbContext>())
    {
        foreach (var chatData in chatsData)
        {
            var oldChatData = await dbContext.ChatStats.FindAsync(chatData.Key);
            if (oldChatData == null)
                dbContext.ChatStats.Add(chatData.Value);
            else
                oldChatData = chatData.Value;

            foreach (var playerData in chatData.Value.PlayerStats)
            {
                var oldPlayerData = await dbContext.ChatPlayerStats.FindAsync(playerData.UserId);

                if (oldPlayerData == null)
                    dbContext.Add(playerData);
                else
                    oldPlayerData = playerData;
            }
        }
    }
}

Console.ReadLine();
cts.Cancel();


async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    if (update.Message is not { } message)
        return;

    if (update.Message.ForwardFrom != null)
        return;

    var chatId = message.Chat.Id;

    if (message.Chat.Type != ChatType.Group && message.Chat.Type != ChatType.Supergroup)
        return;

    if (!chatsData.TryGetValue(chatId, out var chatData))
        chatData = new ChatData
        {
            CasinoBalance = 0,
            ChatId = chatId,
            TopLosers = [],
            TopWinners = [],
            LoserTopEntryBound = int.MaxValue,
            WinnerTopEntryBound = int.MinValue,
            PlayerStats = [],
        };

    chatsData[chatId] = chatData;

    bool commandApplied = await TryApplyCommand(message, chatData, botClient, cancellationToken);
    if (commandApplied)
        return;


    //Console.WriteLine($"Received a '{messageText}' message in chat {chatId}.");
    if (message.Dice == null)
        return;
    if (message.Dice.Emoji != "🎰")
        return;

    await TryApplyCasinoSpin(message, chatData, botClient, cancellationToken);
}
Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
{
    var ErrorMessage = exception switch
    {
        ApiRequestException apiRequestException
            => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
        _ => exception.ToString()
    };

    Console.WriteLine(ErrorMessage);
    return Task.CompletedTask;
}



async Task TryApplyCasinoSpin(Message message, ChatData chatData, ITelegramBotClient botClient, CancellationToken cancellationToken)
{

    var playerId = message.From.Id;

    if (!chatData.PlayerStats.Any(x => x.UserId == playerId))
        chatData.PlayerStats.Add(new PlayerChatStatistic(playerId, chatData.ChatId));

    var playerData = chatData.PlayerStats.FirstOrDefault(x => x.UserId == playerId);
    if (playerData == null)
        throw new Exception("Player is absent in chat data record");

    var winPoints = -1;
    switch (message.Dice.Value)
    {
        case 64: // 777
            winPoints += 25;
            break;
        case 1:  // bar 
            winPoints += 20;
            break;
        case 43: // lemons
            winPoints += 10;
            break;
        case 22: // grapes
            winPoints += 5;
            break;
        default:
            break;
    }
    if (winPoints > 0)
    {
        await Task.Delay(1500);

        playerData.TotalScore += winPoints;
        playerData.SpinsWon++;
        playerData.ScoreWon += winPoints;
        Message m = await botClient.SendTextMessageAsync(chatId: chatData.ChatId,
                                                         text: $"Hey hey hey, we have a winner here. You have won {winPoints} nihuya. Now you have {playerData.TotalScore} nihuya",
                                                         replyToMessageId: message.MessageId,
                                                         cancellationToken: cancellationToken);
    }
    else
    {
        playerData.TotalScore += winPoints;
        playerData.SpinsLost++;
    }

    await TopsUpdate(chatData, playerId);
}

async Task TopsUpdate(ChatData chatData, long playerId)
{
    var topsReworked = await Top10Update(chatData, playerId);
    if (topsReworked)
        return;

    await AntiTop10Update(chatData, playerId);
}

async Task AntiTop10Update(ChatData chatData, long playerId)
{
    async Task<bool> Top10Update(ChatData chatData, long playerId)
    {
        var playerData = chatData.PlayerStats.FirstOrDefault(x => x.UserId == playerId);

        if (playerData == null)
            throw new Exception("Player is absent in chat data record");

        var playerIsInAntiTopAlready = chatData.TopLosers.Any(x => x.UserId == playerId);

        if (!playerIsInAntiTopAlready)
        {
            // player is not in anti-top, but should get into it after this spin
            if (chatData.LoserTopEntryBound >= playerData.TotalScore)
            {
                await RecalulateRatings(chatData);
                return true;
            }
        }

        // player jumped over antitop-10 scoreline and probably should left antitop-10
        if (chatData.LoserTopEntryBound <= playerData.TotalScore)
        {
            await RecalulateRatings(chatData);
            return true;
        }


        // resort just top-10 plyers in case they changed some positions
        chatData.TopLosers = chatData.TopWinners.OrderBy(x => x.TotalScore).ToList();
        return false;
    }
}

async Task<bool> Top10Update(ChatData chatData, long playerId)
{
    var playerData = chatData.PlayerStats.FirstOrDefault(x => x.UserId == playerId);

    if (playerData == null)
        throw new Exception("Player is absent in chat data record");

    var playerIsInTopAlready = chatData.TopWinners.Any(x => x.UserId == playerId);

    if (!playerIsInTopAlready)
    {
        // player is not in top, but should get into it after this spin
        if (chatData.WinnerTopEntryBound <= playerData.TotalScore)
        {
            await RecalulateRatings(chatData);
            return true;
        }

        // player is not in top, but his spin didn't affect this
        return false;
    }

    // player dropped below top-10 scoreline and probably should left top-10
    if (chatData.WinnerTopEntryBound >= playerData.TotalScore)
    {
        await RecalulateRatings(chatData);
        return true;
    }


    // resort just top-10 plyers in case they changed some positions
    chatData.TopWinners = chatData.TopWinners.OrderByDescending(x => x.TotalScore).ToList();
    return false;
}

async Task RecalulateRatings(ChatData chatData)
{
    chatData.PlayerStats = chatData.PlayerStats.OrderByDescending(x => x.TotalScore).ToList();
    chatData.TopWinners = chatData.PlayerStats.Take(10).ToList();
    chatData.TopLosers = chatData.PlayerStats.TakeLast(10).ToList();
    if (chatData.PlayerStats.Count == 0)
        return;

    chatData.LoserTopEntryBound = chatData.TopLosers.LastOrDefault().TotalScore;
    chatData.WinnerTopEntryBound = chatData.TopWinners.LastOrDefault().TotalScore;
}

async Task<bool> TryApplyCommand(Message message, ChatData chatData, ITelegramBotClient botClient, CancellationToken cancellationToken)
{
    if (!botCommands.Any(x => x.Command == message.Text))
        return false;


    if (message.Text == "/top_10")
    {
        var res = "";
        for (int i = 0; i < chatData.TopWinners.Count; i++)
        {
            var user = await botClient.GetChatMemberAsync(chatData.ChatId, chatData.TopWinners[i].UserId);
            res = res + $"{i + 1} : {user.User.FirstName} {user.User.LastName} — {chatData.TopWinners[i].TotalScore}\r\n";
        }
        await botClient.SendTextMessageAsync(chatId: chatData.ChatId,
                                                   text: res,
                                                   replyToMessageId: message.MessageId,
                                                   cancellationToken: cancellationToken);
        return true;
    }

    if (message.Text == "/antitop_10")
    {
        var res = "";
        for (int i = 0; i < chatData.TopLosers.Count; i++)
        {
            var user = await botClient.GetChatMemberAsync(chatData.ChatId, chatData.TopLosers[i].UserId);
            res = res + $"{i + 1} : {user.User.FirstName} {user.User.LastName} — {chatData.TopLosers[i].TotalScore}\r\n";
        }
        await botClient.SendTextMessageAsync(chatId: chatData.ChatId,
                                                   text: res,
                                                   replyToMessageId: message.MessageId,
                                                   cancellationToken: cancellationToken);
        return true;
    }

    if (message.Text == "/get_balance")
    {
        var player = chatData.PlayerStats.FirstOrDefault(x => x.UserId == message.From.Id);
        if (player == null)
        {
            await botClient.SendTextMessageAsync(chatId: chatData.ChatId,
                                                           text: $"Spin at list 1 slot first, you are not in SSU ludomans database",
                                                           replyToMessageId: message.MessageId,
                                                           cancellationToken: cancellationToken);
            return true;
        }
        await botClient.SendTextMessageAsync(chatId: chatData.ChatId,
                                                                text: $"Your balance is {player.TotalScore}",
                                                                replyToMessageId: message.MessageId,
                                                                cancellationToken: cancellationToken);
        return true;
    }

    return false;
}