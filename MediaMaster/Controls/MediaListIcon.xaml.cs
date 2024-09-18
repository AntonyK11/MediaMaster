using DependencyPropertyGenerator;

namespace MediaMaster.Controls;

[DependencyProperty("Uris", typeof(ICollection<string>), DefaultValueExpression = "new List<string>()")]
public sealed partial class MediaListIcon : UserControl
{
    public MediaListIcon()
    {
        this.InitializeComponent();
    }
}

