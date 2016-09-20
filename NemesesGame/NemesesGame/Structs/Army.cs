using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NemesesGame;

namespace NemesesGame
{
    //public enum ArmyType { Regulars }
    public enum ArmyState { Base, March /*,Target ?*/, Invade, Intercept }
    
    public class Army
    {   
        public Army ()
        {
            fronts.Add(new ArmyFront(ArmyState.Base, startingNumber));

            //init typeNumber dict
            //typeNumber.Add(ArmyType.Regulars, 1000);

            //init cost
            //cost.Add(ArmyType.Regulars, regularsCost);
        }
        List<ArmyFront> fronts = new List<ArmyFront>();

        int startingNumber = 1000;
        float power = 1.0f;

        int cost = 3;
        //float costX = 1.0f; // cost multiplier don't use this yet...
        

        // ----------------------------------------------------------------------
        //Dictionary<ArmyState,  int> fronts = new Dictionary<ArmyState, int>();
        //Dictionary<ArmyType, int> typeNumber = new Dictionary<ArmyType, int>();

        //Dictionary<ArmyType, int> cost = new Dictionary<ArmyType, int>(); // for now, we will only use gold

        //public int Regulars { get { return typeNumber[ArmyType.Regulars]; } set { typeNumber[ArmyType.Regulars] = value; } }

        //public Dictionary<ArmyState, Dictionary<ArmyType, int>> Fronts { get { return fronts; } set { fronts = value; } }
        //public Dictionary<ArmyType, int> TypeNumber { get { return typeNumber; } set { typeNumber = value; } }

        //public Dictionary<ArmyType, int> Cost { get { return cost; } set { cost = value; } }
    }
}
