using Microsoft.Extensions.Localization;
using Netto.Core.DirectorPay;
using Netto.Core.Rent;
using Netto.Core.Simulation;
using Netto.Web.Client.DirectorPay;

namespace Netto.Web.Client.River;

// The settings a user rarely changes sit behind a disclosure, folded to one line that states their
// values (Disclosure). These are those lines: what each says, a phrase per value, worded by the card's
// texts; and which refusals open each, since a refused field is never hidden. A lever's facts about
// itself, what Netto cannot see of it and the user sets once, sit behind one too, while the amounts
// the plan decides stay in sight; a lever that cannot count without them says so (NeedsFacts), and
// its disclosure opens by itself the first time the card is drawn.

// One value a disclosure's line states: a key of the card's texts with its arguments, and the origin
// it names beside the value, only for what Netto assumed, the example's or a default. What the user
// gave names none, and what comes from elsewhere says where in its words: as the pay slip says, a
// municipality's rate.
internal sealed record SettingPhrase(string Key, IReadOnlyList<string> Args, InputOrigin? Origin)
{
    public static SettingPhrase Of(string key, InputOrigin origin, params string[] args) => new(key, args, Assumed(origin));

    // The advance payments of a tax, by their total: the example's while all four are.
    public static SettingPhrase Payments(AdvancePaymentInputs payments, string noneKey, string paidKey)
    {
        var total = payments.ByDeadline.Sum(payment => payment.Value);
        var origin = payments.ByDeadline.All(payment => payment.Origin == InputOrigin.Example) ? InputOrigin.Example : InputOrigin.Yours;
        return total == 0m ? Of(noneKey, origin) : Of(paidKey, origin, Formats.Euros(total));
    }

    private static InputOrigin? Assumed(InputOrigin origin) => origin is InputOrigin.Example or InputOrigin.Default ? origin : null;
}

// A value of a disclosure's line once worded, with the origin it names beside it. Public because
// Disclosure, a component, takes it.
public sealed record SettingValue(string Text, InputOrigin? Origin);

// The phrases of a disclosure's line worded by the texts of the card that shows it.
internal static class SettingWords
{
    public static IReadOnlyList<SettingValue> Of(IStringLocalizer texts, IEnumerable<SettingPhrase> phrases) =>
        [.. phrases.Select(phrase => new SettingValue(texts[phrase.Key, [.. phrase.Args]].Value, phrase.Origin))];
}

// Director pay's two disclosures: « Your situation », who pays the contributions, the municipal rate
// and whether the director is in the first years of a first self-employed activity; and « Tax paid
// during the year », what was withheld on the pay and the director's own advance payments.
internal sealed record DirectorPaySettingsView(IReadOnlyList<SettingPhrase> Situation, IReadOnlyList<SettingPhrase> Prepaid)
{
    public static DirectorPaySettingsView From(RiverInputs inputs, RiverRun run)
    {
        var starter = inputs.TaxPrepayments.Starter;
        var firstYear = (inputs.IncomeYear - 2).ToString(Formats.Culture);

        return new DirectorPaySettingsView(
            [
                SettingPhrase.Of(DirectorPayCardText.SituationPaidBy(inputs.ContributionsPaidBy.Value), inputs.ContributionsPaidBy.Origin),
                Rate(inputs.Municipality),
                SettingPhrase.Of(starter.Value ? "Situation_Starter" : "Situation_NotStarter", starter.Origin, firstYear),
            ],
            [
                Withheld(WithheldFieldView.From(inputs, run), run),
                SettingPhrase.Payments(inputs.TaxPrepayments.Paid, "Prepaid_None", "Prepaid_Paid"),
            ]);
    }

    public static bool SituationHolds(string? error) => error == GrossToNet.MunicipalRateOutOfRange;

    public static bool PrepaidHolds(string? error) => error is GrossToNet.TaxWithheldNegative or GrossToNet.TaxAdvancePaymentNegative;

