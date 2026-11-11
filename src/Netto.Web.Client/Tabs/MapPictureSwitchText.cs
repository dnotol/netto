namespace Netto.Web.Client.Tabs;

// The resource keys MapPictureSwitch.razor chooses, held to MapPictureSwitch.resx and its French by
// MapPictureSwitchTextTests, so a picture added without its name fails a test instead of showing its key on screen.
internal static class MapPictureSwitchText
{
    public static string Name(PlanPicture picture) => picture switch
    {
        PlanPicture.Map => "Map",
        PlanPicture.Sankey => "Sankey",
        _ => throw new ArgumentOutOfRangeException(nameof(picture), picture, "A picture the switch has no name for."),
    };
}
