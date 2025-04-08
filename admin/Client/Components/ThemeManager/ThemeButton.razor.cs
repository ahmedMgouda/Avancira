using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Avancira.Admin.Client.Components.ThemeManager;

public partial class ThemeButton
{
    [Parameter]
    public EventCallback<MouseEventArgs> OnClick { get; set; }
}
