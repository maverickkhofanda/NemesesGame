using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Timers;
using NemesesGame;

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
        MerchantGlobal merchantGlobal;
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
				gameStatus = GameStatus.Starting;
				botReply += GetLangString(groupId, "StartGameGroup");
				await PlayerList();

				botReply += GetLangString(groupId, "AskChooseName", turnInterval);
				await BotReply();

                // create new list for numbering player for Merchant journey
                long[] playerIdArray = new long[cities.Count];
                byte i = 0;
                foreach (KeyValuePair<long, City> kvp in cities)
                {
                    playerIdArray[i] = kvp.Key;
                    i++;
                }
                merchantGlobal = new MerchantGlobal(playerIdArray);

                Timer(turnInterval, Turn);
			}
            else
			{
				botReply += GetLangString(groupId, "StartedGameGroup");
				await BotReply();
			}
		}

		/// <summary>
		/// What happens when 'Next Turn' triggered...
		/// </summary>
		public async void Turn(object sender, ElapsedEventArgs e)
        {
            turn++;

            if(turn == 1)
            {
                gameStatus = GameStatus.InGame;
                botReply += GetLangString(groupId, "PlayerListHeader");

                foreach (KeyValuePair<long, City> kvp in cities)
                {
                    CityChatHandler chat = kvp.Value.chat;
                    PlayerDetails pDetails = kvp.Value.playerDetails;

                    chat.AddReply(ReplyType.status, GetLangString(groupId, "StartGamePrivate", pDetails.cityName));
                    // await chat.SendReply();
                    // BroadcastCityStatus();

                    botReply += GetLangString(groupId, "IteratePlayerCityName", pDetails.firstName, pDetails.cityName);
                }
                
                await MainMenu();
            }
            else
            {
				// end this turn
                // try not using TimeUp()
				await TimeUp();


                // Refresh chat
                foreach (KeyValuePair<long, City> kvp in cities)
                {
                    // clear everythingggg
                    CityChatHandler chat = kvp.Value.chat;
                    chat.ClearReply(ReplyType.status);
                }

                // turn actions
                ResourceRegen();
                merchantGlobal.UpdateDemandSupply();
                March();
                
                await MainMenu();

				// new turn actions
				// News();
			}

            botReply += GetLangString(groupId, "NextTurn", turn, turnInterval);
			await BotReply();

            //Insert turn implementation here
            
		}

        void CityStatus (long playerId)
        {
            // not splitted to ResourceStatus and ArmyStatus yet
            City city = cities[playerId];

            city.chat.AddReply(ReplyType.status, GetLangString(groupId, "CurrentResources",
                    city.playerDetails.cityName,
                    city._resources.Gold, city.resourceRegen.Gold,
                    city._resources.Wood, city.resourceRegen.Wood,
                    city._resources.Stone, city.resourceRegen.Stone,
                    city._resources.Mithril, city.resourceRegen.Mithril));

            ArmyStatus(playerId);
            
            city.chat.AddReply(ReplyType.status, GetLangString(groupId, "NextTurn", turn, turnInterval));
        }

        void ArmyStatus (long playerId)
        {
            Army army = cities[playerId]._army;
            CityChatHandler chat = cities[playerId].chat;

            chat.AddReply(ReplyType.status, GetLangString(groupId, "CurrentArmyHeader"));

            for (int i = 0; i < army.Fronts.Length; i++)
            {
				if (army.Fronts[i] != null)
                {
                    //int frontId = i + 1;

                    // in Base reply requires different parameters
                    // therefore, check if in Base
                    if (army.Fronts[i].State == ArmyState.Base)
                    {
                        string armyState = Enum.GetName(typeof(ArmyState), army.Fronts[i].State);

                        chat.AddReply(ReplyType.status, GetLangString(groupId, "CurrentArmyBase",
                            "",
                            GetLangString(groupId, armyState),
                            army.Fronts[i].Number));
                    }
                    else // not in Base, requires more parameter (check the .json file for reference)
                    {
                        string armyState = Enum.GetName(typeof(ArmyState), army.Fronts[i].State);
                        string frontTarget = cities[army.Fronts[i].TargetTelegramId].playerDetails.cityName;

                        chat.AddReply(ReplyType.status, GetLangString(groupId, "CurrentArmyNotBase",
                            i,
                            GetLangString(groupId, armyState, frontTarget),
                            army.Fronts[i].Number,
							army.Fronts[i].MarchLeft));
                    }
                }
                else
                {
                    break;
                }
            }
        }
        
        #region Resource Actions

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

			chat.ClearReply(ReplyType.status);


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

                    chat.AddReply(ReplyType.status, GetLangString(groupId, "ResourceUpgraded", GetLangString(groupId, _resourceType), textLevel));
                }
                else
                {
                    // Send message failure
                    chat.AddReply(ReplyType.status, GetLangString(groupId, "ResourceUpgradeFailed", _resourceType, textLevel+1));
                }
            }
            else
            {
                chat.AddReply(ReplyType.status, GetLangString(groupId, "LvlMaxAlready", _resourceType));
            }

            //CityStatus(playerId);
            // Back to main menu
            await MainMenu(playerId, messageId);
        }

        void ResourceRegen()
        {
            foreach (KeyValuePair<long, City> kvp in cities)
            {
                City city = kvp.Value;
                city._resources += city.resourceRegen;
            }
        }

        #endregion

        #region Army Actions

        public async Task RaiseArmy(long playerId, int messageId/*, string _armyType = null*/, int _armyNumber = 0)
        {
            //ask which armyType
            CityChatHandler chat = cities[playerId].chat;
            Army army = cities[playerId]._army;

			if (_armyNumber == 0)
            {
                // ask how many army to raise
                chat.EditReply(ReplyType.command, GetLangString(groupId, "AskRaiseArmyNumber", army.Cost));

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

				chat.ClearReply(ReplyType.status);

				if (PayCost(ref cities[playerId]._resources, payCost, playerId))
                {
                    army.Fronts[0].Number += _armyNumber;

                    chat.AddReply(ReplyType.status, GetLangString(groupId, "RaiseArmySuccess", _armyNumber));
                }

                //CityStatus(playerId);
                ArmyStatus(playerId);
                // Back to main menu
                await MainMenu(playerId, messageId);

            }
        }

        public async Task Attack(long playerId, int messageId, long targetId = 0, byte frontId = 0, int deployPercent = 0)
        {
            CityChatHandler chat = cities[playerId].chat;

            // ask attack which country ---------------------------------------------------
            if (targetId == 0)
            {
                chat.EditReply(ReplyType.command, GetLangString(groupId, "ChooseOtherPlayer"));

                foreach (KeyValuePair<long, City> kvp in cities)
                {
                    // don't switch out the sender yet... for testing purposes
                    PlayerDetails pDetails = kvp.Value.playerDetails;

                    // iterate all fronts
                    Army targetArmy = kvp.Value._army;
                    for (int i = 0; i < targetArmy.Fronts.Count(); i++)
                    {
                        ArmyFront front = targetArmy.Fronts[i];

                        // check if armyFront is null
                        if (front != null)
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
                    chat.AddReply(ReplyType.command, GetLangString(groupId, "DeployTroopNumber"));
                    chat.AddReply(ReplyType.command, GetLangString(groupId, "CurrentDefendingArmy", army.Fronts[0].Number));

                    // ask how many percentage of your current defending army do you want to unleash
                    for (int i = 10; i < 100; i += 10)
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

					chat.ClearReply(ReplyType.status);

					//ArmyStatus(playerId);

                    // Back to main menu
                    await MainMenu(playerId, messageId);
                }

            }

            // ask how many troops to deploy
        }
		
        void DeployArmy(long playerId, long targetId, byte targetFrontId, int deployPercent)
        {
            Army army = cities[playerId]._army;
            CityChatHandler chat = cities[playerId].chat;

            // find new empty front ------------------------------------------------------------
            byte i = 0;
            for (i = 0; i < army.Fronts.Count(); i++)
            {
                if (army.Fronts[i] == null)
                {
                    // Add the new army
                    float f = army.Fronts[0].Number * (deployPercent * 0.01f);
                    int armyDeployed = (int)f;

                    army.Fronts[i] = new ArmyFront(ArmyState.Base, armyDeployed);
                    
                    // Remove army from base's army
                    army.Fronts[0].Number -= armyDeployed;
                    break;
                }
            }
            // check if no front available
            if (i >= army.Fronts.Count())
            {
                // tell player: you have maximum front
                chat.AddReply(ReplyType.status, GetLangString(groupId, "FrontMaxNumber"));
                return;
            }

            //Console.WriteLine("newFront: {0} {1}: {2}🗡", playerId, i, army.Fronts[i].Number);
            // deploy the front ----------------------------------------------------------------
            army.StartMarch(i, targetId, targetFrontId);

            ArmyFront front = army.Fronts[i];
            PlayerDetails targetDetails = cities[front.TargetTelegramId].playerDetails;
            ArmyFront targetFront = cities[front.TargetTelegramId]._army.Fronts[targetFrontId];

            Console.WriteLine("{0}'s army marching to {1} with {2} troops", playerId, targetDetails.cityName, front.Number);

            // if target is base
            if (targetFront.State == ArmyState.Base)
            {
                chat.AddReply(ReplyType.status, GetLangString(groupId, "ArmyMarchEnemyBase", targetDetails.cityName, front.Number));
            }
            else
            {
                chat.AddReply(ReplyType.status, GetLangString(groupId, "ArmyMarchEnemyFront", targetDetails.cityName, front.Number, targetFront.Number));
            }
        }

        void March()
        {
            // find each marching front

            // iterate each city
            foreach(KeyValuePair<long, City> kvp in cities)
            {
                // then iterate each fronts
                for (byte i = 0; i < 10; i++)
                {
                    // if front have no army, break to next city
                    if (kvp.Value._army.Fronts[i] != null)
                    {
                        ArmyFront front = kvp.Value._army.Fronts[i];
						ArmyFront[] fronts = kvp.Value._army.Fronts;

						// if march = 0 (aka. reached the destination), do the thing
						if (front.State == ArmyState.March)
                        {
                            front.MarchLeft--;

                            if(front.MarchLeft == 0)
                            {
                                ArmyState targetState = GetFront(front.TargetTelegramId, front.TargetFrontId).State;
                                
                                if (targetState == ArmyState.Base)
                                {
                                    Invade(kvp.Key, i, front.TargetTelegramId, front.TargetFrontId);
                                }
                                else
                                {
                                    Intercept(kvp.Key, i, front.TargetTelegramId, front.TargetFrontId);
                                }
                            }
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }
        
        void Invade(long atkId, byte atkFrontId, long defId, byte defFrontId)
        {
            ArmyFront offFront = cities[atkId]._army.Fronts[atkFrontId];
            ArmyFront defFront = cities[defId]._army.Fronts[defFrontId];

			CityChatHandler atkChat = cities[atkId].chat;
			CityChatHandler defChat = cities[defId].chat;

			// First, check if the offFront still existed (Not intercepted)
			if (offFront == null)
			{
				// offFront doesn't exist, intercepted by someone
				atkChat.AddReply(ReplyType.status,
				   GetLangString(groupId, "InvaderInterceptedPrivate"));

				return;
			}

			Console.WriteLine("-Invade-\r\nAttacker : {0}\r\nDefender : {1}", atkId, defId);

            float defaultBase = 0.35f;
            float widthOffset = 0.25f;

            float atkCP = offFront.CombatPower;
            float defCP = defFront.CombatPower;
            float atkNumber = offFront.Number;
            float defNumber = defFront.Number;

            // let's give 'outcome' a max number of 100% (or 1)
            float outcome = atkCP >= defCP ?
                (atkCP - defCP) / atkCP :
                - ((defCP - atkCP) / defCP); // if 'defCP' is higher, make it minus

			Console.WriteLine("Outcome : {0}", outcome);

            // let's do linear...
            float atkCasualtyPct = defaultBase - (widthOffset * outcome);
            float defCasualtyPct = defaultBase + (widthOffset * outcome);

            /* wrong desired graph
            float atkCasualtyPct = defaultBase + ((float)Math.Pow(outcome, 3) - (widthOffset * outcome));
            float defCasualtyPct = defaultBase - ((float)Math.Pow(outcome, 3) - (widthOffset * outcome));
            */

            Console.WriteLine("atkCasualtyPct: " + atkCasualtyPct);
            Console.WriteLine("defCasualtyPct: " + defCasualtyPct);

            float atkCasualtyTemp = atkNumber * atkCasualtyPct;
            float defCasualtyTemp = defNumber * defCasualtyPct;

			int atkCasualty = (int)atkCasualtyTemp;
			int defCasualty = (int)defCasualtyTemp;

			offFront.Number -= atkCasualty;
            defFront.Number -= defCasualty;

            // now, we send the outcomes news...

            PlayerDetails atkPD = cities[atkId].playerDetails;
            PlayerDetails defPD = cities[defId].playerDetails;

            //win loss
            if (outcome > 0.1 )
            {
                // invader win
                botReply += GetLangString(groupId, "InvaderWinBroadcast", atkPD.cityName, defPD.cityName);

                // chat details to players
                atkChat.AddReply(ReplyType.status, 
                    GetLangString(groupId, "InvaderWinPrivate", defPD.cityName, (int)(outcome * 100), atkCasualty, offFront.Number));
                defChat.AddReply(ReplyType.status,
                    GetLangString(groupId, "DefenderLosePrivate", atkPD.cityName, (int)(outcome * -100), defCasualty, defFront.Number));

            }
            else if (outcome < -0.1 )
            {
                // defender win
                botReply += GetLangString(groupId, "DefenderWinBroadcast", defPD.cityName, atkPD.cityName);

                // chat details to players
                atkChat.AddReply(ReplyType.status,
                    GetLangString(groupId, "InvaderLostPrivate", defPD.cityName, (int)(outcome * 100), atkCasualty, offFront.Number));
                defChat.AddReply(ReplyType.status,
                    GetLangString(groupId, "DefenderWinPrivate", atkPD.cityName, (int)(outcome * -100), defCasualty, defFront.Number));
            }
            else
            {
                // its a tie
                botReply += GetLangString(groupId, "InvadeTieBroadcast", defPD.cityName, atkPD.cityName);

                // chat details to players
                atkChat.AddReply(ReplyType.status,
                    GetLangString(groupId, "InvaderTiePrivate", defPD.cityName, atkCasualty, offFront.Number));
                defChat.AddReply(ReplyType.status,
                    GetLangString(groupId, "DefenderTiePrivate", atkPD.cityName, defCasualty, defFront.Number));
            }

			// Resolve
			// If there is army left, send it back to base
			if (offFront.Number > 0)
			{
				ArmyFront frontBase = cities[atkId]._army.Fronts[0];

				frontBase.Number += offFront.Number;
			}

			// Remove current attacking front
			ArmyFront[] fronts = cities[atkId]._army.Fronts;
			fronts[atkFrontId] = null;
		}

		void Intercept(long atkId, byte atkFrontId, long defId, byte defFrontId)
		{
			ArmyFront offFront = cities[atkId]._army.Fronts[atkFrontId];
			ArmyFront defFront = cities[defId]._army.Fronts[defFrontId];

			Console.WriteLine("-Intercept-\r\nAttacker : {0}\r\nDefender : {1}", atkId, defId);

			float defaultBase = 0.35f;
			float widthOffset = 0.25f;

			float atkCP = offFront.CombatPower;
			float defCP = defFront.CombatPower;
			float atkNumber = offFront.Number;
			float defNumber = defFront.Number;

			// let's give 'outcome' a max number of 100% (or 1)
			float outcome = atkCP >= defCP ?
				(atkCP - defCP) / atkCP :
				-((defCP - atkCP) / defCP); // if 'defCP' is higher, make it minus


            Console.WriteLine("Outcome : {0}", outcome);

            // let's do linear...
            float atkCasualtyPct = defaultBase - (widthOffset * outcome);
            float defCasualtyPct = defaultBase + (widthOffset * outcome);

            /* wrong desired graph
            float atkCasualtyPct = defaultBase + ((float)Math.Pow(outcome, 3) - (widthOffset * outcome));
            float defCasualtyPct = defaultBase - ((float)Math.Pow(outcome, 3) - (widthOffset * outcome));
            */

            Console.WriteLine("atkCasualtyPct: " + atkCasualtyPct);
            Console.WriteLine("defCasualtyPct: " + defCasualtyPct);

            float atkCasualtyTemp = atkNumber * atkCasualtyPct;
            float defCasualtyTemp = defNumber * defCasualtyPct;

            int atkCasualty = (int)atkCasualtyTemp;
            int defCasualty = (int)defCasualtyTemp;

            offFront.Number -= atkCasualty;
            defFront.Number -= defCasualty;

            // now, we send the outcomes news...

            PlayerDetails atkPD = cities[atkId].playerDetails;
            PlayerDetails defPD = cities[defId].playerDetails;

            CityChatHandler atkChat = cities[atkId].chat;
            CityChatHandler defChat = cities[defId].chat;

            //win loss
            ArmyFront offFrontBase = cities[atkId]._army.Fronts[0];
            ArmyFront defFrontBase = cities[defId]._army.Fronts[0];
            ArmyFront[] offFronts = cities[atkId]._army.Fronts;
            ArmyFront[] defFronts = cities[defId]._army.Fronts;

            if (outcome > 0.1)
            {
                // intercepter win
                botReply += GetLangString(groupId, "InterceptWinBroadcast", atkPD.cityName, defPD.cityName);

                // chat details to players
                atkChat.AddReply(ReplyType.status,
                    GetLangString(groupId, "InterceptWinPrivate", defPD.cityName, (int)(outcome * 100), atkCasualty, offFront.Number));
                defChat.AddReply(ReplyType.status,
                    GetLangString(groupId, "InterceptDefenderLosePrivate", atkPD.cityName, (int)(outcome * -100), defCasualty, defFront.Number));

                // Resolve
                // Offender & Defender returns to base
                offFrontBase.Number += offFront.Number;
                defFrontBase.Number += defFront.Number;
                offFronts[atkFrontId] = null;
                defFronts[defFrontId] = null;
            }
            else if (outcome < -0.1)
            {
                // defender win
                botReply += GetLangString(groupId, "InterceptDefenderWinBroadcast", defPD.cityName, atkPD.cityName);

                // chat details to players
                atkChat.AddReply(ReplyType.status,
                    GetLangString(groupId, "InterceptLostPrivate", defPD.cityName, (int)(outcome * 100), atkCasualty, offFront.Number));
                defChat.AddReply(ReplyType.status,
                    GetLangString(groupId, "InterceptDefenderWinPrivate", atkPD.cityName, (int)(outcome * -100), defCasualty, defFront.Number));

                // Resolve
                // Only the Offender returns to base
                offFrontBase.Number += offFront.Number;
                offFronts[atkFrontId] = null;
            }
            else
            {
                // its a tie
                botReply += GetLangString(groupId, "InterceptTieBroadcast", defPD.cityName, atkPD.cityName);

                // chat details to players
                atkChat.AddReply(ReplyType.status,
                    GetLangString(groupId, "InterceptTiePrivate", defPD.cityName, atkCasualty, offFront.Number));
                defChat.AddReply(ReplyType.status,
                    GetLangString(groupId, "InterceptDefenderTiePrivate", atkPD.cityName, defCasualty, defFront.Number));

                // Resolve
                // Offender & Defender returns to base
                offFrontBase.Number += offFront.Number;
                defFrontBase.Number += defFront.Number;
                offFronts[atkFrontId] = null;
                defFronts[defFrontId] = null;
            }
        }

		#endregion
        
        #region Merchant Actions

        public async Task Merchant(long playerId, int messageId, string action = "", string materialType = "", int amountOrdered = 0)
        {
            CityChatHandler chat = cities[playerId].chat;
            ResourceType[] mtrlType = { ResourceType.Wood, ResourceType.Stone, ResourceType.Mithril };

            if (action == "")
            {
                chat.EditReply(ReplyType.command, GetLangString(groupId, "MerchantGreeting"));

                // add current price ticker
                foreach (ResourceType r in mtrlType)
                {
                    chat.AddReply(ReplyType.command,
                        string.Format("*{0}*: *{1}*💰 / *{2}*💰\r\n",
                            GetLangString(groupId, Enum.GetName(typeof(ResourceType), r)),
                            merchantGlobal.BuyPrice[r],
                            merchantGlobal.SellPrice[r])
                        );
                }

                chat.buttons.Add(new InlineKeyboardButton(GetLangString(groupId, "Buy"), $"Merchant|{groupId}|Buy"));
                chat.buttons.Add(new InlineKeyboardButton(GetLangString(groupId, "Sell"), $"Merchant|{groupId}|Sell"));
                chat.AddMenuButton(new InlineKeyboardButton(GetLangString(groupId, "Back"), $"Back|{groupId}"));
                chat.SetMenu();

                chat.AddReplyHistory();
                await chat.EditMessage();
            }
            else
            {
                if (materialType == "")
                {
                    // ask which resource to buy/sell
                    // current price shown at the menu before

                    chat.EditReply(ReplyType.command, GetLangString(groupId, "MerchantAskMaterial"));
                    chat.buttons.Add(new InlineKeyboardButton(GetLangString(groupId, "Wood"), $"Merchant|{groupId}|{action}|Wood"));
                    chat.buttons.Add(new InlineKeyboardButton(GetLangString(groupId, "Stone"), $"Merchant|{groupId}|{action}|Stone"));
                    chat.buttons.Add(new InlineKeyboardButton(GetLangString(groupId, "Mithril"), $"Merchant|{groupId}|{action}|Mithril"));
                    chat.AddMenuButton(new InlineKeyboardButton(GetLangString(groupId, "Back"), $"Back|{groupId}"));

                    chat.SetMenu();

                    chat.AddReplyHistory();
                    await chat.EditMessage();
                }
                else
                {
                    // ask the amount ordered
                    if (amountOrdered == 0)
                    {
                        // use ForceReply
                        ForceReply f = new ForceReply();
                        f.Force = true;

                        // need to set some parameters to process the player's reply
                        string rType = materialType.ToLower();

                        chat.EditReply(ReplyType.command, GetLangString(groupId, "MerchantAskAmount",
                            GetLangString(groupId, rType)));


                        // put parameters here embedded as url
                        chat.AddReply(ReplyType.command, $"[?](Merchant.{groupId}.{action}.{materialType})");

                        await chat.EditMessage(forceReply: f);
                    }/*
                    else
                    {
                        // get the trade run!
                        ResourceType rType = (ResourceType)Enum.Parse(typeof(ResourceType), materialType);

                        if (action == "Buy")
                        {
                            MerchantBuy(playerId, messageId, rType,)
                        }
                        else if (action == "Sell")
                        {

                        }
                    }*/
                }
            }
        }

        async Task MerchantBuy(long playerId, int messageId, ResourceType rType, int amountOrdered)
        {
            //MerchantGlobal mg = merchantGlobal;
            CityChatHandler chat = cities[playerId].chat;

            // check if got enough money
            int goldCost = amountOrdered * merchantGlobal.BuyPrice[rType];
            Resources cost = new Resources(goldCost, 0, 0, 0);

            if (PayCost(ref cities[playerId]._resources, cost, playerId))
            {
                // add city's CurrentResources
                cities[playerId]._resources.Add(rType, amountOrdered);

                // add MerchantGlobal.ThisTurnDemand
                merchantGlobal.ThisTurnDemand[rType] += amountOrdered;

                // show trade successful
                chat.EditReply(ReplyType.status, GetLangString(groupId, "MerchantTradeSuccess",
                    amountOrdered,
                    Enum.GetName(typeof(ResourceType), rType),
                    merchantGlobal.BuyPrice[rType],
                    goldCost));
            }

            await MainMenu(playerId, messageId);
            await chat.EditMessage();
        }

        // MerchantSell() waits MerchantBuy() test
        // void MerchantSell() { }
        
        #endregion

        #region Inline Keyboard Interaction
        public async Task MainMenu(long playerId = 0, int messageId = 0)
        {   
            if (playerId != 0 && messageId != 0)
            {
                // this 'if' block can be used after one task has finished
                CityStatus(playerId);
                //ArmyStatus(playerId);

                CityChatHandler chat = cities[playerId].chat;
                chat.ClearReplyHistory();

                SetMainMenu(playerId);

                await cities[playerId].chat.EditMessage();
            }
            else
            {
                foreach (KeyValuePair<long, City> kvp in cities)
                {
                    CityStatus(kvp.Key);
                    //ArmyStatus(kvp.Key);

                    CityChatHandler chat = cities[kvp.Key].chat;
                    chat.ClearReplyHistory();

                    SetMainMenu(kvp.Key);
					
                    if (chat.messageId == 0)
                        await chat.SendReply();
                    else
                        await chat.EditMessage();
                }
            }
        }

        //this method is the child of and only used in MainMenu()
        void SetMainMenu(long playerId)
        {
            CityChatHandler chat = cities[playerId].chat;
            chat.EditReply(ReplyType.command, GetLangString(groupId, "MainMenu"));

            chat.buttons.Add(new InlineKeyboardButton(GetLangString(groupId, "AssignTask"), $"AssignTask|{groupId}"));
            chat.buttons.Add(new InlineKeyboardButton(GetLangString(groupId, "CityStatus"), $"YourStatus|{groupId}"));
            chat.buttons.Add(new InlineKeyboardButton(GetLangString(groupId, "Attack"), $"Attack|{groupId}"));

            if ((bool) merchantGlobal.IsMerchantInCity[playerId])
            {
                chat.buttons.Add(new InlineKeyboardButton(GetLangString(groupId, "Merchant"), $"Merchant|{groupId}"));
            }

            // no Back button
            chat.menu = new InlineKeyboardMarkup(chat.buttons.Select(x => new[] { x }).ToArray());
            
            chat.AddReplyHistory();
        }
        
        public async Task AssignTask(long playerId, int messageId)
        {
            CityChatHandler chat = cities[playerId].chat;

            //CityStatus(playerId);
            chat.EditReply(ReplyType.command, GetLangString(groupId, "AssignTask"));

            chat.AddMenuButton(new InlineKeyboardButton(GetLangString(groupId, "UpgradeProduction"), $"UpgradeProduction|{groupId}"));
            chat.AddMenuButton(new InlineKeyboardButton(GetLangString(groupId, "RaiseArmy"), $"RaiseArmy|{groupId}"));
            chat.AddMenuButton(new InlineKeyboardButton(GetLangString(groupId, "Back"), $"Back|{groupId}"));
            chat.SetMenu();

            chat.AddReplyHistory();
            await chat.EditMessage();
        }

        public async Task MyStatus(long playerId, int messageId)
        {
			cities[playerId].chat.ClearReply(ReplyType.status);

            CityStatus(playerId);
            //ArmyStatus(playerId);

            SetMainMenu(playerId);
            await cities[playerId].chat.EditMessage();
        }

        public async Task UpgradeProduction(long playerId, int messageId)
        {
            CityChatHandler chat = cities[playerId].chat;
			
            chat.EditReply(ReplyType.command, GetLangString(groupId, "AskUpgradeProductionHeader"));
			
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
				
                chat.AddReply(ReplyType.command, GetLangString(groupId, "ResourceUpgradePriceCost", thisResourceString, currentLvl+1 ,upgradeCost));
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

            if (chat.statusReplyHistory.Count != 1)
            {
                //if (chat.menuHistory.Count == chat.backCount)
                //{
                    chat.menu = chat.menuHistory.Pop();
                    chat.menu = chat.menuHistory.Peek();

                    chat.statusString = chat.statusReplyHistory.Pop();
                    chat.statusString = chat.statusReplyHistory.Peek();

                    chat.cmdString = chat.cmdReplyHistory.Pop();
                    chat.cmdString = chat.cmdReplyHistory.Peek();
                //}
                /*
                else
                {
                    chat.menu = chat.menuHistory.Pop();
                    chat.statusString = chat.statusReplyHistory.Pop();
                    chat.cmdString = chat.cmdReplyHistory.Pop();
                }
                */
            }
            else
            {
                chat.menu = chat.menuHistory.Peek();
                chat.statusString = chat.statusReplyHistory.Peek();
                chat.cmdString = chat.cmdReplyHistory.Peek();
            }
            
            await chat.EditMessage();
        }

        public async Task TimeUp()
        {
            // let's kill this 3 times
            for (byte i = 0; i < 3; i++)
            {
                foreach (KeyValuePair<long, City> kvp in cities)
                {
                    CityChatHandler chat = kvp.Value.chat;
                    
                    // clear everythingggg
                    chat.cmdString = "";
                    chat.buttons.Clear();
                    chat.menu = null;


                    chat.EditReply(ReplyType.command, GetLangString(groupId, "TimeUp", turn - 1) 
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
            await BotReply();
        }

        public async Task GameUnhosted()
        {
            gameStatus = GameStatus.Unhosted;
            botReply += "Lobby unhosted!\r\n";
            await BotReply();
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
				await BotReply();
			}
			else
			{
                botReply += GetLangString(groupId, "AlreadyJoinedGame", firstName);
				await BotReply();
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

				await BotReply();
			}
			else
			{
				botReply += "No game has been hosted in this lobby yet.\r\nUse /joingame to make one!\r\n";
				await BotReply();
			}
		}

		public async Task PlayerLeave(long telegramId, string firstName, string lastName)
		{
			if (PlayerCheck(telegramId, firstName, lastName))
			{
				cities.Remove(telegramId);
				playerCount--;

                botReply += GetLangString(groupId, "LeaveGame", firstName, lastName);
				await BotReply();
			}
			else
			{
				botReply += firstName + " " + lastName + " hasn't join the lobby yet!\r\n";
				await BotReply();
			}
		}

		public int PlayerCount { get { return playerCount; } }

        public async Task ChooseName(long playerId, string NewCityName)
        {
            cities[playerId].playerDetails.cityName = NewCityName;
            cities[playerId].chat.AddReply(ReplyType.general, GetLangString(groupId, "NameChosen", NewCityName));

            await cities[playerId].chat.SendReply();
        }

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
                cities[playerId].chat.AddReply(ReplyType.status, GetLangString(groupId, "NotEnoughResources"));
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

        void BroadcastCityStatus()
        {
            foreach (KeyValuePair<long, City> kvp in cities)
            {
                CityStatus(kvp.Key);

                // await kvp.Value.chat.SendReply();
            }
        }

        ArmyFront GetFront(long playerId, byte frontId)
        {
            ArmyFront af = cities[playerId]._army.Fronts[frontId];

            return af;
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
        async Task BotReply(/* IReplyMarkup replyMarkup = null, */ParseMode _parseMode = ParseMode.Markdown)
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
