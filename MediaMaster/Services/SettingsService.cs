using System.ComponentModel;
using System.Reflection;
using System.Text.Json.Serialization.Metadata;
using Windows.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using MediaMaster.Helpers;

namespace MediaMaster.Services;

public partial class SettingsService : ObservableObject
{
    [ObservableProperty] private bool _doNotSendMediaAddedConfirmationNotification;
    [ObservableProperty] private bool _leaveAppRunning;

    [ObservableProperty] private bool _showExtensions;

    public static async Task<T?> ReadSettingAsync<T>(string key, JsonTypeInfo<T> typeInfo)
    {
        if (ApplicationData.Current.LocalSettings.Values.TryGetValue(key, out var obj))
        {
            return await Json.ToObjectAsync((string)obj, typeInfo) ?? default;
        }

        return default;
    }

    public static async Task SaveSettingAsync<T>(string key, T value, JsonTypeInfo<T> typeInfo)
    {
        ApplicationData.Current.LocalSettings.Values[key] = await Json.StringifyAsync(value, typeInfo);
    }

    public async Task InitializeAsync()
    {
        PropertyChanged += SettingsService_PropertyChanged;

        MethodInfo readMethodGeneric = typeof(SettingsService).GetMethod(nameof(ReadSettingAsync))!;
        PropertyInfo[] properties = GetType().GetProperties();
        foreach (PropertyInfo property in properties)
        {
            // Get the generic method for the property type
            MethodInfo readMethod = readMethodGeneric.MakeGenericMethod(property.PropertyType);

            // Dynamically invoke ReadSettingAsync<T> with the correct type
            dynamic? task = readMethod.Invoke(this, [property.Name, SourceGenerationContext.Default.Boolean]);
            if (task == null) continue;

            // Await the task and retrieve the result
            object value = await task;

            // Convert the result to the property's type and set it
            property.SetValue(this, Convert.ChangeType(value, property.PropertyType));
        }
    }

    private async void SettingsService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        var propertyName = e.PropertyName;
        if (propertyName is null) return;

        Type type = GetType();
        PropertyInfo? property = type.GetProperty(propertyName);
        var value = (bool?)property?.GetValue(this);
        if (value == null) return;

        await SaveSettingAsync(propertyName, (bool)value, SourceGenerationContext.Default.Boolean);
    }
}