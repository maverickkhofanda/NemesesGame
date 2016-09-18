using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NemesesGame;

namespace NemesesGame
{
    public enum ArmyType { Regulars }
    
    public class Army
    {
        public Army ()
        {
            //init armyNumber dict
            armyNumber.Add(ArmyType.Regulars, 0);

            //init armyCost
            armyCost.Add(ArmyType.Regulars, regularsCost);
        }

        int regularsCost = 3;

        Dictionary<ArmyType, int> armyNumber = new Dictionary<ArmyType, int>();
        Dictionary<ArmyType, int> armyCost = new Dictionary<ArmyType, int>(); // for now, we will only use gold

        public int Regulars { get { return armyNumber[ArmyType.Regulars]; } set { armyNumber[ArmyType.Regulars] = value; } }

        public Dictionary<ArmyType, int> ArmyNumber { get { return armyNumber; } set { armyNumber = value; } }
        public Dictionary<ArmyType, int> ArmyCost { get { return armyCost; } set { armyCost = value; } }
    }
}
