using Google.Cloud.Firestore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static SmartGuardHub.Database.Firebase.FirebaseModels;

namespace SmartGuardHub.Database.Firebase
{
    /// <summary>
    /// Service for connecting to Firebase Firestore and syncing data.
    /// Handles Users, Devices, and User_ACL collections.
    /// </summary>
    public class FirebaseService
    {
        private readonly FirestoreDb _db;

        public FirebaseService(string projectId, string credentialsPath)
        {
            System.Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credentialsPath);
            _db = FirestoreDb.Create(projectId);
        }

        #region Users

        public async Task<List<User>> GetUsersAsync()
        {
            var collection = _db.Collection("Users");
            var snapshot = await collection.GetSnapshotAsync();
            return snapshot.Documents.Select(d => d.ConvertTo<User>()).ToList();
        }

        public async Task<User> GetUserAsync(string userId)
        {
            var docRef = _db.Collection("Users").Document(userId);
            var snapshot = await docRef.GetSnapshotAsync();

            if (!snapshot.Exists)
            {
                throw new System.Exception($"User with ID {userId} not found");
            }

            return snapshot.ConvertTo<User>();
        }

        public async Task<string> AddUserAsync(User user)
        {
            var collection = _db.Collection("Users");
            var docRef = await collection.AddAsync(user);
            return docRef.Id;
        }

        public async Task UpdateUserAsync(string userId, User user)
        {
            var docRef = _db.Collection("Users").Document(userId);
            await docRef.SetAsync(user, SetOptions.MergeAll);
        }

        public async Task DeleteUserAsync(string userId)
        {
            var docRef = _db.Collection("Users").Document(userId);
            await docRef.DeleteAsync();
        }

        #endregion

        #region Devices

        public async Task<List<Device>> GetDevicesAsync()
        {
            var collection = _db.Collection("Devices");
            var snapshot = await collection.GetSnapshotAsync();
            return snapshot.Documents.Select(d => d.ConvertTo<Device>()).ToList();
        }

        public async Task<Device> GetDeviceAsync(string deviceId)
        {
            var docRef = _db.Collection("Devices").Document(deviceId);
            var snapshot = await docRef.GetSnapshotAsync();

            if (!snapshot.Exists)
            {
                throw new System.Exception($"Device with ID {deviceId} not found");
            }

            return snapshot.ConvertTo<Device>();
        }

        public async Task<string> AddDeviceAsync(Device device)
        {
            var collection = _db.Collection("Devices");
            var docRef = await collection.AddAsync(device);
            return docRef.Id;
        }

        public async Task UpdateDeviceAsync(string deviceId, Device device)
        {
            var docRef = _db.Collection("Devices").Document(deviceId);
            await docRef.SetAsync(device, SetOptions.MergeAll);
        }

        public async Task DeleteDeviceAsync(string deviceId)
        {
            var docRef = _db.Collection("Devices").Document(deviceId);
            await docRef.DeleteAsync();
        }

        #endregion

        #region User ACL

        public async Task<List<UserACL>> GetAclForDeviceAsync(string deviceId)
        {
            var collection = _db.Collection("User_ACL");
            var query = collection.WhereEqualTo("DeviceId", deviceId);
            var snapshot = await query.GetSnapshotAsync();
            return snapshot.Documents.Select(d => d.ConvertTo<UserACL>()).ToList();
        }

        public async Task<string> AddAclEntryAsync(UserACL acl)
        {
            var collection = _db.Collection("User_ACL");
            var docRef = await collection.AddAsync(acl);
            return docRef.Id;
        }

        public async Task RemoveAclEntryAsync(string aclId)
        {
            var docRef = _db.Collection("User_ACL").Document(aclId);
            await docRef.DeleteAsync();
        }

        #endregion
    }
}
