using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NemesesGame;

namespace NemesesGame
{
    public class Game
    {
		private int playerCount = 0;

		private string botReply = "";

		public Dictionary<long, City> players = new Dictionary<long, City>();

        public Game()
        {

        }

        public void GameHosted()  
        {
            botReply += "New game is made in this lobby!\r\n";
        }

		public void GameUnhosted()
		{
			botReply += "Lobby unhosted!\r\n";
		}

		public bool PlayerCheck(long telegramId, string firstName, string lastName)
		{
			//Checks if a player has joined the lobby
			if (players.ContainsKey(telegramId))
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
				players.Add(telegramId, new City(telegramId, firstName, lastName));
				playerCount++;

				if (playerCount == 1) //Lobby has just been made
				{
					GameHosted();
				}

				botReply += firstName + " " + lastName + " has joined the game!\r\n";
			}
			else
			{
				botReply += firstName + " ALREADY joined the game!\n\rStahp confusing the bot :(\r\n";
			}
		}

		public void PlayerList()
		{
			if (playerCount > 0)
			{
				botReply += playerCount + " players have joined this lobby :\r\n";

				foreach (KeyValuePair<long, City> kvp in players)
				{
					botReply += kvp.Value.playerDetails.firstName + " " + kvp.Value.playerDetails.lastName + "\r\n";
				}
			}
			else
			{
				botReply += "No game has been hosted in this lobby yet.\r\nUse /joingame to make one!\r\n";
			}
		}

		public void PlayerLeave(long telegramId, string firstName, string lastName)
		{
			if (PlayerCheck(telegramId, firstName, lastName))
			{
				players.Remove(telegramId);
				playerCount--;

				botReply += firstName + " " + lastName + " has left the lobby!\r\n";
			}
			else
			{
				botReply += firstName + " " + lastName + " hasn't join the lobby yet!\r\n";
			}
		}

		public int PlayerCount { get { return playerCount; } }

		public string BotReply()
		{
			string messageToSend = botReply;
			botReply = ""; //Reset botReply string

			return messageToSend;

		}
    }
}
