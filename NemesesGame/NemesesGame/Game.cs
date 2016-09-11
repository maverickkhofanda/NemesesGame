using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Timers;
using NemesesGame;

namespace NemesesGame
{
    public class Game
    {
		Timer _timer;
        int turnInterval = 5000;

        private int playerCount = 0;
        public long groupId;
        public string chatName;

		private string botReply = "";
        private string privateReply = "";
        
        public Dictionary<long, City> cities = new Dictionary<long, City>();
        string[] cityNames = { "Andalusia", "Transylvania", "Groot", "Saruman", "Azeroth" };

		private GameStatus gameStatus = GameStatus.Unhosted;
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

        public void StartGame()
        {
            //note: Private chat to each player unimplemente yet!

            botReply += "Game is starting... Players in game: \r\n";

            foreach (KeyValuePair<long, City> kvp in cities)
            {
                City city = kvp.Value;
                botReply += city.playerDetails.firstName + " " + city.playerDetails.lastName + "\r\n";
                privateReply += string.Format(Program.GetLangString(groupId, "StartGame", city.playerDetails.cityName));
                privateReply += string.Format(
                    Program.GetLangString(groupId, "CurrentResources",
                    
                    city.playerDetails.cityName,
                    city.cityResources.Gold, city.resourceRegen.Gold,
                    city.cityResources.Wood, city.resourceRegen.Wood,
                    city.cityResources.Stone, city.resourceRegen.Stone,
                    city.cityResources.Iron, city.resourceRegen.Iron));
				BotReply(kvp.Key, ref privateReply);
            }

			BotReply(groupId, ref botReply);
			gameStatus = GameStatus.Starting;
            Timer(turnInterval, Turn);
		}
        
        /// <summary>
        /// still a STUB
        /// </summary>
        public void Turn(object sender, ElapsedEventArgs e)
        {
            turn++;
			gameStatus = GameStatus.InGame;
            botReply += string.Format("Turn *{0}*\n\rNext turn in *{1}* secs", turn, turnInterval/1000);
			BotReply(groupId, ref botReply);

            //Insert turn implementation here
            
		}

        private void Timer(int timerInterval, ElapsedEventHandler elapsedEventHandler, bool timerEnabled = true)
        {
            _timer = new Timer();
            _timer.Elapsed += elapsedEventHandler;
            _timer.Interval = timerInterval;
            _timer.Enabled = true;
        }

        public void GameHosted()  
        {
			gameStatus = GameStatus.Hosted;
            botReply += "New game is made in this lobby!\r\n";
			BotReply(groupId, ref botReply);
		}

		public void GameUnhosted()
		{
			gameStatus = GameStatus.Unhosted;
			botReply += "Lobby unhosted!\r\n";
			BotReply(groupId, ref botReply);
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

		public void PlayerJoin(long telegramId, string firstName, string lastName)
		{
			if (!PlayerCheck(telegramId, firstName, lastName))
			{
                //randomize city name
                int i = cityNames.Length;
                Random rnd = new Random();
                i = rnd.Next(0, i);
                string cityName = cityNames[i];
                Program.RemoveElement(cityNames, i);
                                
				cities.Add(telegramId, new City(telegramId, firstName, lastName, cityName));
				playerCount++;

				if (playerCount == 1) //Lobby has just been made
				{
					GameHosted();
				}

				botReply += firstName + " " + lastName + " has joined the game!\r\n";
				BotReply(groupId, ref botReply);
			}
			else
			{
				botReply += firstName + " ALREADY joined the game!\n\rStahp confusing the bot :(\r\n";
				BotReply(groupId, ref botReply);
			}
		}

		public void PlayerList()
		{
			if (playerCount > 0)
			{
				botReply += playerCount + " players have joined this lobby :\r\n";

				foreach (KeyValuePair<long, City> kvp in cities)
				{
					botReply += kvp.Value.playerDetails.firstName + " " + kvp.Value.playerDetails.lastName + "\r\n";
				}

				BotReply(groupId, ref botReply);
			}
			else
			{
				botReply += "No game has been hosted in this lobby yet.\r\nUse /joingame to make one!\r\n";
				BotReply(groupId, ref botReply);
			}
		}

		public void PlayerLeave(long telegramId, string firstName, string lastName)
		{
			if (PlayerCheck(telegramId, firstName, lastName))
			{
				cities.Remove(telegramId);
				playerCount--;

				botReply += firstName + " " + lastName + " has left the lobby!\r\n";
				BotReply(groupId, ref botReply);
			}
			else
			{
				botReply += firstName + " " + lastName + " hasn't join the lobby yet!\r\n";
				BotReply(groupId, ref botReply);
			}
		}

		public int PlayerCount { get { return playerCount; } }

		public void BotReply(long groupId, ref string message, IReplyMarkup replyMarkup = null, ParseMode _parseMode = ParseMode.Markdown)
		{
			Program.SendMessage(groupId, message, replyMarkup, _parseMode);
			message = ""; //Reset botReply string
		}
    }
}
