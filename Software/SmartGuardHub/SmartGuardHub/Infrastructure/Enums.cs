namespace SmartGuardHub.Infrastructure
{
    public class Enums
    {
        public enum SwitchOutlet
        {
            Unknown = -1,
            First = 0,
            Second = 1,
            Third = 2,
            Fourth = 3,
        }

        // Matches cloud SwitchNo enum — used for deterministic ID generation
        public enum SwitchNo
        {
            Non     = -1,
            Switch1 =  0,
            Switch2 =  1,
            Switch3 =  2,
            Switch4 =  3,
            Switch5 =  4,
            Switch6 =  5,
            Switch7 =  6,
            Switch8 =  7,
        }

        // Matches cloud SensorType enum — used for deterministic ID generation
        public enum SensorType
        {
            Unknown = 0,
            SonOffMiniR3Swich       = 1,
            Temperature = 2,
            Humidity    = 3,
            Pressure    = 4,
            Motion      = 5,
            Gas         = 6,
            Light       = 7,
            Vibration   = 8,
            Current     = 9,
            Voltage     = 10,
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


            SaveUSerScenario = 10,
            DeleteUSerScenario = 11,

            Ping = -1,
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

        public enum MqttTopics
        {
            /// <summary>
            /// Publish from Cloud
            /// </summary>
            CloudSensorConfig,

            /// <summary>
            /// Publish from Device
            /// </summary>
            DeviceSensorConfig,

            /// <summary>
            /// Publish from Cloud
            /// </summary>
            CloudUserScenario,

            /// <summary>
            /// Publish from Device
            /// </summary>
            UserScenario,

            /// <summary>
            /// Publish from Device
            /// </summary>
            DeviceData,

            /// <summary>
            /// Publish from Cloud
            /// </summary>
            RemoteAction,

            /// <summary>
            /// Publish from Device
            /// </summary>
            RemoteAction_Ack,

            /// <summary>
            /// Publish from Cloud
            /// </summary>
            RemoteUpdate,

            /// <summary>
            /// Publish from Device
            /// </summary>
            RemoteUpdate_Ack,
        }
    }
}
