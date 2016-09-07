using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NemesesGame;

namespace NemesesGame
{
    public class City
    {
        public Resources cityResources = new Resources();
        public Resources resourceRegen = new Resources();

        RefResources refResources = new RefResources();
        public PlayerDetails playerDetails = new PlayerDetails();
        

        /* Unimplemented yet
         * public Army cityArmy;
         * public Upgrades cityUpgrades;
         */

        public City(long telegramId, string firstName, string lastName, string cityName)
        {
            playerDetails = new PlayerDetails(telegramId, firstName, lastName, cityName);

            InitCity();
        }

        /// <summary>
        /// City initializer
        /// </summary>
        void InitCity()
        {
            cityResources = refResources.StartingResources;
            resourceRegen = refResources.ResourcesRegenBasic;
        }
    }
}
