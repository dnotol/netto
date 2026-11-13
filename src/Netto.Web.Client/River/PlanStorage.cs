using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Netto.Core.CompanyCar;
using Netto.Core.Dividends;
using Netto.Core.PersonalTax;
using Netto.Core.SocialContributions;
using Netto.Web.Client.DirectorPay;

namespace Netto.Web.Client.River;

// A plan the user saved: its name and the river's inputs as they were, or null when what was stored
// cannot be read back as this version of Netto reads a plan (Stored keeps it as it was, so it is
// written back untouched rather than lost).
internal sealed record SavedPlan(string Id, string Name, RiverInputs? Inputs, JsonElement Stored)
{
    public bool Readable => Inputs is not null;
}

// Every saved plan, the one starred « Today », the user's current setup, and the one the river was
// last opened from.
internal sealed record PlanBook(IReadOnlyList<SavedPlan> Plans, string? Today, string? Opened)
{
    public static PlanBook Empty { get; } = new([], null, null);
}

// How saved plans are kept in the browser: one JSON document under Key, carrying the version of its
// format. A plan is stored as plain values with their origins, never as RiverInputs itself, so the
// format changes only when this file does: a new input of the river is a new field here, a new Version
// (RiverInputsShapeTests fails until it is) and a patch that brings a plan of the version before to it.
// Reading is strict: a missing field, a field this version does not know, an unknown option or
// municipality makes that plan unreadable, never a plan with a default in place of what was saved. A
// document that cannot be read at all, or comes from a newer version, is refused whole.
//
// A plan of an older version is brought to this one a version at a time, on a copy: each patch adds
// what the version before never held, written out here as that version's successor stored it, and
// refuses a plan that already holds it, since that version never wrote one; parsing the copy refuses a
// plan holding a member twice. It is then read as a plan of this version is, and written back in this
// version's format; a plan that cannot be read is kept exactly as it was stored. What a patch adds is
// frozen here, never read from the river's example, which may change: a plan saved before stays as it
// was saved. Version 1 held no company car, so its plans are read without one (decided 2026-10-22),
// versions 1 and 2 no copyright royalty, so theirs are read without one, versions 1 to 3 no
// provisional contributions billed by the fund, so theirs are read with none given, versions 3 and 4 a
// royalty that is always fixed: a royalty the user gave opens fixed at it, as it was saved, and one
// never given follows the pay, as a royalty added now does (2026-10-23), versions 2 to 5 no private
// cost of the car, so theirs are read with none given (2026-10-23), and versions 1 to 6 no band of
// INASTI's company contribution, so theirs open in the lower band, the default a plan starts from, a
// plan whose running costs held the contribution then counting it twice until they are lowered
// (decided unattended, 2026-10-25), nor the rate of the fund's management fee, so theirs are read
// with none given, and versions 1 to 7 no partner, so theirs are read without one, no income given for
// one and neither of the household's statuses, and versions 1 to 8 no dependants, so theirs are read with
// none, and no word of whether there is a partner: version 8's household was its partner, so a plan holding
// it declared one, the user's, and any other has none assumed, as adding the household does, and versions 1
// to 9 no day the director has the car from or gives it back on, so theirs hold the car all year, assumed,
// as they counted it, nor the IT and phone the company provides, so theirs hold none, out of the plan. The car's
// engine, CO2, order date and statuses joined version 2 without raising it,
// since version 2 had not left the car's story branch; a plan saved by the car's first build does not
// open (operator, 2026-10-22). The rest of the royalty's story joins version 3 the same way, and the
// fund's fee version 7 (decided unattended, 2026-10-22 and 2026-10-25), and the IT and phone version 10,
// in the story that raised it (2026-11-13).
internal static class PlanStorage
{
    public const string Key = "netto.plans";

    // Where a document that cannot be read is kept aside before anything is written over it.
    public const string UnreadableKey = "netto.plans.unreadable";

    // 2: the company car. 3: the copyright royalty. 4: what the fund bills each quarter. 5: whether the
    // royalty follows the pay. 6: what the same car would cost privately. 7: the band of INASTI's company
    // contribution and the rate of the fund's management fee. 8: the partner. 9: whether there is a partner,
    // and the dependants. 10: the days the director has the car, and the IT and phone the company provides.
    public const int Version = 10;

