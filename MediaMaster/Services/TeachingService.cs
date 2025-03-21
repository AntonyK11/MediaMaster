using MediaMaster.Interfaces.Services;
using MediaMaster.Services.Navigation;
using MediaMaster.Views;

namespace MediaMaster.Services;

/// <summary>
///     Service responsible for managing the teaching tips.
/// </summary>
public sealed class TeachingService : ITeachingService
{
    private readonly Dictionary<int, TeachingTip> _teachingTips = [];

    private int _teachingStep;

    public void Start()
    {
        var previousStep = _teachingStep;
        _teachingStep = 1;
        Steps(previousStep);
    }

    public void NextStep()
    {
        var previousStep = _teachingStep;
        _teachingStep++;
        Steps(previousStep);
    }

    public void PrevStep()
    {
        if (_teachingStep == 0) return;
        var previousStep = _teachingStep;
        _teachingStep--;
        Steps(previousStep);
    }

    public void Reset()
    {
        _teachingStep = 0;
        CloseAllTeachingTips();
    }

    /// <summary>
    ///     Configures the teaching tip for a specific step.
    /// </summary>
    /// <param name="step"> The step to configure. </param>
    /// <param name="teachingTip"> The teaching tip to configure. </param>
    public void Configure(int step, TeachingTip teachingTip)
    {
        if (!_teachingTips.TryAdd(step, teachingTip))
        {
            _teachingTips[step] = teachingTip;
        }
    }

    private void Steps(int previousStep)
    {
        CloseAllTeachingTips();

        switch (previousStep)
        {
            case 1:
                //App.GetService<MainNavigationService>().NavigateTo(typeof(HomePage).FullName);
                break;
            case 2:
                App.Flyout?.CloseFlyout();
                break;
            case 3:
                App.Flyout?.CloseFlyout();
                break;
        }

        switch (_teachingStep)
        {
            case 1:
                App.GetService<MainNavigationService>().NavigateTo(typeof(HomePage).FullName);
                break;
            case 2:
                App.Flyout?.ShowFlyout();
                break;
            case 3:
                App.Flyout?.ShowFlyout();
                break;
        }

        SetTeachingTipProperty(_teachingStep, "IsOpen", true);

        if (_teachingStep >= _teachingTips.Keys.Last())
        {
            App.GetService<SettingsService>().TutorialWasShown = true;
        }
    }

    private void CloseAllTeachingTips()
    {
        foreach ((_, TeachingTip teachingTip) in _teachingTips)
        {
            teachingTip.IsOpen = false;
        }
    }

    /// <summary>
    ///     Sets a property of the teaching tip of a specific step.
    /// </summary>
    /// <param name="step"> The step of the teaching tip. </param>
    /// <param name="property"> The property to set. </param>
    /// <param name="value"> The value to set. </param>
    private void SetTeachingTipProperty(int step, string property, object value)
    {
        if (_teachingTips.TryGetValue(step, out TeachingTip? teachingTip))
        {
            typeof(TeachingTip).GetProperty(property)?.SetValue(teachingTip, value);
        }
    }
}