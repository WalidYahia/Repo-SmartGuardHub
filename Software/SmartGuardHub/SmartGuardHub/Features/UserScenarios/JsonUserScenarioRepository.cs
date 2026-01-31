using System.Text.Json;
using System.Text.Json.Serialization;

namespace SmartGuardHub.Features.UserScenarios
{
    public class JsonUserScenarioRepository : IUserScenarioRepository
    {
        private readonly string _filePath;
        private readonly SemaphoreSlim _lock = new(1, 1);

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        public JsonUserScenarioRepository(IConfiguration configuration)
        {
            _filePath = configuration["UserScenariosPath"];
            EnsureFileExists();
        }

        public async Task<List<UserScenario>> GetAllAsync()
        {
            await _lock.WaitAsync();
            try
            {
                return await ReadFileAsync();
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<List<UserScenario>> GetEnabledAsync()
        {
            var all = await GetAllAsync();
            return all.Where(x => x.IsEnabled).ToList();
        }

        public async Task<UserScenario?> GetByIdAsync(string id)
        {
            var all = await GetAllAsync();
            return all.FirstOrDefault(x => x.Id == id);
        }

        public async Task<bool> SaveAsync(UserScenario scenario)
        {
            await _lock.WaitAsync();
            try
            {
                var all = await ReadFileAsync();

                var index = all.FindIndex(x => x.Id == scenario.Id);
                if (index >= 0)
                    all[index] = scenario;
                else
                    all.Add(scenario);

                await WriteFileAsync(all);

                return true;
            }
            catch { return false; }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<bool> DeleteAsync(string id)
        {
            await _lock.WaitAsync();
            try
            {
                var all = await ReadFileAsync();
                all.RemoveAll(x => x.Id == id);
                await WriteFileAsync(all);

                return true;
            }
            catch { return false; }
            finally
            {
                _lock.Release();
            }
        }

        /// Helpers =============================//
        private void EnsureFileExists()
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir!);

            if (!File.Exists(_filePath))
                File.WriteAllText(_filePath, "[]");
        }

        private async Task<List<UserScenario>> ReadFileAsync()
        {
            var json = await File.ReadAllTextAsync(_filePath);
            return JsonSerializer.Deserialize<List<UserScenario>>(json, _jsonOptions)
                   ?? new List<UserScenario>();
        }

        private async Task WriteFileAsync(List<UserScenario> scenarios)
        {
            var temp = _filePath + ".tmp";

            var json = JsonSerializer.Serialize(scenarios, _jsonOptions);
            await File.WriteAllTextAsync(temp, json);

            File.Copy(temp, _filePath, overwrite: true);
            File.Delete(temp);
        }
    }
}
