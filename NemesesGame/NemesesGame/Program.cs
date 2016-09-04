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

		private static readonly TelegramBotClient Bot = new TelegramBotClient("242212370:AAF2psk3nA3F1Q78rJTVpGQbb7fryiEBl9Q"); //NemesesBot
		//private static readonly TelegramBotClient Bot = new TelegramBotClient("254602224:AAFgJBae5VsFQw34xlWk--qFlKXXX_J3TSk"); //SeaOfEdenBot

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
            string chatName = "";
			var senderId = message.From.Id;
            var senderFirstName = message.From.FirstName;
            var senderLastName = message.From.LastName;
            var senderName = senderFirstName + " " + senderLastName;
            string entityType = "";

            Console.WriteLine("\r\nMessage received from " + senderName + " (" + senderId + ")" + " at " + chatId);

            try
            {
                entityType = message.Entities.ElementAt(0).Type.ToString();
                chatName = message.Chat.Title;
                
                Console.WriteLine("entityType: " + entityType);

            } catch (Exception e) { }

            //Command 'cabling'
            if (entityType == "BotCommand")
            {
                if (messageText.StartsWith("/joingame"))
                {
					//Check if there is a lobby for that chatId
					if (!GameDict.ContainsKey(chatId))
					{
						//If no, make one
						GameDict.Add(chatId, new Game(chatName));
					}
					//Join the lobby
					GameDict[chatId].PlayerJoin(senderId, senderFirstName, senderLastName);

					await Bot.SendTextMessageAsync(chatId, GameDict[chatId].BotReply());
				}
                else if (messageText.StartsWith("/startgame"))
                {
                    if (GameDict.ContainsKey(chatId))
                    {
                        GameDict[chatId].StartGame();
                        await Bot.SendTextMessageAsync(chatId, GameDict[chatId].BotReply());

                        //chat each player privately: UNIMPLEMENTED YET
                        /*
                            foreach (KeyValuePair<long, City> player in GameDict[chatId].players)
                            {
                                await Bot.SendTextMessageAsync(senderId, GameDict[chatId].PrivateReply());
                            }
                        */
                    }
                    else
                    {
                        await Bot.SendTextMessageAsync(chatId, "No game has been hosted in this lobby yet.\r\nUse /joingame to make one!");
                    }
                }
                else if (messageText.StartsWith("/playerlist"))
				{
					if (GameDict.ContainsKey(chatId))
					{
						GameDict[chatId].PlayerList();

						await Bot.SendTextMessageAsync(chatId, GameDict[chatId].BotReply());
					}
					else
					{
						await Bot.SendTextMessageAsync(chatId, "No game has been hosted in this lobby yet.\r\nUse /joingame to make one!");
					}
				}
				else if (messageText.StartsWith("/leavegame"))
				{
					if (GameDict.ContainsKey(chatId))
					{
						GameDict[chatId].PlayerLeave(senderId, senderFirstName, senderLastName);

						if (GameDict[chatId].PlayerCount <= 0)
						{
							GameDict[chatId].GameUnhosted();

							await Bot.SendTextMessageAsync(chatId, GameDict[chatId].BotReply());

							GameDict.Remove(chatId);
						}
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
			"/playerlist = See the list of players who joined the game\r\n" + 
			"/leavegame = Leave the lobby";
    }
}
