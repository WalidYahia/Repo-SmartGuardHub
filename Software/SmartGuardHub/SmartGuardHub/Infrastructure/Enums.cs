namespace SmartGuardHub.Infrastructure
{
    public class Enums
    {
        public enum UnitType
        {
            SonoffMiniR3 = 0,
            SonoffMiniR4M = 1,
        }

        public enum SwitchOutlet
        {
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
    }
}
