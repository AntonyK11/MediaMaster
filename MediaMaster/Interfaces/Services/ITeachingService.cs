namespace MediaMaster.Interfaces.Services;

/// <summary>
///     Service responsible for managing the teaching tips.
/// </summary>
public interface ITeachingService
{
    void NextStep();

    void PrevStep();

    void Reset();
    
    /// <summary>
    ///     Configures the teaching tip for a specific step.
    /// </summary>
    /// <param name="step"> The step to configure. </param>
    /// <param name="teachingTip"> The teaching tip to configure. </param>
    /// <exception cref="ArgumentException"> Thrown when the step is already configured by another teaching tip. </exception>
    void Configure(int step, TeachingTip teachingTip);
}

