using Microsoft.Extensions.Localization;
using Netto.Core.CompanyCar;
using Netto.Core.Copyright;
using Netto.Core.Dividends;
using Netto.Core.Rules;
using Netto.Core.SocialContributions;
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

    // The advance payments of a tax, by their total, naming no origin: the example holds none of them,
    // and what is paid is the user's.
    public static SettingPhrase Payments(AdvancePaymentInputs payments, string noneKey, string paidKey)
    {
        var total = payments.ByDeadline.Sum(payment => payment.Value);
        return total == 0m ? new(noneKey, [], null) : new(paidKey, [Formats.Euros(total)], null);
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

// Company year's one disclosure, « Your balance sheet »: the band of INASTI's company contribution its
// balance-sheet total puts the company in, with the year of that balance sheet, the penultimate closed
// financial year as on 1 January of the contribution year, two years before it for a company whose
// accounting year is the calendar year (law of 30.12.1992, art. 91), and the threshold between the two
// bands. None for a year without the contribution's rules.
internal sealed record CompanyYearSettingsView(IReadOnlyList<SettingPhrase> BalanceSheet)
{
    public static CompanyYearSettingsView From(RiverInputs inputs)
    {
        var band = inputs.CompanyContributionBand;
        return new CompanyYearSettingsView(CompanyContributionRules.For(inputs.IncomeYear) is { } rules
            ? [SettingPhrase.Of(CompanyYearCardText.BandLine(band.Value), band.Origin, BalanceSheetYear(inputs.IncomeYear), Formats.RuleAmount(rules.BalanceSheetThreshold))]
            : []);
    }

    // The year whose balance sheet sets the band: the penultimate closed financial year of a company whose
    // accounting year is the calendar year.
    public static string BalanceSheetYear(int incomeYear) => (incomeYear - 2).ToString(Formats.Culture);
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

    public static bool SituationHolds(Refusal? refusal) => refusal?.Input == RefusedInput.MunicipalRate;

    public static bool PrepaidHolds(Refusal? refusal) => refusal?.Input is RefusedInput.TaxWithheld or RefusedInput.DirectorAdvancePayments;

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

    public static bool PaymentsHold(Refusal? refusal) => refusal?.Input == RefusedInput.CompanyAdvancePayments;
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
    public static bool HomeHolds(Refusal? refusal) => refusal?.Input is RefusedInput.CadastralIncome or RefusedInput.ShareLet;
}

// The company car's one disclosure, « Your car »: what the car is, as its papers say, its engine, its
// catalogue value, its CO2, when it was first registered and when it was ordered or leased, what makes
// a plug-in hybrid a fake one and whether a natural gas car is a small one. What the company pays for
// it each year, the director's contribution and the private energy the company pays stay in sight.
// NeedsFacts: no car counts without a catalogue value, so the disclosure opens by itself.
internal sealed record CarSettingsView(IReadOnlyList<SettingPhrase> Car, bool NeedsFacts)
{
    public static CarSettingsView From(RiverInputs inputs)
    {
        var car = inputs.Car;
        var engine = car.Engine.Value;

        return new CarSettingsView(
            [
                SettingPhrase.Of($"Car_Engine_{engine}", car.Engine.Origin),
                SettingPhrase.Of("Car_Catalogue", car.CatalogueValue.Origin, Formats.Euros(car.CatalogueValue.Value)),
                .. Co2(car, engine),
                SettingPhrase.Of("Car_Registered", car.FirstRegistration.Origin, MonthAndYear(car.FirstRegistration.Value)),
                SettingPhrase.Of("Car_Acquired", car.Acquired.Origin, MonthAndYear(car.Acquired.Value)),
                .. Hybrid(car, engine),
                .. NaturalGas(car, engine),
            ],
            car.CatalogueValue.Value <= 0m);
    }

    // Every refusal of a field inside; the yearly costs and the contribution are in sight.
    public static bool CarHolds(Refusal? refusal) =>
        refusal?.Input is RefusedInput.CarCatalogueValue or RefusedInput.CarCo2 or RefusedInput.CarMatchingCo2 or RefusedInput.CarAcquired;

    // Only an engine that burns a fuel has a CO2. A CO2 not given names no origin: the example's
    // nothing is not a value to call an example.
    private static IEnumerable<SettingPhrase> Co2(CarInputs car, CarEngine engine) => engine.EmitsCo2()
        ? [car switch
        {
            { NoCo2Data.Value: true } => SettingPhrase.Of("Car_Co2_NoData", car.NoCo2Data.Origin),
            { Co2.Value: > 0 } => SettingPhrase.Of("Car_Co2", car.Co2.Origin, Formats.Grams(car.Co2.Value)),
            _ => SettingPhrase.Of("Car_Co2_NotGiven", InputOrigin.Yours),
        }]
        : [];

    // A plug-in hybrid is a real one until the user says it is a fake one, which has its matching car's
    // CO2, or none.
    private static IEnumerable<SettingPhrase> Hybrid(CarInputs car, CarEngine engine) => engine.IsPlugInHybrid()
        ? [car switch
        {
            { FakeHybrid.Value: false } => SettingPhrase.Of("Car_NotFakeHybrid", car.FakeHybrid.Origin),
            { NoMatchingCar.Value: true } => SettingPhrase.Of("Car_FakeHybrid_NoMatching", car.FakeHybrid.Origin),
            { MatchingCarCo2.Value: > 0 } => SettingPhrase.Of("Car_FakeHybrid", car.FakeHybrid.Origin, Formats.Grams(car.MatchingCarCo2.Value)),
            _ => SettingPhrase.Of("Car_FakeHybrid_NotGiven", car.FakeHybrid.Origin),
        }]
        : [];

    private static IEnumerable<SettingPhrase> NaturalGas(CarInputs car, CarEngine engine) => engine == CarEngine.NaturalGas
        ? [SettingPhrase.Of(car.SmallNaturalGas.Value ? "Car_SmallGas" : "Car_NotSmallGas", car.SmallNaturalGas.Origin)]
        : [];

    // January 2026: a month begun counts whole, so the day does not matter.
    private static string[] MonthAndYear(DateOnly date) => [Formats.Month(date.Year, date.Month), date.Year.ToString(Formats.Culture)];
}

// The copyright royalty's one disclosure, « Your copyright situation »: what Netto cannot see of the
// director's copyright income and works, whether the four previous years' income averaged within the
// cap, whether they hold an attestation du travail des arts, and the share of their working time
// spent creating protected works. The royalty itself is the split's. The share has no default: it
// says it is not given, and names no origin, until the user gives it, and that it is needed while the
// engine says the royalty that follows the pay is none for want of it (CopyrightFollowing), never while
// it is none for another reason a share would not change. NeedsFacts: then the disclosure opens by
// itself (decided with the operator, 2026-10-23). While the river is refused there is no year to say
// why, and a royalty that follows the pay without a share is taken as needing it, which tests no rule.
internal sealed record CopyrightSettingsView(IReadOnlyList<SettingPhrase> Situation, bool NeedsFacts)
{
    public static CopyrightSettingsView From(RiverInputs inputs, RiverRun run)
    {
        var copyright = inputs.Copyright;
        var needsShare = run.Year is { } year
            ? year.Copyright.Following == CopyrightFollowing.NoCreativeShare
            : copyright.FollowsPay.Value && copyright.CreativeShare.Value is null;

        return new CopyrightSettingsView(
            [
                SettingPhrase.Of(copyright.AverageWithinCap.Value ? "Settings_AverageWithin" : "Settings_AverageAbove", copyright.AverageWithinCap.Origin),
                SettingPhrase.Of(copyright.HoldsAttestation.Value ? "Settings_Attestation_Holds" : "Settings_Attestation_None", copyright.HoldsAttestation.Origin),
                copyright.CreativeShare.Value is { } share
                    ? SettingPhrase.Of("Settings_CreativeShare", copyright.CreativeShare.Origin, Formats.Percentage(share))
                    : SettingPhrase.Of(needsShare ? "Settings_CreativeShare_Needed" : "Settings_CreativeShare_None", InputOrigin.Yours),
            ],
            needsShare);
    }

    // The royalty's own refusals are on the split.
    public static bool SituationHolds(Refusal? refusal) => refusal?.Input == RefusedInput.CreativeShare;
}

// The dividend's one disclosure, « Your shares and earlier profits »: the profits the company kept
// from earlier years and can still pay out, and the withholding rate the user declares for their
// shares, with the year's rate beside the category. What the company keeps this year, the slider,
// stays in sight.
internal sealed record DividendSettingsView(IReadOnlyList<SettingPhrase> Facts)
{
    public static DividendSettingsView From(RiverInputs inputs)
    {
        var earlier = inputs.EarlierProfits;
        var category = inputs.DividendCategory;
        var rate = DividendRules.For(inputs.IncomeYear)?.Rate(category.Value) is { } amount ? Formats.RuleAmount(amount) : "";

        return new DividendSettingsView(
            [
                earlier.Value == 0m
                    ? SettingPhrase.Of("Settings_Earlier_None", earlier.Origin)
                    : SettingPhrase.Of("Settings_Earlier", earlier.Origin, Formats.Euros(earlier.Value)),
                SettingPhrase.Of($"Settings_Category_{category.Value}", category.Origin, rate),
            ]);
    }

    // What is kept this year is refused on a field in sight.
    public static bool SharesHold(Refusal? refusal) => refusal?.Input == RefusedInput.EarlierProfits;
}