    private const int FirstVersion = 1;

    // The patch from each older version to the next: Patches[n - FirstVersion] brings a plan version n
    // stored to version n + 1's shape, or refuses it. A new version adds its own.
    private static readonly Func<JsonObject, bool>[] Patches = [ToVersion2, ToVersion3, ToVersion4, ToVersion5, ToVersion6, ToVersion7, ToVersion8, ToVersion9, ToVersion10];

    // A plan of an older version holding a member twice is refused as it is parsed.
    private static readonly JsonDocumentOptions OneOfEachMember = new() { AllowDuplicateProperties = false };

    // The car a plan of version 1 never held, as version 2 stored the example's: a new electric car
    // first registered and ordered on 1 January 2026, with nothing paid.
    private const string CarOfVersion2 = """
        {
            "catalogueValue": {"value": 0, "origin": "Example"},
            "firstRegistration": {"value": "2026-01-01", "origin": "Example"},
            "yearlyCosts": {"value": 0, "origin": "Example"},
            "companyPaysPrivateEnergy": {"value": false, "origin": "Default"},
            "personalContribution": {"value": 0, "origin": "Example"},
            "engine": {"value": "Electric", "origin": "Default"},
            "co2": {"value": 0, "origin": "Example"},
            "noCo2Data": {"value": false, "origin": "Default"},
            "acquired": {"value": "2026-01-01", "origin": "Example"},
            "fakeHybrid": {"value": false, "origin": "Default"},
            "matchingCarCo2": {"value": 0, "origin": "Example"},
            "noMatchingCar": {"value": false, "origin": "Default"},
            "smallNaturalGas": {"value": false, "origin": "Default"}
        }
        """;

    // The royalty a plan of version 2 never held, as version 3 stored the example's: none paid, the four
    // previous years assumed within the cap, no attestation and no creative share given.
    private const string RoyaltyOfVersion3 = """
        {
            "royalty": {"value": 0, "origin": "Example"},
            "averageWithinCap": {"value": true, "origin": "Default"},
            "holdsAttestation": {"value": false, "origin": "Default"},
            "creativeShare": {"value": null, "origin": "Yours"}
        }
        """;

    // The partner a plan of version 7 never held, as version 8 stored the example's: no income given for
    // one, neither partner assumed 66, the year assumed taxed jointly.
    private const string HouseholdOfVersion8 = """
        {
            "partnerIncome": {"value": 0, "origin": "Example"},
            "bothAged66": {"value": false, "origin": "Default"},
            "taxedSeparately": {"value": false, "origin": "Default"}
        }
        """;

    // The dependants a plan of version 8 never held, as version 9 stored the example's: none, and no single
    // parent assumed.
    private const string DependantsOfVersion9 = """
        {
            "children": {"value": 0, "origin": "Example"},
            "childrenUnder3": {"value": 0, "origin": "Example"},
            "singleParent": {"value": false, "origin": "Default"},
            "dependantsAged66InCare": {"value": 0, "origin": "Example"},
            "otherDependants": {"value": 0, "origin": "Example"}
        }
        """;

    // A partner a plan of version 8 declared by holding its household, and none assumed: a partner is the
    // viewer's to tick, as adding the household now opens (2026-11-12).
    private const string PartnerDeclared = """{"value": "MarriedOrLegalCohabitant", "origin": "Yours"}""";

    private const string PartnerAssumed = """{"value": "Alone", "origin": "Default"}""";

    // No day the car is had from or given back on, which a plan of version 9 never held: held all year, assumed.
    private const string HeldAllYear = """{"value": null, "origin": "Default"}""";

    // The IT and phone a plan of version 9 never held, as version 10 stored the example's: no piece, no internet
    // connection assumed, nothing paid, provided all year, and no private cost given.
    private const string EquipmentOfVersion10 = """
        {
            "pcs": {"value": 0, "origin": "Example"},
            "tablets": {"value": 0, "origin": "Example"},
            "mobilePhones": {"value": 0, "origin": "Example"},
            "internet": {"value": false, "origin": "Default"},
            "phoneSubscriptions": {"value": 0, "origin": "Example"},
            "yearlyCosts": {"value": 0, "origin": "Example"},
            "contribution": {"value": 0, "origin": "Example"},
            "privateCost": {"value": null, "origin": "Yours"},
            "providedFrom": {"value": null, "origin": "Default"},
            "returnedOn": {"value": null, "origin": "Default"}
        }
        """;