    // A municipality's rate says whose it is, and a rate of the user's over it whose it replaces.
    private static SettingPhrase Rate(MunicipalityChoice choice)
    {
        var rate = Formats.MunicipalRate(choice.Rate);
        return (choice.Origin, choice.Municipality) switch
        {
            (InputOrigin.Derived, { } municipality) => SettingPhrase.Of("Situation_Rate_Official", InputOrigin.Derived, municipality.Name, rate),
            (InputOrigin.Yours, { } municipality) => SettingPhrase.Of("Situation_Rate_Overridden", InputOrigin.Yours, rate, municipality.Name),
            _ => SettingPhrase.Of("Situation_Rate", choice.Origin, rate),
        };
    }

    // The pay slip's withholding says so, with its amount once the pay is there to derive it from.
    private static SettingPhrase Withheld(WithheldFieldView withheld, RiverRun run) => withheld switch
    {
        { IsYours: true } => SettingPhrase.Of("Prepaid_Withheld", InputOrigin.Yours, Formats.Euros(withheld.Amount)),
        _ when run.Pay is null => SettingPhrase.Of("Prepaid_Withheld_AsPaySlip", InputOrigin.Derived),
        _ => SettingPhrase.Of("Prepaid_Withheld_PaySlip", InputOrigin.Derived, Formats.Euros(withheld.Amount)),
    };
}

// Corporate tax's two disclosures: « Your company », whether the reduced rate is open to it and which
// financial year the income year is; and « Advance payments », what the company paid by the deadlines,
// with the surcharge they leave once the year runs.
internal sealed record CorporateTaxSettingsView(IReadOnlyList<SettingPhrase> Company, IReadOnlyList<SettingPhrase> Payments)
{
    public static CorporateTaxSettingsView From(RiverInputs inputs, RiverRun run)
    {
        IEnumerable<SettingPhrase> surcharge = run.Year switch
        {
            null => [],
            { AdvancePayments.Surcharge: > 0m and var left } => [SettingPhrase.Of("Payments_Surcharge", InputOrigin.Derived, Formats.Euros(left))],
            _ => [SettingPhrase.Of("Payments_NoSurcharge", InputOrigin.Derived)],
        };

        return new CorporateTaxSettingsView(
            [
                SettingPhrase.Of(inputs.SmallCompany.Value ? "Company_Small" : "Company_NotSmall", inputs.SmallCompany.Origin),
                SettingPhrase.Of(CorporateTaxCardText.CompanyFinancialYear(inputs.FinancialYear.Value), inputs.FinancialYear.Origin),
            ],
            [
                SettingPhrase.Payments(inputs.AdvancePayments, "Payments_None", "Payments_Paid"),
                .. surcharge,
            ]);
    }

    public static bool PaymentsHold(string? error) => error == YearSimulation.AdvancePaymentNegative;
}

// Home office rent's one disclosure, « Your home »: the cadastral income of the home and the share of
// it let to the company, which cap the rent. The rent itself stays in sight. NeedsFacts: no rent can
// be given until a part of the home is let (the cap is none), so the disclosure opens by itself.
internal sealed record RentSettingsView(IReadOnlyList<SettingPhrase> Home, bool NeedsFacts)
{
    public static RentSettingsView From(RiverInputs inputs)
    {
        var rent = inputs.Rent;

        return new RentSettingsView(
            [
                SettingPhrase.Of("Home_CadastralIncome", rent.CadastralIncome.Origin, Formats.Euros(rent.CadastralIncome.Value)),
                SettingPhrase.Of("Home_Share", rent.ShareLet.Origin, Formats.Percentage(rent.ShareLet.Value)),
            ],
            rent.CadastralIncome.Value <= 0m || rent.ShareLet.Value <= 0m);
    }

    // The rent's own refusals (negative, above the cap, beside an office allowance) are on a field in
    // sight.
    public static bool HomeHolds(string? error) => error is RentCalculator.CadastralIncomeNegative or RentCalculator.ShareLetOutOfRange;
}
