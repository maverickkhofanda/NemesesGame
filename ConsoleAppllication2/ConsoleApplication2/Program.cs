using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using ConsoleApplication2;

namespace ConsoleApplication2
{
    class Program
    {
        public static Dictionary<long, Game> GameDict = new Dictionary<long, Game>();
            
        private static readonly TelegramBotClient Bot = new TelegramBotClient("242212370:AAF2psk3nA3F1Q78rJTVpGQbb7fryiEBl9Q");

        static void Main(string[] args)
        {
            var me = Bot.GetMeAsync().Result;
            Bot.OnMessage += BotOnMessageReceived;

            Bot.StartReceiving();
            Console.ReadLine();
            Bot.StopReceiving();
        }

        private static async void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {
            var message = messageEventArgs.Message;
            var messageText = message.Text;
            var chatId = message.Chat.Id;
            var telegramId = message.From.Id;
            var firstName = message.From.FirstName;
            var lastName = message.From.LastName;
            string entityType = "";

            Console.WriteLine("\r\nMessage received from " + message.From.FirstName + " at " + chatId);
            try
            {
                entityType = message.Entities.ElementAt(0).Type.ToString();
                
                Console.WriteLine("entityType: " + entityType);

            } catch (Exception e) { }

            //Command 'cabling'
            if (entityType == "BotCommand")
            {
                if (messageText.StartsWith("/joingame")) {
                    if (GameDict.ContainsKey(chatId))
                    {
                        GameDict[chatId].AddPlayer(telegramId, firstName);
                        //string asdf = GameDict[chatId].PlayersDict[telegramId] + "has joined the game!";
                        await Bot.SendTextMessageAsync(chatId, "/joingame has run!");
                        await Bot.SendTextMessageAsync(chatId, "please make this block of code more beautiful :)");
                    }
                    else
                    {
                        GameDict.Add(chatId, new Game(telegramId, firstName));
                        await Bot.SendTextMessageAsync(chatId, GameDict[chatId].IsItRunning());
                    }
                } else if (messageText.StartsWith("/start")) {
                        await Bot.SendTextMessageAsync(chatId,
                            "Insert player data to database unimplemented yet!\r\n\r\n" + commandInfo);
                } else {
                        await Bot.SendTextMessageAsync(chatId, "Command not found!");
                }
            } else { await Bot.SendTextMessageAsync(chatId, commandInfo); }
            
            Console.WriteLine("Reply has been sent!");
        }

        static string commandInfo =
            "In <Game name> game, you govern a city. You got one job: be the strongest state.\r\n\r\nHere is the command list.\r\n/joingame = Create new game / Join existing game";
    }
}