    // An amount with no default, until the user gives it.
    private const string NoneGiven = """{"value": null, "origin": "Yours"}""";

    // The plans stored, or Unreadable when the document is there but cannot be read.
    public static (PlanBook Book, bool Unreadable) Read(string? json)
    {
        if (json is null)
            return (PlanBook.Empty, false);

        StoredBook? stored;
        try
        {
            stored = JsonSerializer.Deserialize(json, PlanJson.Default.StoredBook);
        }
        catch (JsonException)
        {
            return (PlanBook.Empty, true);
        }

        if (stored is null || stored.Version is < FirstVersion or > Version)
            return (PlanBook.Empty, true);

        var plans = stored.Plans.Select(plan => stored.Version == Version ? Plan(plan) : PlanOfOlderVersion(plan, stored.Version)).ToList();
        return (new PlanBook(plans, stored.Today, stored.Opened), false);
    }

    public static string Write(PlanBook book) => JsonSerializer.Serialize(
        new StoredBook(Version, book.Today, book.Opened, [.. book.Plans.Select(plan => new StoredPlan(plan.Id, plan.Name, plan.Stored))]),
        PlanJson.Default.StoredBook);

    // A plan as it is stored, for a new plan.
    public static JsonElement Store(RiverInputs inputs) => JsonSerializer.SerializeToElement(Stored(inputs), PlanJson.Default.StoredInputs);

    // A plan of this version, kept as it was stored.
    private static SavedPlan Plan(StoredPlan plan) =>
        new(plan.Id, plan.Name, Inputs(() => JsonSerializer.Deserialize(plan.Inputs, PlanJson.Default.StoredInputs)), plan.Inputs.Clone());

    // Brought to this version and kept in its format, so the book is written back in one version; a plan
    // it cannot read is kept as it was stored, never as patched.
    private static SavedPlan PlanOfOlderVersion(StoredPlan plan, int version) =>
        Inputs(() => Upgraded(plan.Inputs, version)?.Deserialize(PlanJson.Default.StoredInputs)) is { } inputs
            ? new SavedPlan(plan.Id, plan.Name, inputs, Store(inputs))
            : new SavedPlan(plan.Id, plan.Name, null, plan.Inputs.Clone());

    // A copy of a plan of an older version in this version's shape, patched one version at a time, or
    // null when it is not an object or a patch refuses it.
    private static JsonObject? Upgraded(JsonElement stored, int version)
    {
        if (JsonNode.Parse(stored.GetRawText(), documentOptions: OneOfEachMember) is not JsonObject plan)
            return null;

        for (var from = version; from < Version; from++)
        {
            if (!Patches[from - FirstVersion](plan))
                return null;
        }

        return plan;
    }

    // Version 2 brought the company car: a plan of version 1 holds none, out of the plan.
    private static bool ToVersion2(JsonObject plan) => Add(plan["composition"], "car", "false") && Add(plan, "car", CarOfVersion2);

    // Version 3 brought the copyright royalty: a plan of version 2 holds none, out of the plan.
    private static bool ToVersion3(JsonObject plan) => Add(plan["composition"], "copyright", "false") && Add(plan, "copyright", RoyaltyOfVersion3);

    // Version 4 brought what the fund bills each quarter: none given.
    private static bool ToVersion4(JsonObject plan) => Add(plan, "billedContributions", NoneGiven);

    // Version 5 brought whether the royalty follows the pay. Before, a royalty was always fixed: the one the
    // user gave opens fixed at it, the user's choice; one never given follows the pay, as a royalty added
    // now does (2026-10-23).
    private static bool ToVersion5(JsonObject plan) =>
        plan["copyright"] is JsonObject copyright
        && Add(copyright, "followsPay", Origin(copyright["royalty"]) == InputOrigin.Yours
            ? """{"value": false, "origin": "Yours"}"""
            : """{"value": true, "origin": "Default"}""");

