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

        public async Task CommandHandler(Message message)
        {
            var messageText = message.Text;

            var senderId = message.From.Id;
            var senderFirstName = message.From.FirstName;
            var senderLastName = message.From.LastName;

            var thisChatId = message.Chat.Id;
            var thisChatName = "";

            Console.WriteLine("\r\nMessage received from " + senderFirstName + " (" + senderId + ")" + " at " + thisChatId);

            //assign thischatname
            if (message.Chat.Type == ChatType.Private)
            {
                thisChatName = senderFirstName;
            }
            else
            {
                thisChatName = message.Chat.Title;
            }

            //remove all non alphanumeric char
            char[] arr = messageText.ToCharArray();
            arr = Array.FindAll(arr, (c => (char.IsLetterOrDigit(c)
                                              || char.IsWhiteSpace(c)
                                              || c == '-')));
            messageText = new string(arr);

            string[] args = messageText.Split(null); //here, null means space (" ")
            //args[0] = args[0].Replace("/", "");
            args[0] = args[0].ToLower().Replace(me.Username.ToLower(), "");

            // for now... check explicitly: IsInGroup, IsDev
            switch (args[0])
            {
                case "choosename":
                    if (gameDict.ContainsKey(thisChatId))
                    {
                        if (gameDict[thisChatId].gameStatus == GameStatus.Starting)
                        {
							try
							{
								if (!string.IsNullOrWhiteSpace(args[1]))
                                {
                                    string newCityName = "";
                                    var newArgs = Program.RemoveElement(args, 0);

                                    foreach (string arg in newArgs)
                                    {
                                        newCityName += arg + " ";
                                    }

                                    // limits the cityName length...
                                    byte maxLength = 25;
                                    if (newCityName.Length >= maxLength)
                                    {
                                        newCityName = newCityName.Substring(0, maxLength);
                                    }

                                    await gameDict[thisChatId].ChooseName(senderId, newCityName);
                                    break;
                                }
                                else
                                {
                                    reply += GetLangString(thisChatId, "ChooseNameEmpty");
                                    await BotReply(thisChatId);
                                    break;
                                }
                            } catch (IndexOutOfRangeException e)
                            {
                                reply += GetLangString(thisChatId, "ChooseNameEmpty");
                                await BotReply(thisChatId);
                                break;
                            }
                        }
                        else
                        {
                            reply += GetLangString(thisChatId, "CantChooseName");
                            await BotReply(thisChatId);
                            break;
                        }
                    }
                    else
                    {
                        reply += GetLangString(thisChatId, "GameNotFound");
                        await BotReply(thisChatId);
                    }
                    break;
                
                #region lobby commands

                case "joingame":
                    // check if inGroup or inSuperGroup
                    if (message.Chat.Type == ChatType.Group || message.Chat.Type == ChatType.Supergroup)
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
                    if (message.Chat.Type == ChatType.Group || message.Chat.Type == ChatType.Supergroup)
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
                #endregion

                #region dev commands

                case "forceturn":
                    if (senderId == Program.greyfader)
                    {
                        //await gameDict[thisChatId].Turn();
                        reply += "forceturn unimplemented yet"; //GetLangString(thisChatId, "")
                    } else
                    {
                        reply += GetLangString(thisChatId, "NotDev");
                    }
                    
                    await BotReply(thisChatId);
                    break;

                #endregion

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

        public async Task CallbackQueryHandler(CallbackQuery callbackQuery)
        {
            var msgId = callbackQuery.Message.MessageId;

            var senderId = callbackQuery.From.Id;
            var senderFirstName = callbackQuery.From.FirstName;
            var senderLastName = callbackQuery.From.LastName;
            var senderName = senderFirstName + " " + senderLastName;

            var args = callbackQuery.Data.Split('|');
            // QueryCommand = args[0]
            long groupId = long.Parse(args[1]);

            
            if (gameDict[groupId].cities[senderId].chat.msgId != msgId)
            {
                gameDict[groupId].cities[senderId].chat.msgId = msgId;
            }
            
              
            Console.WriteLine("\r\nCallbackQuery received from " + senderName + " (" + senderId + ")");
            Console.WriteLine("CallbackQuery data : " + callbackQuery.Data);

            switch (args[0])
            {
                #region game callbacks

                case "ResourceUpgrade":
					await gameDict[groupId].ResourceUpgrade(senderId, msgId, args[2]);
                    break;

                case "RaiseArmy":
                    // check if args has the additional parameter...
                    switch(args.Length)
                    {
                        case 2:
                            await gameDict[groupId].RaiseArmy(senderId, msgId);
                            break;
                        case 3:
                            await gameDict[groupId].RaiseArmy(senderId, msgId, args[2]);
                            break;
                        case 4:
                            int armyNumber = int.Parse(args[3]);
                            Console.WriteLine("armyNumber: " + armyNumber);
                            await gameDict[groupId].RaiseArmy(senderId, msgId, args[2], armyNumber);
                            break;
                    }
                    break;

                #endregion

                #region menu callbacks
                case "AssignTask":
                    await gameDict[groupId].AssignTask(senderId, msgId);
                    break;

                    case "UpgradeProduction":
                        await gameDict[groupId].UpgradeProduction(senderId, msgId);
                        break;

                case "YourStatus":
                    await gameDict[groupId].MyStatus(senderId, msgId);
                    break;

                case "Back":
                    await gameDict[groupId].Back(senderId, msgId);
                    break;

                default:
                    break;

                #endregion
            }
        }

        string GetLangString(long chatId, string key, params object[] args)
        {
            return Program.GetLangString(chatId, key, args);
        }

        async Task BotReply(long groupId, IReplyMarkup replyMarkup = null, ParseMode _parseMode = ParseMode.Markdown)
        {
            string replyString = reply;
            reply = "";
            await Program.SendMessage(groupId, replyString, replyMarkup, _parseMode);
            replyString = "";
        }
    }
}
