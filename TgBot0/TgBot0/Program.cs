using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

var botToken = Environment.GetEnvironmentVariable("BotToken");
var botClient = new TelegramBotClient(botToken);
var fileName = "casino121.txt";

using CancellationTokenSource cts = new();

var players = LoadSavedData();

Dictionary<long, int> LoadSavedData()
{
    var res = new Dictionary<long, int>();
    var savedData = System.IO.File.ReadAllLines(fileName);

    foreach (var item in savedData)
    {
        var parts = item.Split(':');
        res[long.Parse(parts[0])] = int.Parse(parts[1]);
    }

    return res;
}

void SaveData(Dictionary<long, int> data)
{
    System.IO.File.WriteAllLines(fileName, data.Select(x => $"{x.Key}:{x.Value}"));
}
// StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
ReceiverOptions receiverOptions = new()
{
    AllowedUpdates = Array.Empty<UpdateType>() // receive all update types except ChatMember related updates
};

botClient.StartReceiving(
    updateHandler: HandleUpdateAsync,
    pollingErrorHandler: HandlePollingErrorAsync,
    receiverOptions: receiverOptions,
    cancellationToken: cts.Token
);

while (true)
{
    await Task.Delay(1000 * 60 * 10); // save every 10 minutes
    SaveData(players);
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

    if (update.Message.Text == "/top_10")
    {
        List<KeyValuePair<long, int>> top;
        if (players.Count <= 10)
            top = players.OrderByDescending(x => x.Value).ToList();
        else
            top = players.OrderByDescending(x => x.Value).Take(10).ToList();
        var res = "";
        for (int i = 0; i < top.Count; i++)
        {
            var user = await botClient.GetChatMemberAsync(chatId, top[i].Key);
            res = res + $"{i + 1} : {user.User.FirstName} {user.User.LastName} — {top[i].Value}\r\n";
        }
        await botClient.SendTextMessageAsync(chatId: chatId,
                                                   text: res,
                                                   replyToMessageId: update.Message.MessageId,
                                                   cancellationToken: cancellationToken);
        return;
    }

    if (update.Message.Text == "/antitop_10")
    {
        List<KeyValuePair<long, int>> top;
        if (players.Count <= 10)
            top = players.OrderBy(x => x.Value).ToList();
        else
            top = players.OrderBy(x => x.Value).Take(10).ToList();
        var res = "";
        for (int i = 0; i < top.Count; i++)
        {
            var user = await botClient.GetChatMemberAsync(chatId, top[i].Key);
            res = res + $"{i + 1} : {user.User.FirstName} {user.User.LastName} — {top[i].Value}\r\n";
        }
        await botClient.SendTextMessageAsync(chatId: chatId,
                                                   text: res,
                                                   replyToMessageId: update.Message.MessageId,
                                                   cancellationToken: cancellationToken);
        return;
    }

    if (update.Message.Text == "/get_balance")
    {

        var getBalanceSuccess = players.TryGetValue(update.Message.From.Id, out int balance);
        if (!getBalanceSuccess)
        {
            await botClient.SendTextMessageAsync(chatId: chatId,
                                                       text: $"Spin at list 1 slot first, you are not in SSU ludomans database",
                                                       replyToMessageId: update.Message.MessageId,
                                                       cancellationToken: cancellationToken);
            return;
        }
        await botClient.SendTextMessageAsync(chatId: chatId,
                                                        text: $"Your balance is {balance}",
                                                        replyToMessageId: update.Message.MessageId,
                                                        cancellationToken: cancellationToken);
        return;
    }
    //Console.WriteLine($"Received a '{messageText}' message in chat {chatId}.");
    if (message.Dice == null)
        return;
    if (message.Dice.Emoji != "🎰")
        return;


    var playerId = message.From.Id;

    int playerBalance;

    if (!players.ContainsKey(playerId))
        playerBalance = 0;
    else
        playerBalance = players[playerId];



    Console.WriteLine(message.Dice.Value);
    var win = 0;
    switch (message.Dice.Value)
    {
        case 64: // 777
            win = 25;
            break;
        case 1:  // bar 
            win = 20;
            break;
        case 43: // lemons
            win = 10;
            break;
        case 22: // grapes
            win = 5;
            break;
        default:
            break;
    }
    if (win != 0)
    {
        await Task.Delay(1500);
        playerBalance += win;
        Message m = await botClient.SendTextMessageAsync(chatId: chatId,
                                                         text: $"Hey hey hey, we have a winner here. You win {win} nihuya. Now you have {playerBalance} nihuya",
                                                         replyToMessageId: update.Message.MessageId,
                                                         cancellationToken: cancellationToken);

        //var replyMarkup = new ReplyKeyboardMarkup(chatId, messageText);

        //Message m = await botClient.SendTextMessageAsync(
        //chatId: chatId,
        //text: "Trying *all the parameters* of `sendMessage` method",
        //parseMode: ParseMode.MarkdownV2,
        //disableNotification: true,
        //replyToMessageId: update.Message.MessageId,
        //replyMarkup: new InlineKeyboardMarkup(
        //    InlineKeyboardButton.WithPayment(
        //        text: "Pay")),
        //cancellationToken: cancellationToken);
    }
    else playerBalance--;

    players[playerId] = playerBalance;
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



