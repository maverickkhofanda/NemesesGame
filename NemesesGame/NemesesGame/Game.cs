using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Timers;
using NemesesGame;
using Telegram.Bot.Types;

namespace NemesesGame
{
    public class Game
    {
		Timer _timer;
        int turnInterval = 30;

        private int playerCount = 0;
        public long groupId;
        public string chatName;
        RefResources refResources = new RefResources();

		private string botReply = "";
        private string privateReply = "";
        List<InlineKeyboardButton> buttons = new List<InlineKeyboardButton>();
        InlineKeyboardMarkup menu;

        public Dictionary<long, City> cities = new Dictionary<long, City>();
        string[] cityNames = { "Andalusia", "Transylvania", "Groot", "Saruman", "Azeroth" };

		public GameStatus gameStatus = GameStatus.Unhosted;
        byte turn = 0;

        public Game()
        {
            Console.WriteLine("chatName & groupId unassigned yet!");
        }


        public Game(long ChatId, string ChatName)
        {
            groupId = ChatId;
            chatName = ChatName;
			gameStatus = GameStatus.Hosted;
        }

        public async Task StartGame()
        {
            botReply += GetLangString(groupId, "StartGameGroup");
            await PlayerList();

            botReply += GetLangString(groupId, "AskChooseName", turnInterval);
			gameStatus = GameStatus.Starting;
            await BotReply(groupId);

            Timer(turnInterval, Turn);
		}
        
        /// <summary>
        /// still a STUB
        /// </summary>
        public async void Turn(object sender, ElapsedEventArgs e)
        {
            turn++;
			gameStatus = GameStatus.InGame;

            if(turn == 1)
            {
                botReply += GetLangString(groupId, "PlayerListHeader");

                foreach (KeyValuePair<long, City> kvp in cities)
                {
                    City city = kvp.Value;
                    privateReply += string.Format(GetLangString(groupId, "StartGamePrivate", city.playerDetails.cityName));
                    privateReply += string.Format(
                        Program.GetLangString(groupId, "CurrentResources",

                        city.playerDetails.cityName,
                        city.cityResources.Gold, city.resourceRegen.Gold,
                        city.cityResources.Wood, city.resourceRegen.Wood,
                        city.cityResources.Stone, city.resourceRegen.Stone,
                        city.cityResources.Mithril, city.resourceRegen.Mithril));
                    await PrivateReply(kvp.Key);

                    botReply += GetLangString(groupId, "IteratePlayerCityName", city.playerDetails.firstName, city.playerDetails.cityName);
                }
            }

            botReply += GetLangString(groupId, "NextTurn", turn, turnInterval);
			await BotReply(groupId);

            //Insert turn implementation here
            await AskAction();
            
		}

        public async Task ChooseName(long PlayerId, string NewCityName)
        {
            cities[PlayerId].playerDetails.cityName = NewCityName;
            privateReply += GetLangString(groupId, "NameChosen", NewCityName);
            await PrivateReply(PlayerId);
        }

        private void Timer(int timerInterval, ElapsedEventHandler elapsedEventHandler, bool timerEnabled = true)
        {
            _timer = new Timer();
            _timer.Elapsed += elapsedEventHandler;
            _timer.Interval = timerInterval * 1000;
            _timer.Enabled = true;
        }

        #region Inline Keyboard Interaction
        public async Task AskAction()
        {
            foreach (KeyValuePair<long, City> kvp in cities)
            {
                privateReply += GetLangString(groupId, "AskAction");

                buttons.Add(new InlineKeyboardButton("Assign Task", $"AssignTask|{groupId}"));
                buttons.Add(new InlineKeyboardButton("Your Status", $"YourStatus|{groupId}"));
                menu = new InlineKeyboardMarkup(buttons.Select(x => new[] { x }).ToArray());
                
                await PrivateReply(kvp.Key, replyMarkup: menu);
            }
        }

        public async Task AssignTask(long playerId, int messageId)
        {
            privateReply += GetLangString(groupId, "AssignTask");

            buttons.Add(new InlineKeyboardButton("UpgradeProduction", $"UpgradeProduction|{groupId}"));
            buttons.Add(new InlineKeyboardButton("Raise Army", $"RaiseArmy|{groupId}"));
            menu = new InlineKeyboardMarkup(buttons.Select(x => new[] { x }).ToArray());

            await EditMessage(playerId, messageId, replyMarkup: menu);
        }

