using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using NemesesGame;
using Telegram.Bot.Types.Enums;

namespace NemesesGame
{
    public enum GameStatus
    {
        Unhosted, // Lobby hasn't been made
        Hosted, // Lobby has been hosted, waiting for players to join and start game
        Starting, // Lobby just started, starting turn 0 (Enable turn 0 commands, etc.)
        InGame, // Game is currently running
        Ending // Game has just finished
    }

    /// <summary>
    /// Handles commands & callback queries, contains games
    /// </summary>
    public class GamesHandler
    {
        public static Dictionary<long, Game> gameDict = new Dictionary<long, Game>();

        string reply;
        User me;

        public GamesHandler(User thisBot)
        {
            me = thisBot;
        }

        public async Task CommandHandler(UpdateEventArgs u)
        {
            var message = u.Update.Message;

            var messageText = message.Text;

            var senderId = message.From.Id;
            var senderFirstName = message.From.FirstName;
            var senderLastName = message.From.LastName;

            var thisChatId = message.Chat.Id;
            var thisChatName = "";

            //assign thischatname
            if (message.Chat.Type == ChatType.Private)
            {
                thisChatName = senderFirstName;
            }
            else
            {
                thisChatName = message.Chat.Title;
            }

            string[] args = messageText.Split(null); //here, null means space (" ")
            args[0] = args[0].Replace("/", "");
            args[0] = args[0].ToLower().Replace("@" + me.Username.ToLower(), "");
            /*
            try
            {
                Console.WriteLine(args[1]);
            } catch { Console.WriteLine("args[1] not found"); }
            */

            // for now... check explicitly: IsInGroup, IsDev
            switch (args[0])
            {
                case "joingame":
                    // check if inGroup or inSuperGroup
                    if (message.Chat.Type == ChatType.Group | message.Chat.Type == ChatType.Supergroup)
                    {
                        //Check if there is a lobby for that thisChatId
                        if (!gameDict.ContainsKey(thisChatId))
                        {
                            //If no, make one
                            gameDict.Add(thisChatId, new Game(thisChatId, thisChatName));
                        }
                        //Join the lobby
                        await gameDict[thisChatId].PlayerJoin(senderId, senderFirstName, senderLastName);
                    }
                    else
                    {
                        reply += GetLangString(thisChatId, "InGroupOnly");
                        await BotReply(thisChatId);
                    }
                    break;

                case "startgame":
                    // check if inGroup or inSuperGroup
                    if (message.Chat.Type == ChatType.Group | message.Chat.Type == ChatType.Supergroup)
                    {
                        if (gameDict.ContainsKey(thisChatId))
                        {
                            await gameDict[thisChatId].StartGame();
                            
                        }
                        else
                        {
                            reply += GetLangString(thisChatId, "GameNotFound");
                            await BotReply(thisChatId);
                            break;
                        }
                    }
                    else
                    {
                        reply += GetLangString(thisChatId, "InGroupOnly");
                        await BotReply(thisChatId);
                    }
                    break;

                case "choosename":
                    if (gameDict.ContainsKey(thisChatId)) {
                        if (gameDict[thisChatId].gameStatus == GameStatus.Starting)
                        {
                            string newCityName = args[1];
                            await gameDict[thisChatId].ChooseName(senderId, newCityName);
                            break;
                        }
                        else
                        {
                            reply += GetLangString(thisChatId, "CantChooseName");
                            await BotReply(thisChatId);
                            break;
                        }
                    } else
                    {
                        reply += GetLangString(thisChatId, "GameNotFound");
                        await BotReply(thisChatId);
                    }
                    break;

                case "playerlist":
                    if (gameDict.ContainsKey(thisChatId))
                    {
                        await gameDict[thisChatId].PlayerList();
                    }
                    else
                    {
                        reply += GetLangString(thisChatId, "GameNotFound");
                        await BotReply(thisChatId);
                    }
                    break;

                case "leavegame":
                    if (gameDict.ContainsKey(thisChatId))
                    {
                        await gameDict[thisChatId].PlayerLeave(senderId, senderFirstName, senderLastName);

                        if (gameDict[thisChatId].PlayerCount <= 0)
                        {
                            await gameDict[thisChatId].GameUnhosted();

                            gameDict.Remove(thisChatId);
                        }
                    }
                    else
                    {
                        reply += GetLangString(thisChatId, "GameNotFound");
                        await BotReply(thisChatId);
                    }
                    break;

                case "start":
                    // Game Info not inserted to language.json yet!
                    reply += Program.gameInfo;
                    await BotReply(thisChatId);
                    break;

                default:
                    reply += GetLangString(thisChatId, "CommandNotFound");
                    await BotReply(thisChatId);
                    break;
            }
        }

        public string GetLangString(long chatId, string key, params object[] args)
        {
            return Program.GetLangString(chatId, key, args);
        }

        public async Task BotReply(long groupId, IReplyMarkup replyMarkup = null, ParseMode _parseMode = ParseMode.Markdown)
        {
            await Program.SendMessage(groupId, reply, replyMarkup, _parseMode);
            reply += ""; //Reset reply string
        }
    }
}
