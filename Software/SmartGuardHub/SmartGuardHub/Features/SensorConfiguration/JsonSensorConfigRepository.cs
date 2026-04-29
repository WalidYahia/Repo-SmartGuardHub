using System.Text.Json;
using System.Text.Json.Serialization;

namespace SmartGuardHub.Features.SensorConfiguration
{
    public class JsonSensorConfigRepository : ISensorConfigRepository
    {
        private readonly string _filePath;
        private readonly SemaphoreSlim _lock = new(1, 1);

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        public JsonSensorConfigRepository(IConfiguration configuration)
        {
            _filePath = configuration["SensorConfigPath"];
            EnsureFileExists();
        }

        public async Task<List<SensorConfig>> GetAllAsync()
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

        public async Task<bool> SaveAllAsync(List<SensorConfig> configs)
        {
            await _lock.WaitAsync();
            try
            {
                await WriteFileAsync(configs);
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

        private async Task<List<SensorConfig>> ReadFileAsync()
        {
            var json = await File.ReadAllTextAsync(_filePath);
            return JsonSerializer.Deserialize<List<SensorConfig>>(json, _jsonOptions)
                   ?? new List<SensorConfig>();
        }

        private async Task WriteFileAsync(List<SensorConfig> configs)
        {
            var temp = _filePath + ".tmp";
            var json = JsonSerializer.Serialize(configs, _jsonOptions);
            await File.WriteAllTextAsync(temp, json);
            File.Copy(temp, _filePath, overwrite: true);
            File.Delete(temp);
        }
    }
}
