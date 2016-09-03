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
        private long chatId;

		private int playerCount = 0;

		public Dictionary<long, City> players = new Dictionary<long, City>();

        public Game()
        {

        }

        public string GameHosted()  
        {
            return "New game is made in this lobby!";
        }

		public string PlayerJoin(long TelegramId, string FirstName, string LastName)
		{
            string s;
            if (players.ContainsKey(TelegramId))
            {
                s = FirstName + " ALREADY joined the game!\n\rStahp confusing the bot :(";
            } else
            {
                players.Add(TelegramId, new NemesesGame.City(TelegramId, FirstName, LastName));
                s = players[TelegramId].ThisPlayer();
            }

            return s;
		}

		public int PlayerCount { get { return playerCount; } }
    }
}
