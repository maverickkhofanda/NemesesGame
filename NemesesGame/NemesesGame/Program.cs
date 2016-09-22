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
        public static GamesHandler gamesHandler;

        public static Dictionary<long, string> groupLangPref = new Dictionary<long, string>(); // dunno how to save this yet... unimplemented yet
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

        public static long greyfader = 257394282;
        private static readonly TelegramBotClient Bot = new TelegramBotClient("242212370:AAF2psk3nA3F1Q78rJTVpGQbb7fryiEBl9Q"); //NemesesBot
		//private static readonly TelegramBotClient Bot = new TelegramBotClient("254602224:AAFgJBae5VsFQw34xlWk--qFlKXXX_J3TSk"); //SeaOfEdenBot

		static void Main(string[] args)
        {
            var me = Bot.GetMeAsync().Result;
            Bot.OnCallbackQuery += BotOnCallbackQueryReceived;
            Bot.OnMessage += BotOnMessageReceived;

            gamesHandler = new GamesHandler(me);
            LoadLanguage();
            
			Console.Title = me.Username;
            
            Bot.StartReceiving();

            //debug send language
            //SendMessage(-172612224, GetLangString(-172612224, "StartGame", "args[0]"));
            Console.WriteLine("Bot ready!");
            Console.ReadLine();
            Bot.StopReceiving();
        }

        private static async void BotOnCallbackQueryReceived(object sender, CallbackQueryEventArgs callbackQueryEventArgs)
        {
            var callbackQuery = callbackQueryEventArgs.CallbackQuery;
            var senderId = callbackQuery.From.Id;

            try
            {
               await gamesHandler.CallbackQueryHandler(callbackQuery);
            }
            catch (KeyNotFoundException e)
            {
                string reply = GetLangString(0, "NotJoinedGame");
                //Console.WriteLine(e);

                await SendMessage(senderId, reply);
            }
        }

        private static async void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {
            var message = messageEventArgs.Message;

            var chatId = message.Chat.Id;
            string entityType = "";

            try
            {
                entityType = message.Entities.ElementAt(0).Type.ToString();
            } catch { }

            // BotCommandHandler
            if (message.Text != null && entityType == "BotCommand")
            {
                try
                {
                    await gamesHandler.CommandHandler(message); ;
                }
                catch (KeyNotFoundException)
                {
                    string reply = GetLangString(0, "NotJoinedGame");
                    await SendMessage(chatId, reply);
                }
            }
        }
        #region Messaging
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
                    //Console.WriteLine("This chatgroup {0} don't have lang pref yet!", chatId);
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

                        //Console.WriteLine("arrayCount: " + arrayCount);
                        //Console.WriteLine("Output on index: " + i);

                        output = string.Format(token[i].ToString(), args);

                        return output;
                    }
                    else if (token.Type == JTokenType.String)
                    {
                        output = string.Format(token.ToString(), args);
                        return output;
                    }

                    Console.WriteLine("Error GetLangString(): No definition for type" + token.Type);
                    output = "Hmm... something went wrong in the game... please contact the dev (@greyfader or @leecopper15)\n\r[Error: string key not found in .json]\n\rThanks";

                    return output;
                }
                else
                {
                    output = "Hmm... something went wrong in the game... please contact the dev (@greyfader or @leecopper15)\n\rThanks";
                    throw new Exception($"Error getting string {key} with parameters {args.Aggregate((a, b) => a + "," + b.ToString())}");

                    return output;
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Error getting string {key} with parameters {args.Aggregate((a, b) => a + "," + b.ToString())}", e);
            }
        }

		public static async Task SendMessage(long chatId, string messageContent, IReplyMarkup repMarkup=null, ParseMode _parseMode = ParseMode.Markdown)
		{
            byte count = 0;
            try
            {
                await Bot.SendTextMessageAsync(chatId, messageContent, replyMarkup: repMarkup, parseMode: _parseMode);
                Console.WriteLine("Reply to " + chatId + " has been sent!");
            }
            catch (Telegram.Bot.Exceptions.ApiRequestException e)
            {
                count++;
                if (count <= 10)
                {
                    Console.WriteLine("Error SendMessage to {0} ({1})", chatId, e.ToString());
                    await Bot.SendTextMessageAsync(chatId, messageContent, replyMarkup: repMarkup, parseMode: _parseMode);
                }
                else
                {
                    for (byte i = 0; i < 10; i++)
                    {
                        Console.WriteLine("Telegram ApiRequestException can't be handled\r\nPlease break operation");
                    }
                    
                }
            }
        }

        public static async Task EditMessage(long chatId, int msgId, string messageContent, IReplyMarkup repMarkup = null, ParseMode _parseMode = ParseMode.Markdown)
        {
            byte count = 0;
            try
            {
                await Bot.EditMessageTextAsync(chatId, msgId, messageContent, replyMarkup: repMarkup, parseMode: _parseMode);
                Console.WriteLine("Message editted at " + chatId);
            }
            catch (Telegram.Bot.Exceptions.ApiRequestException e)
            {
                count++;
                if (count <= 10)
                {
                    Console.WriteLine("Error EditMessage to {0} ({1})", chatId, e.ToString());
                    await Bot.SendTextMessageAsync(chatId, messageContent, replyMarkup: repMarkup, parseMode: _parseMode);
                }
                else
                {
                    for (byte i = 0; i < 10; i++)
                    {
                        Console.WriteLine("Telegram ApiRequestException can't be handled\r\nPlease break operation");
                    }

                }
            }
        }
        #endregion
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

        public static string gameInfo =
			"In <Game name> game, you govern a city. You got one job: be the strongest state.\r\n\r\n" +
			"Here is the command list.\r\n" +
			"/joingame = Create new game / Join existing game\r\n" +
			"/playerlist = See the list of players who joined the game\r\n" + 
			"/leavegame = Leave the lobby";
    }
}
