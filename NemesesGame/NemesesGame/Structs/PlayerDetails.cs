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
        public string cityName;
        public long groupId;

        public PlayerDetails(long TelegramId, string FirstName, string LastName, string CityName, long GroupId)
        {
            firstName = FirstName;
            lastName = LastName;
            telegramId = TelegramId;
            cityName = CityName;
            groupId = GroupId;
        }
    }
}
