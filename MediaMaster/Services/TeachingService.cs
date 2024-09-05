using MediaMaster.Interfaces.Services;

namespace MediaMaster.Services;

/// <summary>
///     Service responsible for managing the teaching tips.
/// </summary>
public class TeachingService : ITeachingService
{
    private readonly Dictionary<int, TeachingTip> _teachingTips = [];

    private int _teachingStep;

    public void NextStep()
    {
        _teachingStep++;
        Steps();
    }

    public void PrevStep()
    {
        if (_teachingStep == 0) return;
        _teachingStep--;
        Steps();
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

    private void Steps()
    {
        CloseAllTeachingTips();

        switch (_teachingStep)
        {
            case 1:
                Debug.WriteLine("Step 1");
                break;
            case 2:
                Debug.WriteLine("Step 2");
                break;
            case 3:
                Debug.WriteLine("Step 3");
                break;
        }

        SetTeachingTipProperty(_teachingStep, "IsOpen", true);
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