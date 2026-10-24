using Netto.Core.SocialContributions;

namespace Netto.Web.Client.River;

// The resource keys the Company year card builds from the band of INASTI's company contribution, gone
// through by the card, the line of its disclosure (SettingsViews) and its tests alike.
internal static class CompanyYearCardText
{
    // A band as its choice reads, with the threshold between the two.
    public static string Band(CompanyContributionBand band) => $"Band_{band}";

    // A band as the line of « Your balance sheet » states it, with the year of the balance sheet.
    public static string BandLine(CompanyContributionBand band) => $"BandLine_{band}";
}