    // Version 6 brought what the same car would cost privately: none given.
    private static bool ToVersion6(JsonObject plan) => Add(plan["car"], "privateCost", NoneGiven);

    // Version 7 brought the band of INASTI's company contribution, the lower one, the default, and the
    // rate of the fund's management fee, none given.
    private static bool ToVersion7(JsonObject plan) =>
        Add(plan, "companyContributionBand", """{"value": "Lower", "origin": "Default"}""") && Add(plan, "fundFeeRate", NoneGiven);

    // Version 8 brought the partner: a plan of version 7 holds none, out of the plan.
    private static bool ToVersion8(JsonObject plan) => Add(plan["composition"], "household", "false") && Add(plan, "household", HouseholdOfVersion8);

    // Version 9 brought whether there is a partner, version 8's household being its partner, and the
    // dependants: none. A composition that is not an object, or a household flag that is not a boolean, is
    // not a plan version 8 wrote: refused.
    private static bool ToVersion9(JsonObject plan) =>
        plan["composition"] is JsonObject composition && composition["household"] is JsonValue held && held.TryGetValue<bool>(out var partnerHeld)
        && Add(plan["household"], "status", partnerHeld ? PartnerDeclared : PartnerAssumed) && Add(plan["household"], "dependants", DependantsOfVersion9);

    // Version 10 brought the day the director has the car from and the day they give it back on: none, the car
    // held all year, as a plan of version 9 counted it.
    private static bool ToVersion10(JsonObject plan) =>
        Add(plan["car"], "heldFrom", HeldAllYear) && Add(plan["car"], "returnedOn", HeldAllYear)
        && Add(plan["composition"], "equipment", "false") && Add(plan, "equipment", EquipmentOfVersion10);

    // Adds a member the version before never held, or refuses a plan that already holds it: that version
    // never wrote one.
    private static bool Add(JsonNode? parent, string member, string json)
    {
        if (parent is not JsonObject members || members.ContainsKey(member))
            return false;

        members.Add(member, JsonNode.Parse(json));
        return true;
    }

    // The origin a stored value names, read as a plan's are, or none when it names none.
    private static InputOrigin? Origin(JsonNode? stored) =>
        stored is JsonObject value && value["origin"] is { } origin ? origin.Deserialize(PlanJson.Default.InputOrigin) : null;

