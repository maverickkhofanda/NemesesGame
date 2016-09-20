using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NemesesGame
{
    public struct ArmyFront
    {
        ArmyState _state;
        int _number;
        string _target;
        //float _power;


        public ArmyFront(ArmyState armyState, int number/*, float power*/)
        {
            _state = armyState;
            _number = number;
            _target = "";
            //_power = power;
        }
        public ArmyFront(ArmyState armyState, int number, string target/*, float power*/)
        {
            _state = armyState;
            _number = number;
            _target = target;
            //_power = power;
        }

        public ArmyState State { get { return _state; } set { _state = value; } }
        /// <summary>
        /// This front's troop number
        /// </summary>
        public int Number { get { return _number; } set { _number = value; } }
        /// <summary>
        /// This front's target for current action
        /// </summary>
        public string Target { get { return _target; } set { _target = value; } }
        //public float Power { get { return _power; } set { _power = value; } }
    }
}
