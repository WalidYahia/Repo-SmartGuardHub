namespace SmartGuardHub.Infrastructure
{
    public class Enums
    {
        public enum UnitType
        {
            Unknown = -1,
            SonoffMiniR3 = 0,
            SonoffMiniR4M = 1,
        }

        public enum SwitchOutlet
        {
            Unknown = -1,
            First = 0,
            Second = 1,
            Third = 2,
            Fourth = 3,
        }

        public enum SwitchOutletStatus
        {
            Off = 0,
            On = 1,
        }
        public enum JsonCommandType
        {
            TurnOn = 0,
            TurnOff = 1,
            InchingOn = 2,
            InchingOff = 3,
            GetInfo = 4,
            CreateDevice = 5,
            RenameDevice = 6,
            LoaddAllUnits = 7,
        }

        public enum ScenarioCondition
        {
            Duration = 0,
            OnTime = 1,
            OnOtherSensorValue = 2
        }
        public enum ScenarioOperator
        {
            Equals,
            NotEquals,
            GreaterThan,
            LessThan,
            GreaterOrEqual,
            LessOrEqual
        }

        public enum ScenarioLogic
        {
            And,
            Or
        }
    }
}
