using System;
using System.Collections.Generic;
using System.IO;
using System.Linq; // Added for .Any()
using System.Windows;
using testing1.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace testing1.ViewModels
{
    public class DeviceConfigManager
    {
        private readonly string _configDirectory;
        private readonly ISerializer _yamlSerializer;

        public DeviceConfigManager()
        {
            // Create config directory in user's Documents folder
            _configDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DeviceConfigs");

            // Ensure directory exists
            if (!Directory.Exists(_configDirectory))
            {
                Directory.CreateDirectory(_configDirectory);
            }

            // Initialize YAML serializer
            _yamlSerializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .WithIndentedSequences()
                .Build();
        }


        public bool SaveDeviceConfig(DeviceInfo device)
        {
            try
            {
                // Prevent duplicate IP addresses (instead of MAC)
                var existingConfigs = LoadAllDeviceConfigs();
                if (existingConfigs.Any(cfg => cfg.DeviceInfo?.IP == device.IP))
                {
                    MessageBox.Show($"Device with IP address {device.IP} is already added.", "Duplicate IP Address", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                // Get next device number
                int deviceNumber = GetNextDeviceNumber();

                // Create device config
                var config = new DeviceConfig
                {
                    DeviceInfo = device,
                    CreatedDate = DateTime.Now,
                    DeviceNumber = deviceNumber,
                    ConfigVersion = "1.0"
                };

                // Generate filename - keep using MAC for filename since it's still a good unique identifier for files
                string fileName = $"Device_{deviceNumber:D3}_{device.MAC.Replace(".", "_")}.yaml";
                string filePath = Path.Combine(_configDirectory, fileName);

                // Serialize to YAML
                string yamlContent = _yamlSerializer.Serialize(config);

                // Write to file
                File.WriteAllText(filePath, yamlContent);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving device config: {ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public bool SaveMultipleDeviceConfigs(List<DeviceInfo> devices)
        {
            try
            {
                int successCount = 0;
                List<string> errors = new List<string>();

                foreach (var device in devices)
                {
                    if (SaveDeviceConfig(device))
                    {
                        successCount++;
                    }
                }

                // Remove the MessageBox.Show here - let the calling code handle UI feedback
                // Only log errors if needed for debugging
                if (errors.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"Errors occurred while saving: {string.Join(", ", errors)}");
                }

                return successCount > 0;
            }
            catch (Exception ex)
            {
                // Keep error messages for actual exceptions
                MessageBox.Show($"Error saving multiple device configs: {ex.Message}", "Save Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
        public List<DeviceConfig> LoadAllDeviceConfigs()
        {
            var configs = new List<DeviceConfig>();
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            try
            {
                var yamlFiles = Directory.GetFiles(_configDirectory, "*.yaml");

                foreach (var file in yamlFiles)
                {
                    try
                    {
                        string yamlContent = File.ReadAllText(file);
                        var config = deserializer.Deserialize<DeviceConfig>(yamlContent);
                        configs.Add(config);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error loading config file {file}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading device configs: {ex.Message}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return configs;
        }

        private int GetNextDeviceNumber()
        {
            try
            {
                var existingConfigs = LoadAllDeviceConfigs();
                if (existingConfigs.Count == 0)
                    return 1;

                int maxNumber = 0;
                foreach (var config in existingConfigs)
                {
                    if (config.DeviceNumber > maxNumber)
                        maxNumber = config.DeviceNumber;
                }

                return maxNumber + 1;
            }
            catch
            {
                return 1;
            }
        }

        public string GetConfigDirectory()
        {
            return _configDirectory;
        }

        public void OpenConfigDirectory()
        {
            try
            {
                System.Diagnostics.Process.Start("explorer.exe", _configDirectory);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening config directory: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}