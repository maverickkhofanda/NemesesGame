using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NemesesGame
{
    public struct ArmyFront
    {
        ArmyState armyState;
        int number;
        //float power;

        public ArmyFront(ArmyState _armyState, int _number/*, float _power*/)
        {
            armyState = _armyState;
            number = _number;
            //power = _power;
        }
    }
}
