using Netto.Web.Client.Tabs;

namespace Netto.Web.Client.Pages;

// Where the focus goes once the notice of a module taken out leaves, by Undo or Dismiss: to « Add to plan »,
// where the plan's modules are set, on the Plan tab, which draws it over the map; on another tab, which does
// not, the notice having outlived the tab the module was taken out on, to the start of the tab's content,
// never to a button no longer on the page.
internal static class RemovalNoticeFocus
{
    public static RemovalFocus After(PlanTab tab) => tab == PlanTab.Plan ? RemovalFocus.AddToPlan : RemovalFocus.TabContent;
}

internal enum RemovalFocus
{
    AddToPlan,
    TabContent,
}