    // The river's inputs a plan stores, or null when they cannot be read as this version reads a plan.
    private static RiverInputs? Inputs(Func<StoredInputs?> read)
    {
        try
        {
            return read() is { } stored ? Inputs(stored) : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static StoredInputs Stored(RiverInputs inputs) => new(
        inputs.IncomeYear,
        inputs.Company is { } company ? new StoredCompany(Stored(company.Revenue), Stored(company.RunningCosts)) : null,
        new StoredComposition(
            inputs.Composition.Vouchers, inputs.Composition.Allowances, inputs.Composition.Rent, inputs.Composition.Equipment, inputs.Composition.Car, inputs.Composition.Copyright,
            inputs.Composition.Household),
        new StoredVouchers(Stored(inputs.Vouchers.MealVoucherDays), Stored(inputs.Vouchers.EcoVouchers), Stored(inputs.Vouchers.SportCultureVouchers)),
        new StoredAllowances(Stored(inputs.Allowances.Office), Stored(inputs.Allowances.Internet), Stored(inputs.Allowances.Computer), Stored(inputs.Allowances.Phone)),
        new StoredRent(Stored(inputs.Rent.CadastralIncome), Stored(inputs.Rent.ShareLet), Stored(inputs.Rent.Rent)),
        Stored(inputs.Equipment),
        Stored(inputs.Car),
        Stored(inputs.Copyright),
        Stored(inputs.SmallCompany),
        Stored(inputs.FinancialYear),
        Stored(inputs.CompanyContributionBand),
        Stored(inputs.AdvancePayments),
        Stored(inputs.DirectorPay),
        new StoredMunicipality(inputs.Municipality.Municipality?.InsCode, inputs.Municipality.Rate, inputs.Municipality.Origin),
        new StoredTaxPrepayments(Stored(inputs.TaxPrepayments.Withheld), Stored(inputs.TaxPrepayments.Paid), Stored(inputs.TaxPrepayments.Starter)),
        Stored(inputs.Reserve),
        Stored(inputs.DividendCategory),
        Stored(inputs.EarlierProfits),
        Stored(inputs.ContributionsPaidBy),
        Stored(inputs.BilledContributions),
        Stored(inputs.FundFeeRate),
        Stored(inputs.Household));

    // Null when the year has no municipal rates or no longer lists the municipality saved: a plan is
    // never opened with another rate than the one it was saved with. A plan is a Walloon resident's, the
    // one region with rules, so its format holds no region.
    private static RiverInputs? Inputs(StoredInputs stored)
    {
        if (MunicipalRates.For(stored.IncomeYear, Region.Wallonia) is not { } rates)
            return null;

        Municipality? municipality = null;
        if (stored.Municipality.InsCode is { } code && (municipality = rates.Municipalities.FirstOrDefault(listed => listed.InsCode == code)) is null)
            return null;

        return new RiverInputs(
            stored.IncomeYear,
            stored.Company is { } company ? new CompanyFigures(Input(company.Revenue), Input(company.RunningCosts)) : null,
            new PlanComposition(
                stored.Composition.Vouchers, stored.Composition.Allowances, stored.Composition.Rent, stored.Composition.Equipment, stored.Composition.Car, stored.Composition.Copyright,
                stored.Composition.Household),
            new VoucherInputs(Input(stored.Vouchers.MealVoucherDays), Input(stored.Vouchers.EcoVouchers), Input(stored.Vouchers.SportCultureVouchers)),
            new AllowanceInputs(Input(stored.Allowances.Office), Input(stored.Allowances.Internet), Input(stored.Allowances.Computer), Input(stored.Allowances.Phone)),
            new RentInputs(Input(stored.Rent.CadastralIncome), Input(stored.Rent.ShareLet), Input(stored.Rent.Rent)),
            Input(stored.Equipment),
            Input(stored.Car),
            new CopyrightInputs(
                Input(stored.Copyright.Royalty), Input(stored.Copyright.FollowsPay), Input(stored.Copyright.AverageWithinCap), Input(stored.Copyright.HoldsAttestation), Input(stored.Copyright.CreativeShare)),
            Input(stored.SmallCompany),
            Input(stored.FinancialYear),
            Input(stored.CompanyContributionBand),
            Input(stored.AdvancePayments),
            Input(stored.DirectorPay),
            new MunicipalityChoice(rates, municipality, stored.Municipality.Rate, stored.Municipality.Origin),
            new TaxPrepaymentInputs(Input(stored.TaxPrepayments.Withheld), Input(stored.TaxPrepayments.Paid), Input(stored.TaxPrepayments.Starter)),
            Input(stored.Reserve),
            Input(stored.DividendCategory),
            Input(stored.EarlierProfits),
            Input(stored.ContributionsPaidBy),
            Input(stored.BilledContributions),
            Input(stored.FundFeeRate),
            Input(stored.Household));
    }

    private static StoredHousehold Stored(HouseholdInputs household) => new(
        Stored(household.PartnerIncome), Stored(household.BothAged66), Stored(household.TaxedSeparately), Stored(household.Status),
        new StoredDependants(
            Stored(household.Dependants.Children), Stored(household.Dependants.ChildrenUnder3), Stored(household.Dependants.SingleParent),
            Stored(household.Dependants.DependantsAged66InCare), Stored(household.Dependants.OtherDependants)));

    private static HouseholdInputs Input(StoredHousehold stored) => new(
        Input(stored.PartnerIncome), Input(stored.BothAged66), Input(stored.TaxedSeparately), Input(stored.Status),
        new DependantInputs(
            Input(stored.Dependants.Children), Input(stored.Dependants.ChildrenUnder3), Input(stored.Dependants.SingleParent),
            Input(stored.Dependants.DependantsAged66InCare), Input(stored.Dependants.OtherDependants)));

    // What the example holds none of is stored as the example's, as the format held it before
    // InputOrigin.None, so a plan saved here still opens in the build before; read back, an example's
    // amount stored at nothing is the example's nothing again, in a plan of any version and in a file
    // imported. A nothing the user typed is theirs, stored and read back as such.
    private static StoredValue<T> Stored<T>(Input<T> input) =>
        new(input.Value, input.Origin == InputOrigin.None ? InputOrigin.Example : input.Origin);

    private static Input<T> Input<T>(StoredValue<T> stored) =>
        new(stored.Value, stored.Origin == InputOrigin.Example && stored.Value is 0m or 0 ? InputOrigin.None : stored.Origin);

    private static StoredEquipment Stored(EquipmentInputs set) => new(
        Stored(set.Pcs), Stored(set.Tablets), Stored(set.MobilePhones), Stored(set.Internet), Stored(set.PhoneSubscriptions), Stored(set.YearlyCosts),
        Stored(set.Contribution), Stored(set.PrivateCost), Stored(set.ProvidedFrom), Stored(set.ReturnedOn));

    private static EquipmentInputs Input(StoredEquipment stored) => new(
        Input(stored.Pcs), Input(stored.Tablets), Input(stored.MobilePhones), Input(stored.Internet), Input(stored.PhoneSubscriptions), Input(stored.YearlyCosts),
        Input(stored.Contribution), Input(stored.PrivateCost), Input(stored.ProvidedFrom), Input(stored.ReturnedOn));

    private static StoredCar Stored(CarInputs car) => new(
        Stored(car.CatalogueValue), Stored(car.FirstRegistration), Stored(car.YearlyCosts), Stored(car.CompanyPaysPrivateEnergy), Stored(car.PersonalContribution),
        Stored(car.Engine), Stored(car.Co2), Stored(car.NoCo2Data), Stored(car.Acquired), Stored(car.FakeHybrid), Stored(car.MatchingCarCo2),
        Stored(car.NoMatchingCar), Stored(car.SmallNaturalGas), Stored(car.PrivateCost), Stored(car.HeldFrom), Stored(car.ReturnedOn));

    private static CarInputs Input(StoredCar stored) => new(
        Input(stored.CatalogueValue), Input(stored.FirstRegistration), Input(stored.YearlyCosts), Input(stored.CompanyPaysPrivateEnergy), Input(stored.PersonalContribution),
        Input(stored.Engine), Input(stored.Co2), Input(stored.NoCo2Data), Input(stored.Acquired), Input(stored.FakeHybrid), Input(stored.MatchingCarCo2),
        Input(stored.NoMatchingCar), Input(stored.SmallNaturalGas), Input(stored.PrivateCost), Input(stored.HeldFrom), Input(stored.ReturnedOn));

    private static StoredCopyright Stored(CopyrightInputs copyright) => new(
        Stored(copyright.Royalty), Stored(copyright.FollowsPay), Stored(copyright.AverageWithinCap), Stored(copyright.HoldsAttestation),
        Stored(copyright.CreativeShare));

    private static StoredPayments Stored(AdvancePaymentInputs payments) =>
        new(Stored(payments.First), Stored(payments.Second), Stored(payments.Third), Stored(payments.Fourth));

    private static AdvancePaymentInputs Input(StoredPayments stored) =>
        new(Input(stored.First), Input(stored.Second), Input(stored.Third), Input(stored.Fourth));
}

// The stored document and plan, version 10. Renaming or removing a member changes the format.
internal sealed record StoredBook(int Version, string? Today, string? Opened, IReadOnlyList<StoredPlan> Plans);

internal sealed record StoredPlan(string Id, string Name, JsonElement Inputs);

internal sealed record StoredInputs(
    int IncomeYear,
    StoredCompany? Company,
    StoredComposition Composition,
    StoredVouchers Vouchers,
    StoredAllowances Allowances,
    StoredRent Rent,
    StoredEquipment Equipment,
    StoredCar Car,
    StoredCopyright Copyright,
    StoredValue<bool> SmallCompany,
    StoredValue<FinancialYear> FinancialYear,
    StoredValue<CompanyContributionBand> CompanyContributionBand,
    StoredPayments AdvancePayments,
    StoredValue<decimal> DirectorPay,
    StoredMunicipality Municipality,
    StoredTaxPrepayments TaxPrepayments,
    StoredValue<decimal> Reserve,
    StoredValue<WithholdingCategory> DividendCategory,
    StoredValue<decimal> EarlierProfits,
    StoredValue<ContributionsPaidBy> ContributionsPaidBy,
    StoredValue<decimal?> BilledContributions,
    StoredValue<decimal?> FundFeeRate,
    StoredHousehold Household);

internal sealed record StoredHousehold(
    StoredValue<decimal> PartnerIncome,
    StoredValue<bool> BothAged66,
    StoredValue<bool> TaxedSeparately,
    StoredValue<HouseholdStatus> Status,
    StoredDependants Dependants);

internal sealed record StoredDependants(
    StoredValue<int> Children,
    StoredValue<int> ChildrenUnder3,
    StoredValue<bool> SingleParent,
    StoredValue<int> DependantsAged66InCare,
    StoredValue<int> OtherDependants);

internal sealed record StoredValue<T>(T Value, InputOrigin Origin);

internal sealed record StoredCompany(StoredValue<decimal> Revenue, StoredValue<decimal> RunningCosts);

internal sealed record StoredComposition(bool Vouchers, bool Allowances, bool Rent, bool Equipment, bool Car, bool Copyright, bool Household);

internal sealed record StoredCopyright(
    StoredValue<decimal> Royalty,
    StoredValue<bool> FollowsPay,
    StoredValue<bool> AverageWithinCap,
    StoredValue<bool> HoldsAttestation,
    StoredValue<decimal?> CreativeShare);

internal sealed record StoredVouchers(StoredValue<int> MealVoucherDays, StoredValue<decimal> EcoVouchers, StoredValue<decimal> SportCultureVouchers);

internal sealed record StoredAllowances(StoredValue<decimal> Office, StoredValue<decimal> Internet, StoredValue<decimal> Computer, StoredValue<decimal> Phone);

internal sealed record StoredRent(StoredValue<decimal> CadastralIncome, StoredValue<decimal> ShareLet, StoredValue<decimal> Rent);

internal sealed record StoredEquipment(
    StoredValue<int> Pcs,
    StoredValue<int> Tablets,
    StoredValue<int> MobilePhones,
    StoredValue<bool> Internet,
    StoredValue<int> PhoneSubscriptions,
    StoredValue<decimal> YearlyCosts,
    StoredValue<decimal> Contribution,
    StoredValue<decimal?> PrivateCost,
    StoredValue<DateOnly?> ProvidedFrom,
    StoredValue<DateOnly?> ReturnedOn);

internal sealed record StoredCar(
    StoredValue<decimal> CatalogueValue,
    StoredValue<DateOnly> FirstRegistration,
    StoredValue<decimal> YearlyCosts,
    StoredValue<bool> CompanyPaysPrivateEnergy,
    StoredValue<decimal> PersonalContribution,
    StoredValue<CarEngine> Engine,
    StoredValue<int> Co2,
    StoredValue<bool> NoCo2Data,
    StoredValue<DateOnly> Acquired,
    StoredValue<bool> FakeHybrid,
    StoredValue<int> MatchingCarCo2,
    StoredValue<bool> NoMatchingCar,
    StoredValue<bool> SmallNaturalGas,
    StoredValue<decimal?> PrivateCost,
    StoredValue<DateOnly?> HeldFrom,
    StoredValue<DateOnly?> ReturnedOn);

internal sealed record StoredPayments(StoredValue<decimal> First, StoredValue<decimal> Second, StoredValue<decimal> Third, StoredValue<decimal> Fourth);

// The municipality by its INS code, or none, with the rate on screen and where it comes from.
internal sealed record StoredMunicipality(int? InsCode, decimal Rate, InputOrigin Origin);

internal sealed record StoredTaxPrepayments(StoredValue<decimal?> Withheld, StoredPayments Paid, StoredValue<bool> Starter);

// Strict, so a plan is read as it was saved or not at all: every field required, none unknown, no
// null where the format has none, options by name so reordering an enum changes nothing. Generated
// at build time, so trimming the WebAssembly app cannot strip it.
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true,
    RespectRequiredConstructorParameters = true,
    RespectNullableAnnotations = true,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow)]
[JsonSerializable(typeof(StoredBook))]
[JsonSerializable(typeof(StoredInputs))]
internal sealed partial class PlanJson : JsonSerializerContext;
