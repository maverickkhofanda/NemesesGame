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
        //private string privateReply = "";
        //List<InlineKeyboardButton> buttons = new List<InlineKeyboardButton>();
        //InlineKeyboardMarkup menu;

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
                    CityChatHandler chat = kvp.Value.chat;
                    PlayerDetails pDetails = kvp.Value.playerDetails;

                    chat.AddReply(GetLangString(groupId, "StartGamePrivate", pDetails.cityName));
                    await chat.SendReply();
                    await BroadcastCityStatus();
                    botReply += GetLangString(groupId, "IteratePlayerCityName", pDetails.firstName, pDetails.cityName);
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
            await MainMenu();
		}

        void CityStatus (long playerId)
        {
            City city = cities[playerId];

            city.chat.AddReply(GetLangString(groupId, "CurrentResources",
                    city.playerDetails.cityName,
                    city._resources.Gold, city.resourceRegen.Gold,
                    city._resources.Wood, city.resourceRegen.Wood,
                    city._resources.Stone, city.resourceRegen.Stone,
                    city._resources.Mithril, city.resourceRegen.Mithril));
            city.chat.AddReply(GetLangString(groupId, "NextTurn", turn, turnInterval));
        }

        void ResourceRegen ()
        {
            foreach (KeyValuePair<long, City> kvp in cities)
            {
                City city = kvp.Value;
                city._resources += city.resourceRegen;
            }
        }

        public async Task ChooseName(long playerId, string NewCityName)
        {
            cities[playerId].playerDetails.cityName = NewCityName;
            cities[playerId].chat.AddReply(GetLangString(groupId, "NameChosen", NewCityName));

            await cities[playerId].chat.SendReply();
        }

        #region Game related functions

        public async Task ResourceUpgrade(long playerId, int messageId, string _resourceType)
        {
            byte curLevel = 99;
            CityChatHandler chat = cities[playerId].chat;

            ResourceType resourceType = (ResourceType)Enum.Parse(typeof(ResourceType), _resourceType);
            curLevel = cities[playerId].lvlResourceRegen[resourceType];

            // this variable is for containing the 'level' to be shown to the player, bcoz lvl 0 is not intuitive...
            byte textLevel;
            textLevel = curLevel;
            textLevel++;

            if (curLevel + 1 < refResources.UpgradeCost[resourceType].Length)
            {
                //Console.WriteLine("newLevel : {0}\r\n", curLevel);
                if (PayCost(ref cities[playerId]._resources, refResources.UpgradeCost[resourceType][++curLevel], playerId))
                {
                    // Increase the level & Update the new regen
                    cities[playerId].lvlResourceRegen[resourceType]++;
                    cities[playerId].UpdateRegen();

                    // readjusting the textLevel
                    textLevel = curLevel;
                    textLevel++;

                    chat.AddReply(GetLangString(groupId, "ResourceUpgraded", GetLangString(groupId, _resourceType), textLevel));
                }
                else
                {
                    // Send message failure
                    chat.AddReply(GetLangString(groupId, "ResourceUpgradeFailed", _resourceType, textLevel));
                }
            }
            else
            {
                chat.AddReply(GetLangString(groupId, "LvlMaxAlready", _resourceType));
            }

            CityStatus(playerId);
            // Back to main menu
            await MainMenu(playerId, messageId);
        }

        public async Task RaiseArmy(long playerId, int messageId, string _armyType = null, int _armyNumber = 0)
        {
            //ask which armyType
            CityChatHandler chat = cities[playerId].chat;

            // entering the RaiseArmy menu...
            if (_armyType == null && _armyNumber == 0)
            {
                //ask which armyType
                chat.AddReply(GetLangString(groupId, "AskRaiseArmyType"));

                foreach(KeyValuePair<ArmyType, int> kvp in cities[playerId]._army.ArmyNumber)
                {
                    string armyType = Enum.GetName(typeof(ArmyType), kvp.Key);

                    string buttonOutput = GetLangString(groupId, "ArmyPrice", 
                        GetLangString(groupId, armyType),
                        cities[playerId]._army.ArmyCost[kvp.Key]); // gets the price
                    chat.AddMenuButton(new InlineKeyboardButton(buttonOutput, $"RaiseArmy|{groupId}|{armyType}"));
                }
                chat.AddMenuButton(new InlineKeyboardButton(GetLangString(groupId, "Back"), $"Back|{groupId}"));

                chat.SetMenu();
                chat.AddReplyHistory();
                await chat.EditMessage();
            }
            else if (_armyType != null)
            {
                //ask how much army do you want to raise, give the price in the chat
                ArmyType armyType = (ArmyType) Enum.Parse(typeof(ArmyType), _armyType);
                int armyCost = cities[playerId]._army.ArmyCost[armyType];

                // ask the player to input army number
                if (_armyNumber == 0)
                {
                    chat.AddReply(GetLangString(groupId, "AskRaiseArmyNumber",
                    GetLangString(groupId, _armyType),
                    armyCost));

                    //lets give the player options : 100, 200, 300, 400, 500
                    for (int i = 100; i <= 500; i += 100)
                    {
                        string buttonOutput = string.Format("{0} ({1}💰)", i, armyCost * i);
                        chat.AddMenuButton(new InlineKeyboardButton(buttonOutput, $"RaiseArmy|{groupId}|{_armyType}|{i}"));
                    }
                    chat.AddMenuButton(new InlineKeyboardButton(GetLangString(groupId, "Back"), $"Back|{groupId}"));

                    chat.SetMenu();
                    chat.AddReplyHistory();
                    await chat.EditMessage();
                }
                // we got the army number... now process it
                else
                {
                    int goldCost = armyCost * _armyNumber;
                    Resources payCost = new Resources(goldCost, 0, 0, 0);
                    if (PayCost(ref cities[playerId]._resources, payCost, playerId))
                    {
                        cities[playerId]._army.ArmyNumber[armyType] += _armyNumber;
                        chat.AddReply(GetLangString(groupId, "RaiseArmySuccess",
                            _armyNumber,
                            GetLangString(groupId, _armyType)));

                        Console.WriteLine("{0} current number: {1}", armyType, cities[playerId]._army.ArmyNumber[armyType]);
                    }

                    CityStatus(playerId);
                    // Back to main menu
                    await MainMenu(playerId, messageId);
                }

                
            }

            

            //ask how much
        }

        #endregion

        #region Inline Keyboard Interaction
        public async Task MainMenu(long playerId = 0, int messageId = 0)
        {   
            if (playerId != 0 && messageId != 0)
            {
                // this 'if' block can be used after one task has finished
                SetMainMenu(playerId);

                await cities[playerId].chat.EditMessage();
            }
            else
            {
                foreach (KeyValuePair<long, City> kvp in cities)
                {
                    CityChatHandler chat = cities[kvp.Key].chat;
                    chat.ClearReplyHistory();
                    SetMainMenu(kvp.Key);

                    await chat.SendReply();
                }
            }
        }

        //this method is the child of and only used in MainMenu()
        void SetMainMenu(long playerId)
        {
            CityChatHandler chat = cities[playerId].chat;
            chat.AddReply(GetLangString(groupId, "MainMenu"));

            chat.buttons.Add(new InlineKeyboardButton(GetLangString(groupId, "AssignTask"), $"AssignTask|{groupId}"));
            chat.buttons.Add(new InlineKeyboardButton(GetLangString(groupId, "CityStatus"), $"YourStatus|{groupId}"));
            // no Back button
            chat.menu = new InlineKeyboardMarkup(chat.buttons.Select(x => new[] { x }).ToArray());

            //intentionally doubled ==> trying 1 reply history
            chat.AddReplyHistory();
            //chat.AddReplyHistory();
        }
        
        public async Task AssignTask(long playerId, int messageId)
        {
            CityChatHandler chat = cities[playerId].chat;

            chat.AddReply(GetLangString(groupId, "AssignTask"));

            chat.AddMenuButton(new InlineKeyboardButton(GetLangString(groupId, "UpgradeProduction"), $"UpgradeProduction|{groupId}"));
            chat.AddMenuButton(new InlineKeyboardButton(GetLangString(groupId, "RaiseArmy"), $"RaiseArmy|{groupId}"));
            chat.AddMenuButton(new InlineKeyboardButton(GetLangString(groupId, "Back"), $"Back|{groupId}"));
            chat.SetMenu();

            chat.AddReplyHistory();
            await chat.EditMessage();
        }

        public async Task MyStatus(long playerId, int messageId)
        {
            CityStatus(playerId);

            await cities[playerId].chat.SendReply();
        }

        public async Task UpgradeProduction(long playerId, int messageId)
        {
            CityChatHandler chat = cities[playerId].chat;

            chat.AddReply(GetLangString(groupId, "UpgradeProductionHeader"));

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
                        regen = "(" + regen + ")";
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

				//Console.WriteLine("upgradeCost({0}): {1}", thisResourceString, upgradeCost);
				Console.WriteLine("{0}\n", resourceLevels);
                chat.AddReply(GetLangString(groupId, "ResourceUpgradePriceCost", thisResourceString, resourceLevels, upgradeCost));
				buttonString = thisResourceString;

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
            //end of string generator
            

            chat.buttons.Add(new InlineKeyboardButton(woodString, $"ResourceUpgrade|{groupId}|Wood"));
            chat.buttons.Add(new InlineKeyboardButton(stoneString, $"ResourceUpgrade|{groupId}|Stone"));
            chat.buttons.Add(new InlineKeyboardButton(mithrilString, $"ResourceUpgrade|{groupId}|Mithril"));
            chat.buttons.Add(new InlineKeyboardButton(GetLangString(groupId, "Back"), $"Back|{groupId}"));
            chat.menu = new InlineKeyboardMarkup(chat.buttons.Select(x => new[] { x }).ToArray());

            chat.AddReplyHistory();
            await chat.EditMessage();
        }

        public async Task Back(long playerId, int messageId)
        {
            CityChatHandler chat = cities[playerId].chat;

            // intentionally doubled
            chat.menu = chat.menuHistory.Pop();
            chat.menu = chat.menuHistory.Pop();

            chat.privateReply = chat.replyHistory.Pop();
            chat.privateReply = chat.replyHistory.Pop();
            
            await chat.EditMessage();
        }

        public async Task TimeUp()
        {
            // let's kill this 2 times
            for (byte i = 0; i < 3; i++)
            {
                foreach (KeyValuePair<long, City> kvp in cities)
                {
                    kvp.Value.chat.AddReply(GetLangString(groupId, "TimeUp", turn - 1) 
                        + new string ('!', i));
                    await kvp.Value.chat.EditMessage();
                    
                }
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

        #region Behind the scenes

        private void Timer(int timerInterval, ElapsedEventHandler elapsedEventHandler, bool timerEnabled = true)
        {
            _timer = new Timer();
            _timer.Elapsed += elapsedEventHandler;
            _timer.Interval = timerInterval * 1000;
            _timer.Enabled = true;
        }

		private bool PayCost(ref Resources currentResource, Resources resourceCost, long playerId) // Pay resourceCost with currentResource
		{
            //Console.WriteLine("Current gold, wood, stone, mithril : {0}, {1}, {2}, {3}\r\n", currentResource.Gold, currentResource.Wood, currentResource.Stone, currentResource.Mithril);
            //Console.WriteLine("Upgrade gold, wood, stone, mithril cost : {0}, {1}, {2}, {3}\r\n", resourceCost.Gold, resourceCost.Wood, resourceCost.Stone, resourceCost.Mithril);
            
			// If currentResource is not enough
			if (currentResource < resourceCost)
			{
                // Resource not enough
                //Console.WriteLine("Not enough resources\r\n");
                cities[playerId].chat.AddReply(GetLangString(groupId, "NotEnoughResources"));
				return false;
			}
			else // currentResource is enough, deduct resourceCost from currentResource
			{
				currentResource = (currentResource - resourceCost);
				//Console.WriteLine("Current gold, wood, stone, mithril : {0}, {1}, {2}, {3}\r\n", currentResource.Gold, currentResource.Wood, currentResource.Stone, currentResource.Mithril);
				// Paid 'resourceCost'
				return true;
			}
		}

        async Task BroadcastCityStatus()
        {
            foreach (KeyValuePair<long, City> kvp in cities)
            {
                CityStatus(kvp.Key);

                await kvp.Value.chat.SendReply();
            }
        }

        #endregion


        public string GetLangString(long chatId, string key, params object[] args)
        {
            return Program.GetLangString(chatId, key, args);
        }

        /// <summary>
        /// Send message to group where game is running
        /// </summary>
        /// <param name="groupId"></param>
        /// <returns></returns>
        async Task BotReply(long groupId,/* IReplyMarkup replyMarkup = null, */ParseMode _parseMode = ParseMode.Markdown)
		{
            string replyString = botReply;

            botReply = "";
			await Program.SendMessage(groupId, replyString,/* replyMarkup,*/ _parseMode: _parseMode);
			replyString = "";

            /*// commented out because it doesn't seem replying to the group will use menu...
            if (buttons != null)
            {
                buttons.Clear();
                menu = null;
            }
            */
        }

        /*

        async Task SendReply(long groupId, IReplyMarkup replyMarkup = null, ParseMode _parseMode = ParseMode.Markdown)
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
        */
    }
}
