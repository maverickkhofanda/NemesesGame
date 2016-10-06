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
using System.Diagnostics;

namespace NemesesGame
{
    static class Program
    {
        public static GamesHandler gamesHandler;

        public static Dictionary<long, string> groupLangPref = new Dictionary<long, string>(); // dunno how to save this yet... unimplemented yet
        public static Dictionary<string, JObject> langFiles = new Dictionary<string, JObject>();
        static string languageDirectory = Path.GetFullPath(Path.Combine(Program.rootDirectory, @"..\Language"));

        public static Dictionary<int, Stopwatch> timers = new Dictionary<int, Stopwatch>();

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
            int msgId = callbackQueryEventArgs.CallbackQuery.Message.MessageId;

            try
            {
                timers.Add(msgId, new Stopwatch());
                timers[msgId].Start();
            }
            catch (ArgumentException) { }

            var callbackQuery = callbackQueryEventArgs.CallbackQuery;
            var senderId = callbackQuery.From.Id;

            try
            {
               await gamesHandler.CallbackQueryHandler(callbackQuery);
            }
            catch (KeyNotFoundException)
            {
                string reply = GetLangString(0, "NotJoinedGame");
                //Console.WriteLine(e);

                await SendMessage(senderId, reply);
            }
        }

        private static async void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {
            var message = messageEventArgs.Message;
            
            // start stopwatch
            Console.WriteLine("\nMsg {0} received from user {1} in chat {2}",
                message.MessageId,
                message.From.FirstName,
                message.Chat.Id);
            Stopwatch sw = new Stopwatch();
            sw.Start();

            var chatId = message.Chat.Id;
            MessageEntityType msgEntityType = MessageEntityType.Bold;

            // only get TextLink if it's in reply
            // we use TextLink for saving parameters
            if (message.ReplyToMessage != null)
            {
                foreach (MessageEntity msgEnt in message.ReplyToMessage.Entities)
                {
                    if (msgEnt.Type == MessageEntityType.TextLink)
                    {
                        msgEntityType = msgEnt.Type;
                        break;
                    }
                }
            }
            else
            {
                try
                {
                    // find the command or textLink
                    foreach (MessageEntity msgEnt in message.Entities)
                    {
                        if (msgEnt.Type == MessageEntityType.BotCommand || msgEnt.Type == MessageEntityType.TextLink)
                        {
                            msgEntityType = msgEnt.Type;
                            break;
                        }
                    }
                }
                catch { }
            }

            // BotCommandHandler
            if (message.Text != null && (msgEntityType == MessageEntityType.BotCommand || msgEntityType == MessageEntityType.TextLink))
            {
                if (msgEntityType == MessageEntityType.BotCommand)
                {
                    try
                    {
                        await gamesHandler.CommandHandler(message);
                    }
                    catch (KeyNotFoundException)
                    {
                        string reply = GetLangString(0, "NotJoinedGame");
                        await SendMessage(chatId, reply);
                    }
                }
                else if (msgEntityType == MessageEntityType.TextLink)
                {
                    await gamesHandler.ReplyMsgHandler(message);
                }
                
            }
            //else if(message.Text != null && msgEntity.Type == MessageEntityType.TextLink)
            //{

            //    Console.WriteLine("Received TextLink from reply!");
            //    await gamesHandler.ReplyMsgHandler(message);
            //}

            sw.Stop();
            Console.WriteLine("Msg {0} processed in {1} ms", message.MessageId, sw.ElapsedMilliseconds);
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

                    //return output;
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Error getting string {key} with parameters {args.Aggregate((a, b) => a + "," + b.ToString())}", e);
            }
        }

        public static async Task<Message> SendMessage(long chatId, string messageContent, IReplyMarkup repMarkup = null, ParseMode _parseMode = ParseMode.Markdown)
		{
            //byte count = 0;
            Message message = await Bot.SendTextMessageAsync(chatId, messageContent, replyMarkup: repMarkup, parseMode: _parseMode);

            Console.WriteLine("Reply to " + message.Chat.Id + " has been sent! (msgId: " + message.MessageId);

            return message;
            /*
            try
            {
                
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
            */
        }

        public static async Task EditMessage(long chatId, int msgId, string messageContent, IReplyMarkup repMarkup = null, ParseMode _parseMode = ParseMode.Markdown)
        {
            byte count = 0;
            try
            {
                await Bot.EditMessageTextAsync(chatId, msgId, messageContent, replyMarkup: repMarkup, parseMode: _parseMode);

                object elapsed;

                try
                {
                    // stop stopwatch
                    timers[msgId].Stop();
                    elapsed = timers[msgId].ElapsedMilliseconds;
                    timers.Remove(msgId);
                } catch (KeyNotFoundException)
                {
                    elapsed = "???";
                }
                Console.WriteLine("Msg {1} at {0} processed in {2} ms", chatId, msgId, elapsed);
            }
            catch (Telegram.Bot.Exceptions.ApiRequestException)
            {
                count++;
                if (count <= 10)
                {
                    //Console.WriteLine("Error EditMessage to {0} ({1})", chatId, e.ToString());
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

        public static void Shuffle<T>(this Random rng, T[] array)
        {
            int n = array.Length;
            while (n > 1)
            {
                int k = rng.Next(n--);
                T temp = array[n];
                array[n] = array[k];
                array[k] = temp;
            }
        }

        public static string UppercaseFirst(string s)
        {
            char[] zero = s.ToCharArray(0, 1);
            zero[0] = char.ToUpperInvariant(zero[0]);
            return new string(zero) + s.Substring(1);
        }

        public static string gameInfo =
			"In <Game name> game, you govern a city. You got one job: be the strongest state.\r\n\r\n" +
			"Here is the command list.\r\n" +
			"/joingame = Create new game / Join existing game\r\n" +
			"/playerlist = See the list of players who joined the game\r\n" + 
			"/leavegame = Leave the lobby";
    }
}
