using Google.Cloud.Firestore;

namespace SmartGuardHub.Database.Firebase
{
    public class FirebaseModels
    {
        [FirestoreData]
        public class User
        {
            [FirestoreDocumentId]
            public string Id { get; set; }

            [FirestoreProperty("UserName")]
            public string UserName { get; set; }

            [FirestoreProperty("Phone")]
            public string Phone { get; set; }

            [FirestoreProperty("Password")]
            public string Password { get; set; }

            [FirestoreProperty("Email")]
            public string Email { get; set; }

            [FirestoreProperty("IsActive")]
            public bool IsActive { get; set; }

            [FirestoreProperty("CanPerformRemoteAction")]
            public bool CanPerformRemoteAction { get; set; }
        }

        [FirestoreData]
        public class Device
        {
            [FirestoreDocumentId]
            public string Id { get; set; }

            [FirestoreProperty("DeviceName")]
            public string DeviceName { get; set; }

            [FirestoreProperty("DeviceType")]
            public string DeviceType { get; set; }

            [FirestoreProperty("Location")]
            public string Location { get; set; }

            [FirestoreProperty("IsOnline")]
            public bool IsOnline { get; set; }

            [FirestoreProperty("InstalledSensors")]
            public List<InstalledSensor> InstalledSensors { get; set; }
        }

        [FirestoreData]
        public class InstalledSensor
        {
            [FirestoreProperty("SensorId")]
            public string SensorId { get; set; }

            [FirestoreProperty("SensorType")]
            public string SensorType { get; set; }

            [FirestoreProperty("SensorName")]
            public string SensorName { get; set; }

            [FirestoreProperty("Value")]
            public object Value { get; set; }

            [FirestoreProperty("Unit")]
            public string Unit { get; set; }

            [FirestoreProperty("LastUpdated")]
            public Timestamp LastUpdated { get; set; }
        }

        [FirestoreData]
        public class UserACL
        {
            [FirestoreDocumentId]
            public string Id { get; set; }

            [FirestoreProperty("UserId")]
            public string UserId { get; set; }

            [FirestoreProperty("DeviceId")]
            public string DeviceId { get; set; }

            [FirestoreProperty("CanRead")]
            public bool CanRead { get; set; }

            [FirestoreProperty("CanWrite")]
            public bool CanWrite { get; set; }

            [FirestoreProperty("CanControl")]
            public bool CanControl { get; set; }
        }
    }
}
