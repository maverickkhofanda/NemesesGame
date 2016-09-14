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
        /// What happens when 'Next Turn' triggered...
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
                    await PrivateReply(kvp.Key);
                    await BroadcastCityStatus();
                    botReply += GetLangString(groupId, "IteratePlayerCityName", city.playerDetails.firstName, city.playerDetails.cityName);
                }
            }
            else
            {
                await TimeUp();
                ResourceRegen();
                await BroadcastCityStatus();
            }

            botReply += GetLangString(groupId, "NextTurn", turn, turnInterval);
			await BotReply(groupId);

            //Insert turn implementation here
            await AskAction();
		}

        void CityStatus (City city)
        {
            privateReply += string.Format(
                    Program.GetLangString(groupId, "CurrentResources",
                    city.playerDetails.cityName,
                    city.cityResources.Gold, city.resourceRegen.Gold,
                    city.cityResources.Wood, city.resourceRegen.Wood,
                    city.cityResources.Stone, city.resourceRegen.Stone,
                    city.cityResources.Mithril, city.resourceRegen.Mithril));
            privateReply += string.Format(GetLangString(groupId, "NextTurn", turn, turnInterval));
        }

        void ResourceRegen ()
        {
            foreach (KeyValuePair<long, City> kvp in cities)
            {
                City city = kvp.Value;
                city.cityResources += city.resourceRegen;
            }
        }

        public async Task ChooseName(long PlayerId, string NewCityName)
        {
            cities[PlayerId].playerDetails.cityName = NewCityName;
            privateReply += GetLangString(groupId, "NameChosen", NewCityName);
            await PrivateReply(PlayerId);
        }

        #region Behind the scenes

        private void Timer(int timerInterval, ElapsedEventHandler elapsedEventHandler, bool timerEnabled = true)
        {
            _timer = new Timer();
            _timer.Elapsed += elapsedEventHandler;
            _timer.Interval = timerInterval * 1000;
            _timer.Enabled = true;
        }

        async Task BroadcastCityStatus()
        {
            foreach (KeyValuePair<long, City> kvp in cities)
            {
                City city = kvp.Value;
                CityStatus(city);

                await PrivateReply(kvp.Key);
            }
        }

        #endregion

        #region Inline Keyboard Interaction
        public async Task AskAction(long playerId = 0, int messageId = 0)
        {
            if (playerId != 0 && messageId != 0)
            {
                // this 'if' block can be used after one task has finished

                privateReply += GetLangString(groupId, "AskAction");
                //privateReply += GetLangString(groupId, "ThisTurn", turn);

                buttons.Add(new InlineKeyboardButton(GetLangString(groupId, "AssignTask"), $"AssignTask|{groupId}"));
                buttons.Add(new InlineKeyboardButton(GetLangString(groupId, "CityStatus"), $"YourStatus|{groupId}"));
                // no Back button
                menu = new InlineKeyboardMarkup(buttons.Select(x => new[] { x }).ToArray());

                //intentionally doubled
                cities[playerId].AddReplyHistory(menu, privateReply);
                cities[playerId].AddReplyHistory(menu, privateReply);

                await EditMessage(playerId, messageId, replyMarkup: menu);
            }
            else
            {
                foreach (KeyValuePair<long, City> kvp in cities)
                {
                    privateReply += GetLangString(groupId, "AskAction");
                    //privateReply += GetLangString(groupId, "ThisTurn", turn);

                    buttons.Add(new InlineKeyboardButton(GetLangString(groupId, "AssignTask"), $"AssignTask|{groupId}"));
                    buttons.Add(new InlineKeyboardButton(GetLangString(groupId, "CityStatus"), $"YourStatus|{groupId}"));
                    // no Back button
                    menu = new InlineKeyboardMarkup(buttons.Select(x => new[] { x }).ToArray());

                    //intentionally doubled
                    cities[kvp.Key].AddReplyHistory(menu, privateReply);
                    cities[kvp.Key].AddReplyHistory(menu, privateReply);

                    await PrivateReply(kvp.Key, replyMarkup: menu);
                }
            }
        }
        
        public async Task AssignTask(long playerId, int messageId)
        {
            privateReply += GetLangString(groupId, "AssignTask");
            //privateReply += GetLangString(groupId, "ThisTurn", turn);

            buttons.Add(new InlineKeyboardButton(GetLangString(groupId, "UpgradeProduction"), $"UpgradeProduction|{groupId}"));
            buttons.Add(new InlineKeyboardButton(GetLangString(groupId, "RaiseArmy"), $"RaiseArmy|{groupId}"));
            buttons.Add(new InlineKeyboardButton(GetLangString(groupId, "Back"), $"Back|{groupId}"));
            menu = new InlineKeyboardMarkup(buttons.Select(x => new[] { x }).ToArray());

            cities[playerId].AddReplyHistory(menu, privateReply);
            await EditMessage(playerId, messageId, replyMarkup: menu);
        }

        public async Task MyStatus(long playerId, int messageId)
        {
            City city = cities[playerId];
            CityStatus(city);

            await PrivateReply(playerId);
        }

        public async Task UpgradeProduction(long playerId, int messageId)
        {
            privateReply += GetLangString(groupId, "UpgradeProductionHeader");
            //privateReply += GetLangString(groupId, "ThisTurn", turn);

            // Creating strings for button, with all upgrades & current level
            string woodString = "";
            string stoneString = "";
            string mithrilString = "";

            // Generate woodString, stoneString, mithrilString
            for (byte index = 0; index < 3; index++)
            {
                ResourceType thisResourceType = ResourceType.Gold;
                string thisResourceString = "";
                string buttonString = "";
                string resourceLevels = "";
                string upgradeCost = "";
                Resources cost = new Resources(0, 0, 0, 0);

                if (index == 0)
                {
                    thisResourceType = ResourceType.Wood;
                    thisResourceString = GetLangString(groupId, "Wood");
                }
                else if (index == 1)
                {
                    thisResourceType = ResourceType.Stone;
                    thisResourceString = GetLangString(groupId, "Stone");
                }
                else if (index == 2)
                {
                    thisResourceType = ResourceType.Mithril;
                    thisResourceString = GetLangString(groupId, "Mithril");
                }

                byte totalLvls = (byte)refResources.ResourceRegen[thisResourceType].Length;
                byte currentLvl = cities[playerId].lvlResourceRegen[thisResourceType];

                if (currentLvl < totalLvls - 1)
                { cost = refResources.UpgradeCost[thisResourceType][currentLvl + 1]; }
                else { }

                for (byte i = 0; i < totalLvls; i++)
                {
                    string regen = refResources.ResourceRegen[thisResourceType][i].ToString();
                    
                    //Console.WriteLine("{0} regen level {1}: {2}", thisResourceString, i, regen);

                    if (currentLvl == i)
                    {
                        regen = "[" + regen + "]";
                    }
                    resourceLevels += regen;

                    if (i != totalLvls - 1)
                    {
                        resourceLevels += "/";
                    }
                }

                if (cost != new Resources(0,0,0,0))
                { upgradeCost = string.Format("{0}💰 {1}🌲 {2}🗿 {3}💎", cost.Gold, cost.Wood, cost.Stone, cost.Mithril); }
                else
                { upgradeCost = "Max Lvl"; }

                Console.WriteLine("upgradeCost({0}): {1}", thisResourceString, upgradeCost);

                buttonString = GetLangString(groupId, "ResourceUpgradePriceCost", thisResourceString, resourceLevels, upgradeCost);

                //output
                if (index == 0)
                {
                    woodString += buttonString;
                }
                else if (index == 1)
                {
                    stoneString += buttonString;
                }
                else if (index == 2)
                {
                    mithrilString += buttonString;
                }
            }

            /*// Unused code
            woodString = "Wood🌲: ";
            byte woodLength = (byte) refResources.ResourceRegen[ResourceType.Wood].Length;
            byte currentWoodLevel = cities[playerId].lvlResourceRegen[ResourceType.Wood];
            Resources woodUpgradeCost = refResources.UpgradeCost[ResourceType.Wood][currentWoodLevel];
            for (byte i = 0; i < woodLength; i++)
            {
                string regen = refResources.ResourceRegen[ResourceType.Wood][i].ToString();

                if (currentWoodLevel == i)
                {
					regen = "[" + regen + "]";
                }
				woodString += regen;
				if (i != woodLength - 1)
				{
					woodString += "/";
                }
            }
            woodString += string.Format("Cost: {0}💰 {1}🌲 {2}🗿 {3}💎", woodUpgradeCost.Gold, woodUpgradeCost.Wood, woodUpgradeCost.Stone, woodUpgradeCost.Mithril);

            stoneString = "Stone🗿: ";
            byte stoneLength = (byte)refResources.ResourceRegen[ResourceType.Stone].Length;
            for (byte i = 0; i < stoneLength; i++)
            {
                string regen = refResources.ResourceRegen[ResourceType.Stone][i].ToString();

                if (cities[playerId].lvlResourceRegen[ResourceType.Stone] == i)
                {
                    regen = "[" + regen + "]";
                }
                stoneString += regen;
                if (i != stoneLength - 1)
                {
                    stoneString += "/";
                }
            }

            mithrilString = "Mithril💎: ";
            byte mithrilLength = (byte)refResources.ResourceRegen[ResourceType.Mithril].Length;
            for (byte i = 0; i < mithrilLength; i++)
            {
                string regen = refResources.ResourceRegen[ResourceType.Mithril][i].ToString();

                if (cities[playerId].lvlResourceRegen[ResourceType.Mithril] == i)
                {
                    regen = "[" + regen + "]";
                }
                mithrilString += regen;
                if (i != mithrilLength - 1)
                {
                    mithrilString += "/";
                }
            }
            //end of iterating string creator
            */

            buttons.Add(new InlineKeyboardButton(woodString, $"resourceUpgrade|{groupId}|wood"));
            buttons.Add(new InlineKeyboardButton(stoneString, $"resourceUpgrade|{groupId}|stone"));
            buttons.Add(new InlineKeyboardButton(mithrilString, $"resourceUpgrade|{groupId}|mithril"));
            buttons.Add(new InlineKeyboardButton(GetLangString(groupId, "Back"), $"Back|{groupId}"));
            menu = new InlineKeyboardMarkup(buttons.Select(x => new[] { x }).ToArray());

            cities[playerId].AddReplyHistory(menu, privateReply);
            await EditMessage(playerId, messageId, replyMarkup: menu);
        }

        public async Task Back(long playerId, int messageId)
        {
            // intentionally doubled
            menu = cities[playerId].menuHistory.Pop();
            menu = cities[playerId].menuHistory.Pop();

            privateReply = cities[playerId].replyHistory.Pop();
            privateReply = cities[playerId].replyHistory.Pop();
            
            await EditMessage(playerId, messageId, replyMarkup: menu);
        }

        public async Task TimeUp()
        {
            foreach (KeyValuePair<long, City> kvp in cities)
            {
                privateReply += GetLangString(groupId, "TimeUp", turn - 1);
                await EditMessage(kvp.Key, kvp.Value.msgId);
            }
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
            string replyString = botReply;

            botReply = "";
			await Program.SendMessage(groupId, replyString, replyMarkup, _parseMode);
			replyString = "";

            if (buttons != null)
            {
                buttons.Clear();
                menu = null;
            }
        }

        async Task PrivateReply(long groupId, IReplyMarkup replyMarkup = null, ParseMode _parseMode = ParseMode.Markdown)
        {
            string replyString = privateReply;

            privateReply = "";
            await Program.SendMessage(groupId, replyString, replyMarkup, _parseMode);
            replyString = "";

            if (buttons != null)
            {
                buttons.Clear();
                menu = null;
            }
        }
        /// <summary>
        /// This only uses 'privateReply' (assuming no Edit needed in groups...)
        /// </summary>
        /// <param name="chatId"></param>
        /// <param name="msgId"></param>
        /// <param name="replyMarkup"></param>
        /// <param name="ParseMode"></param>
        /// <returns></returns>
        async Task EditMessage(long chatId, int msgId, IReplyMarkup replyMarkup = null, ParseMode ParseMode = ParseMode.Markdown)
        {
            string replyString = privateReply;

            privateReply = "";
            await Program.EditMessage(chatId, msgId, replyString, repMarkup: replyMarkup, _parseMode: ParseMode);
            replyString = "";

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
