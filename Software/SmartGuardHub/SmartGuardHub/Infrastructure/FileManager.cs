using System.Text;

namespace SmartGuardHub.Infrastructure
{
    public static class FileManager
    {
        public static async Task SaveFileAsync(string path, string content)
        {
            try
            {
                // Ensure directory exists
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Write content to file
                await File.WriteAllTextAsync(path, content, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                throw new IOException($"Error saving file at {path}: {ex.Message}", ex);
            }
        }

        public static async Task<string> LoadFileAsync(string path)
        {
            try
            {
                if (!File.Exists(path))
                    throw new FileNotFoundException($"File not found: {path}");

                return await File.ReadAllTextAsync(path, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                throw new IOException($"Error reading file at {path}: {ex.Message}", ex);
            }
        }
    }
}
