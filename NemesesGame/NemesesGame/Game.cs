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
			if (gameStatus == GameStatus.Hosted)
			{
				botReply += GetLangString(groupId, "StartGameGroup");
				await PlayerList();

				botReply += GetLangString(groupId, "AskChooseName", turnInterval);
				gameStatus = GameStatus.Starting;
				await BotReply(groupId);

				Timer(turnInterval, Turn);
			}
            else
			{
				botReply += GetLangString(groupId, "StartedGameGroup");
				await BotReply(groupId);
			}
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
            // not splitted to ResourceStatus and ArmyStatus yet
            City city = cities[playerId];

            city.chat.AddReply(GetLangString(groupId, "CurrentResources",
                    city.playerDetails.cityName,
                    city._resources.Gold, city.resourceRegen.Gold,
                    city._resources.Wood, city.resourceRegen.Wood,
                    city._resources.Stone, city.resourceRegen.Stone,
                    city._resources.Mithril, city.resourceRegen.Mithril));
            city.chat.AddReply(GetLangString(groupId, "NextTurn", turn, turnInterval));
        }

        void ArmyStatus (long playerId)
        {
            Army army = cities[playerId]._army;
            CityChatHandler chat = cities[playerId].chat;

            chat.AddReply(GetLangString(groupId, "CurrentArmyHeader"));

            for (int i = 0; i < army.Fronts.Length; i++)
            {
                if (army.Fronts[i].Number != 0)
                {
                    //int frontId = i + 1;

                    // in Base reply requires different parameters
                    // therefore, check if in Base
                    if (army.Fronts[i].State == ArmyState.Base)
                    {
                        string armyState = Enum.GetName(typeof(ArmyState), army.Fronts[i].State);

                        chat.AddReply(GetLangString(groupId, "CurrentArmy",
                            "",
                            GetLangString(groupId, armyState),
                            army.Fronts[i].Number));
                    }
                    else // not in Base, requires more parameter (check the .json file for reference)
                    {
                        string armyState = Enum.GetName(typeof(ArmyState), army.Fronts[i].State);
                        string frontTarget = cities[army.Fronts[i].TargetTelegramId].playerDetails.cityName;

                        chat.AddReply(GetLangString(groupId, "CurrentArmy",
                            i,
                            GetLangString(groupId, armyState, frontTarget),
                            army.Fronts[i].Number));
                    }
                }
                else
                {
                    break;
                }
            }
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
                    chat.AddReply(GetLangString(groupId, "ResourceUpgradeFailed", _resourceType, textLevel+1));
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

        public async Task RaiseArmy(long playerId, int messageId/*, string _armyType = null*/, int _armyNumber = 0)
        {
            //ask which armyType
            CityChatHandler chat = cities[playerId].chat;
            Army army = cities[playerId]._army;

            if (_armyNumber == 0)
            {
                // ask how many army to raise
                chat.AddReply(GetLangString(groupId, "AskRaiseArmyNumber", army.Cost));

                //lets give the player options : 100, 200, 300, 400, 500
                for (int i = 100; i <= 500; i += 100)
                {
                    string buttonOutput = string.Format("{0} ({1}💰)", i, army.Cost * i);
                    chat.AddMenuButton(new InlineKeyboardButton(buttonOutput, $"RaiseArmy|{groupId}|{i}"));
                }
                chat.AddMenuButton(new InlineKeyboardButton(GetLangString(groupId, "Back"), $"Back|{groupId}"));

                chat.SetMenu();
                chat.AddReplyHistory();
                await chat.EditMessage();
            }
            else
            {
                // now we got the armyNumber
                // check the price, raise the army, then return to menu
                
                int goldCost = army.Cost * _armyNumber;
                Resources payCost = new Resources(goldCost, 0, 0, 0);
                Console.WriteLine("goldCost: " + goldCost);

                if (PayCost(ref cities[playerId]._resources, payCost, playerId))
                {
                    army.Fronts[0].Number += _armyNumber;

                    chat.AddReply(GetLangString(groupId, "RaiseArmySuccess", _armyNumber));
                    Console.WriteLine("Raise Army Success: " + chat.privateReply);
                }

                CityStatus(playerId);
                ArmyStatus(playerId);
                // Back to main menu
                await MainMenu(playerId, messageId);

            }

            /* revamp Army... this code is not used
            // entering the RaiseArmy menu...
            if (_armyType == null && _armyNumber == 0)
            {
                //ask which armyType
                chat.AddReply(GetLangString(groupId, "AskRaiseArmyType"));

                foreach(KeyValuePair<ArmyType, int> kvp in cities[playerId]._army.TypeNumber)
                {
                    string armyType = Enum.GetName(typeof(ArmyType), kvp.Key);

                    string buttonOutput = GetLangString(groupId, "ArmyPrice", 
                        GetLangString(groupId, armyType),
                        cities[playerId]._army.Cost[kvp.Key]); // gets the price
                    chat.AddMenuButton(new InlineKeyboardButton(buttonOutput, $"RaiseArmy|{groupId}|{armyType}"));
                }
                chat.AddMenuButton(new InlineKeyboardButton(GetLangString(groupId, "Back"), $"Back|{groupId}"));

                chat.SetMenu();
                chat.AddReplyHistory();
                await chat.EditMessage();
            }
            else
            {
                //ask how much army do you want to raise, give the price in the chat
                ArmyType armyType = (ArmyType) Enum.Parse(typeof(ArmyType), _armyType);
                int armyCost = cities[playerId]._army.Cost[armyType];

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
                        cities[playerId]._army.TypeNumber[armyType] += _armyNumber;
                        chat.AddReply(GetLangString(groupId, "RaiseArmySuccess",
                            _armyNumber,
                            GetLangString(groupId, _armyType)));

                        Console.WriteLine("{0} current number: {1}", armyType, cities[playerId]._army.TypeNumber[armyType]);
                    }

                    CityStatus(playerId);
                    // Back to main menu
                    await MainMenu(playerId, messageId);
                }
            }
            */

        }

        public async Task Attack(long playerId, int messageId, long targetId = 0, byte frontId = 0, int deployPercent = 0)
        {
            CityChatHandler chat = cities[playerId].chat;

            // ask attack which country ---------------------------------------------------
            if (targetId == 0)
            {
                chat.AddReply(GetLangString(groupId, "ChooseOtherPlayer"));

                foreach(KeyValuePair<long, City> kvp in cities)
                {
                    // don't switch out the sender yet... for testing purposes
                    PlayerDetails pDetails = kvp.Value.playerDetails;

                    // iterate all fronts
                    Army targetArmy = kvp.Value._army;
                    for(int i = 0; i < targetArmy.Fronts.Count(); i++)
                    {
                        ArmyFront front = targetArmy.Fronts[i];

                        // check if armyFront is empty
                        if (front.Number != 0)
                        {
                            // if army in base, enemy can't see his number. Otherwise, it's visible
                            if (front.State == ArmyState.Base)
                            {
                                string buttonOutput = string.Format("{0} ({1}) {2}", pDetails.cityName, pDetails.firstName, GetLangString(groupId, "Base"));
                                chat.AddMenuButton(new InlineKeyboardButton(buttonOutput, $"Attack|{groupId}|{pDetails.telegramId}|{i}"));
                            }
                            else
                            {
                                string targetState = Enum.GetName(typeof(ArmyState), front.State);
                                string targetsTarget = cities[front.TargetTelegramId].playerDetails.cityName;
                                int targetNumber = front.Number;

                                string buttonOutput = string.Format("{0} {1} ({2}🗡)",
                                    pDetails.cityName,
                                    GetLangString(groupId, targetState, targetsTarget),
                                    targetNumber);

                                chat.AddMenuButton(new InlineKeyboardButton(buttonOutput, $"Attack|{groupId}|{pDetails.telegramId}|{i}"));
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                    
                }
                chat.AddMenuButton(new InlineKeyboardButton(GetLangString(groupId, "Back"), $"Back|{groupId}"));

                chat.SetMenu();

                chat.AddReplyHistory();
                await chat.EditMessage();
            }
            else
            {
                Army army = cities[playerId]._army;

                // ask how many to deploy ---------------------------------------------------------------------------------
                if (deployPercent == 0)
                {
                    // show how many troops you have
                    chat.AddReply(GetLangString(groupId, "DeployTroopNumber"));
                    chat.AddReply(GetLangString(groupId, "CurrentDefendingArmy", army.Fronts[0].Number));
                    
                    // ask how many percentage of your current defending army do you want to unleash
                    for (int i = 10; i < 100; i+= 10)
                    {
                        // need to format this to 2-columns
                        string buttonOutput = string.Format("{0}%", i);
                        chat.AddMenuButton(new InlineKeyboardButton(buttonOutput, $"Attack|{groupId}|{targetId}|{frontId}|{i}"));
                    }
                    chat.AddMenuButton(new InlineKeyboardButton(GetLangString(groupId, "Back"), $"Back|{groupId}"));
                    chat.SetMenu();

                    chat.AddReplyHistory();
                    await chat.EditMessage();

                    /* revamp army...
                    foreach (KeyValuePair<ArmyType, int> _type in cities[playerId]._army.TypeNumber)
                    {
                        string typeName = Enum.GetName(typeof(ArmyType), _type.Key);
                        chat.AddReply(string.Format("*{0}* *{1}*\r\n", _type.Value, GetLangString(groupId, typeName)));
                    }
                    //Console.WriteLine(chat.privateReply);
                    */
                }
                // get the attack ordered ---------------------------------------------------------------------------------
                else
                {
                    // TODO : Insert news in Group
                    DeployArmy(playerId, targetId, frontId, deployPercent);
                    Console.WriteLine("Reply to {0}: {1} ", playerId, chat.privateReply);

                    ArmyStatus(playerId);

                    // Back to main menu
                    await MainMenu(playerId, messageId);
                }

            }

            // ask how many troops to deploy
        }

        #endregion

        #region Inline Keyboard Interaction
        public async Task MainMenu(long playerId = 0, int messageId = 0)
        {   
            if (playerId != 0 && messageId != 0)
            {
                // this 'if' block can be used after one task has finished
                CityChatHandler chat = cities[playerId].chat;
                chat.ClearReplyHistory();

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
            chat.buttons.Add(new InlineKeyboardButton(GetLangString(groupId, "Attack"), $"Attack|{groupId}"));
            // no Back button
            chat.menu = new InlineKeyboardMarkup(chat.buttons.Select(x => new[] { x }).ToArray());

            //intentionally doubled ==> trying 1 reply history
            chat.AddReplyHistory();
            //chat.AddReplyHistory();
        }
        
        public async Task AssignTask(long playerId, int messageId)
        {
            CityChatHandler chat = cities[playerId].chat;

            CityStatus(playerId);
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

            SetMainMenu(playerId);
            await cities[playerId].chat.EditMessage();
        }

        public async Task UpgradeProduction(long playerId, int messageId)
        {
            CityChatHandler chat = cities[playerId].chat;

            chat.AddReply(GetLangString(groupId, "UpgradeProductionHeader"));

            // Creating strings for button, with all upgrades & current level
            string woodString = "";
            string stoneString = "";
            string mithrilString = "";
            byte currentLvl;

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
                currentLvl = cities[playerId].lvlResourceRegen[thisResourceType];

                if (currentLvl < totalLvls - 1)
                { cost = refResources.UpgradeCost[thisResourceType][currentLvl + 1]; }

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
                { upgradeCost = string.Format("*{0}*💰 *{1}*🌲 *{2}*🗿 *{3}*💎", cost.Gold, cost.Wood, cost.Stone, cost.Mithril); }
                else
                { upgradeCost = "Max Lvl"; }

				//Console.WriteLine("upgradeCost({0}): {1}", thisResourceString, upgradeCost);
				
                chat.AddReply(GetLangString(groupId, "ResourceUpgradePriceCost", thisResourceString, currentLvl+1 ,upgradeCost));
				buttonString = thisResourceString + " : " + resourceLevels;

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

            if (chat.menuHistory.Count == chat.backCount)
            {
                chat.menu = chat.menuHistory.Pop();
                chat.menu = chat.menuHistory.Pop();

                chat.privateReply = chat.replyHistory.Pop();
                chat.privateReply = chat.replyHistory.Pop();
            }
            else
            {
                chat.menu = chat.menuHistory.Pop();
                chat.privateReply = chat.replyHistory.Pop();
            }
            
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
        
        void DeployArmy(long playerId, long targetId, byte targetFrontId, int deployPercent)
        {
            Army army = cities[playerId]._army;
            CityChatHandler chat = cities[playerId].chat;

            // find new empty front ------------------------------------------------------------
            byte i = 0;
            for (i = 0; i < army.Fronts.Count(); i++)
            {
                if (army.Fronts[i].Number == 0)
                {
                    // Add the new army
                    float f = army.Fronts[0].Number * (deployPercent * 0.01f);
                    int armyDeployed = (int) f;

                    army.Fronts[i].Number = armyDeployed;

                    // Remove army from base's army
                    army.Fronts[0].Number -= armyDeployed;
                    break;
                }
            }
            // check if no front available
            if (i >= army.Fronts.Count())
            {
                // tell player: you have maximum front
                chat.AddReply(GetLangString(groupId, "FrontMaxNumber"));
                return;
            }

            //Console.WriteLine("newFront: {0} {1}: {2}🗡", playerId, i, army.Fronts[i].Number);
            // deploy the front ----------------------------------------------------------------
            army.StartMarch(i, targetId, targetFrontId);
            
            ArmyFront front = army.Fronts[i];
            Console.WriteLine("#001 work");
            Console.WriteLine("front.TargetTelegramId: " + front.TargetTelegramId);
            PlayerDetails targetDetails = cities[front.TargetTelegramId].playerDetails; // Error
            Console.WriteLine("#002 work");
            ArmyFront targetFront = cities[front.TargetTelegramId]._army.Fronts[targetFrontId];

            Console.WriteLine("{0}'s army marching to {1} with {2} troops", playerId, targetDetails.cityName, front.Number);

            // if target is base
            if (targetFront.State == ArmyState.Base)
            {
                chat.AddReply(GetLangString(groupId, "ArmyMarchEnemyBase", targetDetails.cityName, front.Number));
                Console.WriteLine(chat.privateReply);
            }
            else
            {
                chat.AddReply(GetLangString(groupId, "ArmyMarchEnemyFront", targetDetails.cityName, front.Number, targetFront.Number));
            }
        }

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
