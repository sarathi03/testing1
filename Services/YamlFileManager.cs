using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using testing1.Models;

namespace testing1.Services
{
    public class YamlFileManager
    {
        private readonly string _configDirectory;
        private readonly ISerializer _yamlSerializer;
        private readonly IDeserializer _yamlDeserializer;

        public YamlFileManager()
        {
            _configDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DeviceConfigs");

            if (!Directory.Exists(_configDirectory))
            {
                Directory.CreateDirectory(_configDirectory);
            }

            _yamlSerializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .WithIndentedSequences()
                .Build();

            _yamlDeserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties() // Add this to ignore unknown properties
                .Build();
        }

        public bool SaveDeviceManagementModel(DeviceManagementModel deviceModel)
        {
            try
            {
                if (deviceModel.DeviceNumber <= 0)
                {
                    deviceModel.DeviceNumber = GetNextDeviceNumber();
                }

                deviceModel.LastUpdated = DateTime.Now;

                string macForFile = deviceModel.DeviceInfo.MAC?.Replace(".", "_").Replace(":", "_") ?? "Unknown";
                string fileName = $"Device_{deviceModel.DeviceNumber:D3}_{macForFile}.yaml";
                string filePath = Path.Combine(_configDirectory, fileName);

                string yamlContent = _yamlSerializer.Serialize(deviceModel);
                File.WriteAllText(filePath, yamlContent);

                System.Diagnostics.Debug.WriteLine($"Saved device config: {fileName}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving device config: {ex.Message}");
                MessageBox.Show($"Error saving device config: {ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public List<DeviceManagementModel> LoadAllDeviceManagementModels()
        {
            var deviceModels = new List<DeviceManagementModel>();

            try
            {
                var yamlFiles = Directory.GetFiles(_configDirectory, "*.yaml");
                System.Diagnostics.Debug.WriteLine($"Found {yamlFiles.Length} YAML files in {_configDirectory}");

                foreach (var file in yamlFiles)
                {
                    try
                    {
                        string yamlContent = File.ReadAllText(file);
                        System.Diagnostics.Debug.WriteLine($"Loading file: {Path.GetFileName(file)}");

                        // Try to deserialize as DeviceManagementModel first
                        var deviceModel = _yamlDeserializer.Deserialize<DeviceManagementModel>(yamlContent);

                        if (deviceModel?.DeviceInfo != null)
                        {
                            // Ensure LastSeen is properly set
                            if (deviceModel.DeviceInfo.LastSeen == DateTime.MinValue)
                            {
                                deviceModel.DeviceInfo.LastSeen = deviceModel.LastUpdated != DateTime.MinValue
                                    ? deviceModel.LastUpdated
                                    : deviceModel.CreatedDate;
                            }

                            // Ensure DeviceName and Location have default values if empty
                            if (string.IsNullOrEmpty(deviceModel.DeviceInfo.DeviceName))
                            {
                                deviceModel.DeviceInfo.DeviceName = $"Device_{deviceModel.DeviceInfo.IP}";
                            }

                            if (string.IsNullOrEmpty(deviceModel.DeviceInfo.Location))
                            {
                                deviceModel.DeviceInfo.Location = "Unknown Location";
                            }

                            deviceModels.Add(deviceModel);
                            System.Diagnostics.Debug.WriteLine($"Successfully loaded device: {deviceModel.DeviceInfo.DeviceName} ({deviceModel.DeviceInfo.IP})");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed to load device model from {Path.GetFileName(file)} - DeviceInfo is null");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error loading config file {Path.GetFileName(file)}: {ex.Message}");

                        // Try to load as legacy DeviceConfig format
                        try
                        {
                            string yamlContent = File.ReadAllText(file);
                            var legacyConfig = _yamlDeserializer.Deserialize<DeviceConfig>(yamlContent);

                            if (legacyConfig?.DeviceInfo != null)
                            {
                                var deviceModel = new DeviceManagementModel(legacyConfig.DeviceInfo)
                                {
                                    DeviceNumber = legacyConfig.DeviceNumber,
                                    CreatedDate = legacyConfig.CreatedDate,
                                    LastUpdated = legacyConfig.LastModified
                                };

                                deviceModels.Add(deviceModel);
                                System.Diagnostics.Debug.WriteLine($"Loaded legacy format: {deviceModel.DeviceInfo.DeviceName}");

                                // Convert to new format
                                SaveDeviceManagementModel(deviceModel);
                            }
                        }
                        catch (Exception legacyEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed to load as legacy format: {legacyEx.Message}");
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"Total loaded devices: {deviceModels.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading device configs: {ex.Message}");
                MessageBox.Show($"Error loading device configs: {ex.Message}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return deviceModels.OrderBy(d => d.DeviceNumber).ToList();
        }

        public bool DeleteDeviceManagementModel(DeviceManagementModel deviceModel)
        {
            try
            {
                string macForFile = deviceModel.DeviceInfo.MAC?.Replace(".", "_").Replace(":", "_") ?? "Unknown";
                string fileName = $"Device_{deviceModel.DeviceNumber:D3}_{macForFile}.yaml";
                string filePath = Path.Combine(_configDirectory, fileName);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    System.Diagnostics.Debug.WriteLine($"Deleted device config: {fileName}");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting device config: {ex.Message}");
                MessageBox.Show($"Error deleting device config: {ex.Message}", "Delete Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public bool DeleteDeviceConfig(DeviceInfo deviceInfo)
        {
            try
            {
                var yamlFiles = Directory.GetFiles(_configDirectory, "*.yaml");
                foreach (var file in yamlFiles)
                {
                    string yamlContent = File.ReadAllText(file);
                    bool shouldDelete = false;

                    // Try DeviceManagementModel first
                    try
                    {
                        var deviceModel = _yamlDeserializer.Deserialize<DeviceManagementModel>(yamlContent);
                        if (deviceModel?.DeviceInfo != null &&
                            ((deviceInfo.MAC != null && deviceModel.DeviceInfo.MAC == deviceInfo.MAC) ||
                             (deviceInfo.IP != null && deviceModel.DeviceInfo.IP == deviceInfo.IP)))
                        {
                            shouldDelete = true;
                        }
                    }
                    catch { }

                    // Try DeviceConfig/EnhancedDeviceConfig
                    if (!shouldDelete)
                    {
                        try
                        {
                            var deviceConfig = _yamlDeserializer.Deserialize<DeviceConfig>(yamlContent);
                            if (deviceConfig?.DeviceInfo != null &&
                                ((deviceInfo.MAC != null && deviceConfig.DeviceInfo.MAC == deviceInfo.MAC) ||
                                 (deviceInfo.IP != null && deviceConfig.DeviceInfo.IP == deviceInfo.IP)))
                            {
                                shouldDelete = true;
                            }
                        }
                        catch { }
                    }

                    if (shouldDelete)
                    {
                        File.Delete(file);
                        System.Diagnostics.Debug.WriteLine($"Deleted device config file: {Path.GetFileName(file)}");
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting device config: {ex.Message}");
                MessageBox.Show($"Error deleting device config: {ex.Message}", "Delete Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private int GetNextDeviceNumber()
        {
            try
            {
                var existingModels = LoadAllDeviceManagementModels();
                if (existingModels.Count == 0)
                    return 1;

                int maxNumber = existingModels.Max(m => m.DeviceNumber);
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