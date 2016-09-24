using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NemesesGame
{
    public class ArmyFront
    {
        ArmyState _state;
        int _number;
        long _targetTelegramId;
        byte _targetFrontId;
        float _power;
        byte _marchLeft;
        
        public ArmyFront(ArmyState armyState, int number, long targetPlayerId = 0, byte targetFrontId = 0, float power = 1.0f, byte marchLeft = 0)
        {
            _state = armyState;
            _number = number;
            _targetTelegramId = targetPlayerId;
            _targetFrontId = targetFrontId;
            _power = power;
            _marchLeft = marchLeft;
        }

        public ArmyState State { get { return _state; } set { _state = value; } }
        /// <summary>
        /// This front's troop number
        /// </summary>
        public int Number { get { return _number; } set { _number = value; } }
        public long TargetTelegramId { get { return _targetTelegramId; } set { _targetTelegramId = value; } }
        public byte TargetFrontId { get { return _targetFrontId; } set { _targetFrontId = value; } }
        public byte MarchLeft { get { return _marchLeft; } set { _marchLeft = value; } }
        public float Power { get { return _power; } set { _power = value; } }
        public int CombatPower
        {
            get
            {
                float f = _power * _number;
                return (int) f;
            }
        }

        /// <summary>
        /// This front's target for current action
        /// </summary>
        //public Tuple<long, int> Target { get { return _target; } set { _target = value; } }

    }
}
