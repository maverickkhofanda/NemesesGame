using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using NemesesGame;
using System.Reflection;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NemesesGame
{
    class Program
    {
        public static Dictionary<long, Game> GameDict = new Dictionary<long, Game>();

        public static Dictionary<long, string> groupLangPref = new Dictionary<long, string>();
        public static Dictionary<string, JObject> langFiles = new Dictionary<string, JObject>();
        static string languageDirectory = Path.GetFullPath(Path.Combine(Program.rootDirectory, @"..\Language"));

        public static string rootDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

		private static readonly TelegramBotClient Bot = new TelegramBotClient("242212370:AAF2psk3nA3F1Q78rJTVpGQbb7fryiEBl9Q"); //NemesesBot
		//private static readonly TelegramBotClient Bot = new TelegramBotClient("254602224:AAFgJBae5VsFQw34xlWk--qFlKXXX_J3TSk"); //SeaOfEdenBot

		static void Main(string[] args)
        {
            var me = Bot.GetMeAsync().Result;
            Bot.OnMessage += BotOnMessageReceived;

            LoadLanguage();

			Console.Title = me.Username;

            Bot.StartReceiving();

            //debug send language
            //SendMessage(-172612224, GetLangString(-172612224, "StartGame", "args[0]"));
            Console.WriteLine("Bot ready!");
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

                if(message.Chat.Type == ChatType.Private)
                {
                    chatName = senderFirstName;
                } else
                {
                    chatName = message.Chat.Title;
                }
            } catch { }

            // Check if the message is a BotCommand, then implement it
            if (entityType == "BotCommand")
            {
                if (messageText.StartsWith("/joingame"))
                {
					//Check if there is a lobby for that chatId
					if (!GameDict.ContainsKey(chatId))
					{
						//If no, make one
						GameDict.Add(chatId, new Game(chatId, chatName));
					}
					//Join the lobby
					GameDict[chatId].PlayerJoin(senderId, senderFirstName, senderLastName);
				}
                else if (messageText.StartsWith("/startgame"))
                {
                    if (GameDict.ContainsKey(chatId))
                    {
                        GameDict[chatId].StartGame();
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
            
            Console.WriteLine("Reply to " + chatName + " has been sent!");
        }

        public static void LoadLanguage()
        {
            try
            {
                var files = Directory.GetFiles(languageDirectory);
                foreach (string file in files)
                {
                    string languageName = Path.GetFileNameWithoutExtension(file);

                    using (TextReader tr = System.IO.File.OpenText(file))
                    {
                        using (JsonTextReader jtr = new JsonTextReader(tr))
                        {
                            langFiles.Add(languageName, (JObject)JToken.ReadFrom(jtr));
                            //Console.WriteLine(languageFile.ToString());

                            Console.WriteLine("Loaded language: " + languageName);
                        }
                    }
                }
            }
            catch (Exception e) { Console.WriteLine(e); }
        }
        public static string GetLangString(long chatId, string key, params object[] args)
        {
            string output;

            try
            {
                JToken events;

                if (!groupLangPref.ContainsKey(chatId))
                {
                    events = langFiles["English"].SelectToken("events");
                }
                else
                {
                    string thisLangPref = groupLangPref[chatId];
                    events = langFiles[thisLangPref].SelectToken("events");
                }

                var token = events.SelectToken(key);

                if (token != null)
                {
                    if (token.Type == JTokenType.Array)
                    {
                        JArray array = (JArray)token;

                        //randomize
                        int arrayCount = array.Count();
                        Random rnd = new Random();
                        int i = rnd.Next(0, arrayCount);

                        Console.WriteLine("arrayCount: " + arrayCount);
                        Console.WriteLine("Output on index: " + i);

                        output = string.Format(token[i].ToString(), args);

                        return output;
                    }
                    else if (token.Type == JTokenType.String)
                    {
                        output = string.Format(token.ToString(), args);
                        return output;
                    }

                    Console.WriteLine("Error GetLangString(): No definition for type" + token.Type);
                    output = "Hmm... something went wrong in the game... please contact the dev (@greyfader or @leecopper15)\n\rThanks";

                    return output;
                }
                else
                {
                    output = "Hmm... something went wrong in the game... please contact the dev (@greyfader or @leecopper15)\n\rThanks";
                    return output;

                    throw new Exception($"Error getting string {key} with parameters {args.Aggregate((a, b) => a + "," + b.ToString())}");
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Error getting string {key} with parameters {args.Aggregate((a, b) => a + "," + b.ToString())}", e);
            }
        }

		public static async void SendMessage(long chatId, string messageContent, IReplyMarkup repMarkup=null)
		{
			await Bot.SendTextMessageAsync(chatId, messageContent, replyMarkup: repMarkup);
		}

		public static T[] RemoveElement<T>(T[] thisArray, int RemoveAt)
        {
            T[] newIndicesArray = new T[thisArray.Length - 1];

            int i = 0;
            int j = 0;
            while (i < thisArray.Length)
            {
                if (i != RemoveAt)
                {
                    newIndicesArray[j] = thisArray[i];
                    j++;
                }

                i++;
            }

            return newIndicesArray;
        }

        static string gameInfo =
			"In <Game name> game, you govern a city. You got one job: be the strongest state.\r\n\r\n" +
			"Here is the command list.\r\n" +
			"/joingame = Create new game / Join existing game\r\n" +
			"/playerlist = See the list of players who joined the game\r\n" + 
			"/leavegame = Leave the lobby";
    }
}