        public async Task UpgradeProduction(long playerId, int messageId)
        {
            privateReply += GetLangString(groupId, "UpgradeProductionHeader");

            // wood {0}/{1}/{2} each turn
            string woodString = "Wood🌲: ";
            byte woodLength = (byte) refResources.ResourceRegen[ResourceType.Wood].Length;
            for (byte i = 0; i < woodLength; i++)
            {
                string regen = refResources.ResourceRegen[ResourceType.Wood][0].ToString();

                if (cities[playerId].lvlResourceRegen[ResourceType.Wood] == i)
                {
                    regen = ToBold(ref regen);
                }
                    woodString += regen + "/";
            }
            woodString.TrimEnd('/');

            buttons.Add(new InlineKeyboardButton(woodString, $"woodUpgrade|{groupId}"));
            menu = new InlineKeyboardMarkup(buttons.Select(x => new[] { x }).ToArray());

            await EditMessage(playerId, messageId, replyMarkup: menu);
        }

        #endregion

        #region Manage Lobby
        public async Task GameHosted()
        {
            gameStatus = GameStatus.Hosted;
            botReply += "New game is made in this lobby!\r\n";
            await BotReply(groupId);
        }

        public async Task GameUnhosted()
        {
            gameStatus = GameStatus.Unhosted;
            botReply += "Lobby unhosted!\r\n";
            await BotReply(groupId);
        }
        public bool PlayerCheck(long telegramId, string firstName, string lastName)
        {
            //Checks if a player has joined the lobby
            if (cities.ContainsKey(telegramId))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task PlayerJoin(long telegramId, string firstName, string lastName)
		{
			if (!PlayerCheck(telegramId, firstName, lastName))
			{
                //randomize city name
                int i = cityNames.Length;
                Random rnd = new Random();
                i = rnd.Next(0, i);
                string cityName = cityNames[i];
                Program.RemoveElement(cityNames, i);
                                
				cities.Add(telegramId, new City(telegramId, firstName, lastName, cityName, groupId));
				playerCount++;

				if (playerCount == 1) //Lobby has just been made
				{
					await GameHosted();
				}
                botReply += GetLangString(groupId, "JoinedGame", firstName, lastName).Replace("  ", " ");
				await BotReply(groupId);
			}
			else
			{
                botReply += GetLangString(groupId, "AlreadyJoinedGame", firstName);
				await BotReply(groupId);
			}
		}

        public async Task PlayerList()
		{
			if (playerCount > 0)
			{
				botReply += playerCount + " players have joined this lobby :\r\n";

				foreach (KeyValuePair<long, City> kvp in cities)
				{
                    botReply += string.Format("*{0}* *{1}*\r\n", kvp.Value.playerDetails.firstName, kvp.Value.playerDetails.lastName);
				}

				await BotReply(groupId);
			}
			else
			{
				botReply += "No game has been hosted in this lobby yet.\r\nUse /joingame to make one!\r\n";
				await BotReply(groupId);
			}
		}

		public async Task PlayerLeave(long telegramId, string firstName, string lastName)
		{
			if (PlayerCheck(telegramId, firstName, lastName))
			{
				cities.Remove(telegramId);
				playerCount--;

                botReply += GetLangString(groupId, "LeaveGame");
				await BotReply(groupId);
			}
			else
			{
				botReply += firstName + " " + lastName + " hasn't join the lobby yet!\r\n";
				await BotReply(groupId);
			}
		}

		public int PlayerCount { get { return playerCount; } }

        #endregion

        public string GetLangString(long chatId, string key, params object[] args)
        {
            return Program.GetLangString(chatId, key, args);
        }

        async Task BotReply(long groupId, IReplyMarkup replyMarkup = null, ParseMode _parseMode = ParseMode.Markdown)
		{
			await Program.SendMessage(groupId, botReply, replyMarkup, _parseMode);
			botReply = ""; //Reset botReply string
            if (buttons != null)
            {
                buttons.Clear();
                menu = null;
            }
        }

        async Task PrivateReply(long groupId, IReplyMarkup replyMarkup = null, ParseMode _parseMode = ParseMode.Markdown)
        {
            await Program.SendMessage(groupId, privateReply, replyMarkup, _parseMode);
            privateReply = ""; //Reset botReply string
            if (buttons != null)
            {
                buttons.Clear();
                menu = null;
            }
        }
        async Task EditMessage(long chatId, int msgId, IReplyMarkup replyMarkup = null, ParseMode ParseMode = ParseMode.Markdown)
        {
            await Program.EditMessage(chatId, msgId, privateReply, repMarkup: replyMarkup, _parseMode: ParseMode);
            privateReply = ""; //Reset botReply string
            if (buttons != null)
            {
                buttons.Clear();
                menu = null;
            }
        }

        string ToBold(ref string thisString)
        {
            thisString = string.Format("*{0}*", thisString);
            return thisString;
        }
    }
}
