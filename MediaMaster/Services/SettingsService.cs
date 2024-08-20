using System.ComponentModel;
using System.Reflection;
using Windows.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using MediaMaster.Helpers;

namespace MediaMaster.Services;

public partial class SettingsService : ObservableObject
{
    [ObservableProperty] private bool _leaveAppRunning;

    [ObservableProperty] private bool _showExtensions;

    [ObservableProperty] private bool _doNotSendMediaAddedConfirmationNotification;

    public static async Task<T?> ReadSettingAsync<T>(string key)
    {
        if (ApplicationData.Current.LocalSettings.Values.TryGetValue(key, out var obj))
        {
            return await Json.ToObjectAsync<T>((string)obj) ?? default;
        }

        return default;
    }

    public static async Task SaveSettingAsync<T>(string key, T value)
    {
        ApplicationData.Current.LocalSettings.Values[key] = await Json.StringifyAsync(value);
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
            dynamic? task = readMethod.Invoke(this, [property.Name]);
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

        PropertyInfo? property = GetType().GetProperty(propertyName);
        if (property is null) return;

        var value = property.GetValue(this);
        await SaveSettingAsync(propertyName, value);
    }
}