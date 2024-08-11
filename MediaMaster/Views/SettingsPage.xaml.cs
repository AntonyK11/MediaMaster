using Microsoft.UI.Xaml.Controls;
using MediaMaster.ViewModels;
using WinUI3Localizer;
using Windows.Foundation;
using MediaMaster.Services;

namespace MediaMaster.Views;

public sealed partial class SettingsPage
{
    public SettingsViewModel ViewModel { get; }
    public SettingsService Settings { get; }

    public SettingsPage()
    {
        ViewModel = App.GetService<SettingsViewModel>();
        Settings = App.GetService<SettingsService>();
        InitializeComponent();
        AdjustComboBoxWidth();
    }

    private void AdjustComboBoxWidth()
    {
        var maxWidth = ViewModel.AvailableLanguages
            .Select(l => l.UidKey.GetLocalizedString())
            .Max(item =>
            {
                TextBlock textBlock = new TextBlock
                {
                    Text = item,
                    FontFamily = ComboBox.FontFamily,
                    FontSize = ComboBox.FontSize,
                    FontStyle = ComboBox.FontStyle,
                    FontWeight = ComboBox.FontWeight
                };

                textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                return textBlock.DesiredSize.Width;
            });

        ComboBox.Width = maxWidth + 52; // 52 is to take care of button width and the menu's margins
    }
}
