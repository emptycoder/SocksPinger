using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using SocksPingerWpf.Extensions;

namespace SocksPingerWpf
{
    public class Settings
    {
        private readonly Dictionary<string, JToken> _entities = new();
        public Settings(string path) => LoadSettings(path);

        private void LoadSettings(string path)
        {
            if (!File.Exists(path))
                throw new Exception($"settings.json not found (https://github.com/emptycoder/Spys.onePinger)").Log();

            StreamReader streamReader = null;
            try
            {
                streamReader = new StreamReader(path);
                while (!streamReader.EndOfStream)
                {
                    string line = streamReader.ReadLine();
					
                    if (line == null) { break; }

                    int spaceIndex = line.IndexOf(' ', StringComparison.Ordinal);
                    // ReSharper disable once UnusedVariable
                    string settingValueType = line.Substring(0, spaceIndex);
                    string settingBody = line.Substring(spaceIndex + 1);
                    JToken entry = JObject.Parse(settingBody).First;
					
                    if (entry == null) { break; }
                    _entities.Add(entry.Path, entry.First);
                }
            }
            finally
            {
                streamReader?.Close();
                streamReader?.Dispose();
            }
        }

        public T Get<T>(string key)
        {
            if (_entities.TryGetValue(key, out var value) && value is not null)
                return value.ToObject<T>();
            
            throw new Exception($"Value for key ({key}) not found! (https://github.com/emptycoder/Spys.onePinger)").Log();
        }
    }
}