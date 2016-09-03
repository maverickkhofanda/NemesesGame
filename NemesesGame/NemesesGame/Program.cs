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
            
        private static readonly TelegramBotClient Bot = new TelegramBotClient("242212370:AAF2psk3nA3F1Q78rJTVpGQbb7fryiEBl9Q");

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
            var senderFirstName = message.From.FirstName;
            var senderLastName = message.From.LastName;
            var senderName = senderFirstName + senderLastName;
            string entityType = "";

            Console.WriteLine("\r\nMessage received from " + senderName + " (" + senderId + ")" + " at " + chatId);

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
                    string string0;
                    if (GameDict.ContainsKey(chatId))
                    {
                        string0 = GameDict[chatId].PlayerJoin(senderId, senderFirstName, senderLastName);
                        await Bot.SendTextMessageAsync(chatId, string0);
                    }
                    else
                    {
                        GameDict.Add(chatId, new Game());
                        await Bot.SendTextMessageAsync(chatId, GameDict[chatId].GameHosted());

                        string0 = GameDict[chatId].PlayerJoin(senderId, senderFirstName, senderLastName);
                        await Bot.SendTextMessageAsync(chatId, string0);
                    }
                }
                else if (messageText.StartsWith("/playerlist"))
				{
					if (GameDict.ContainsKey(chatId))
					{
						int playerCount = GameDict[chatId].PlayerCount;
						string playerListInfo = playerCount + " players have joined this lobby :";

						foreach (KeyValuePair<long, City> kvp in GameDict[chatId].players)
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
