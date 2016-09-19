using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NemesesGame;

namespace NemesesGame
{
    public enum ArmyType { Regulars }
    public enum ArmyState { Defense , March, Attack, Return }
    
    public class Army
    {
        public Army ()
        {
            //init typeNumber dict
            typeNumber.Add(ArmyType.Regulars, 1000);

            //init cost
            cost.Add(ArmyType.Regulars, regularsCost);
        }

        int regularsCost = 3;

        // City Army >> Fronts >> armyType & number

        Dictionary<ArmyState, Dictionary<ArmyType, int>> fronts = new Dictionary<ArmyState, Dictionary<ArmyType, int>>();
        Dictionary<ArmyType, int> typeNumber = new Dictionary<ArmyType, int>();

        Dictionary<ArmyType, int> cost = new Dictionary<ArmyType, int>(); // for now, we will only use gold

        //public int Regulars { get { return typeNumber[ArmyType.Regulars]; } set { typeNumber[ArmyType.Regulars] = value; } }

        public Dictionary<ArmyState, Dictionary<ArmyType, int>> Fronts { get { return fronts; } set { fronts = value; } }
        public Dictionary<ArmyType, int> TypeNumber { get { return typeNumber; } set { typeNumber = value; } }

        public Dictionary<ArmyType, int> Cost { get { return cost; } set { cost = value; } }
    }
}
