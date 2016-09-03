using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NemesesGame
{
    public struct PlayerDetails
    {
        public string firstName;
        public string lastName;
        public long telegramId;

        public PlayerDetails(long TelegramId, string FirstName, string LastName)
        {
            firstName = FirstName;
            lastName = LastName;
            telegramId = TelegramId;
        }
    }
}
