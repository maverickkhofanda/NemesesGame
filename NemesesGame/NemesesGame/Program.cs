using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using NemesesGame;

namespace NemesesGame
{
    class Program
    {
        public static Dictionary<long, Game> GameDict = new Dictionary<long, Game>();
            
        private static readonly TelegramBotClient Bot = new TelegramBotClient("254602224:AAFgJBae5VsFQw34xlWk--qFlKXXX_J3TSk");

        static void Main(string[] args)
        {
            var me = Bot.GetMeAsync().Result;
            Bot.OnMessage += BotOnMessageReceived;

			Console.Title = me.Username;

            Bot.StartReceiving();
            Console.ReadLine();
            Bot.StopReceiving();
        }

        private static async void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {
            var message = messageEventArgs.Message;
            var messageText = message.Text;
            var chatId = message.Chat.Id;
			var senderId = message.From.Id;
			var senderName = message.From.FirstName + " " + message.From.LastName;
            string entityType = "";

            Console.WriteLine("Message received from " + senderName + " (" + senderId + ")" + " at " + chatId);

            try
            {
                entityType = message.Entities.ElementAt(0).Type.ToString();
                
                Console.WriteLine("entityType: " + entityType);

            } catch (Exception e) { }

            //Command 'cabling'
            if (entityType == "BotCommand")
            {
                if (messageText.StartsWith("/joingame"))
				{
					if (GameDict.ContainsKey(chatId))
					{
						GameDict[chatId].PlayerJoin(senderId, senderName);
						await Bot.SendTextMessageAsync(chatId, senderName + " joined the game!");
					}
					else
					{
						GameDict.Add(chatId, new Game());
						GameDict[chatId].PlayerJoin(senderId, senderName);
                        await Bot.SendTextMessageAsync(chatId, GameDict[chatId].GameHosted());
                    }
                }
				else if (messageText.StartsWith("/playerlist"))
				{
					if (GameDict.ContainsKey(chatId))
					{
						int playerCount = GameDict[chatId].PlayerCount;
						string playerListInfo = playerCount + " players have joined this lobby :";

						foreach (KeyValuePair<long, string> kvp in GameDict[chatId].players)
						{
							playerListInfo += "\r\n" + kvp.Value;
						}
						await Bot.SendTextMessageAsync(chatId, playerListInfo);
					}
					else
					{
						await Bot.SendTextMessageAsync(chatId, "No game has been hosted in this lobby yet.\r\nUse /joingame to make one!");
					}
				}
				else if (messageText.StartsWith("/start"))
				{
					await Bot.SendTextMessageAsync(chatId, "Insert player data to database unimplemented yet!\r\n\r\n" + gameInfo);
                }
				else
				{
                    await Bot.SendTextMessageAsync(chatId, "Command not found!");
                }
            }
			else
			{
				await Bot.SendTextMessageAsync(chatId, gameInfo);
			}
            
            Console.WriteLine("Reply has been sent!");
        }

		static string gameInfo =
			"In <Game name> game, you govern a city. You got one job: be the strongest state.\r\n\r\n" +
			"Here is the command list.\r\n" +
			"/joingame = Create new game / Join existing game\r\n" +
			"/playerlist = See the list of players who joined the game";
    }
}
