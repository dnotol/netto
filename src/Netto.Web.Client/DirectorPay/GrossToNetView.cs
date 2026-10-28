using Netto.Core.DirectorPay;
using Netto.Core.PersonalTax;
using Netto.Core.Simulation;
using Netto.Web.Client.Steps;

namespace Netto.Web.Client.DirectorPay;

// What the pay slip draws from one gross-to-net result: each track as rows of bars on one scale
// shared by every track, so equal amounts are equally long, the next 1,000 as shares, the monthly
// statement, the key formula's withholding as a step with its lines and sources for each period of
// the year's withholding rules, with the months it covers, the balance on the assessment, and each
// step's amounts grouped under their sources. It only selects, orders and scales the engine's
// figures; every amount is the engine's. When the company pays the contributions
// on the director's behalf (CompanyPaysContributions) they are pay that goes straight back out, so
// both tracks start from what the pay costs the company, the gross plus them, which is also the pay
// the director declares, and the contributions stay paid out. The fund's fee on the contributions is a
// row of its own after them on both tracks, deducted with them, and a part of the next 1,000 while
// there is one. With a rent taxed with the pay
// (TaxedWithOtherIncome) the tracks stay the pay's own, while the next 1,000 and the balance are the
// year's, the rent's tax in them. A benefit in kind, a company car's, joins the tax base track after
// the gross, never the cash track: it is taxed as pay but never paid. A copyright royalty's part beyond
// its limits joins both after the gross: taxed as pay and paid, though besides the payslip, so each
// row's share is out of the gross and that part. With a royalty within its limits taxed apart
// (TaxedApart), the balance says how much of the final tax is its and what the company withheld on it.
internal sealed record GrossToNetView(
    bool CompanyPaysContributions,
    bool TaxedWithOtherIncome,
    IReadOnlyList<FunnelTrack> Tracks,
    IReadOnlyList<NextThousandPart> NextThousand,
    StatementView Statement,
    IReadOnlyList<WithholdingPeriodView> Withholding,
    AssessmentView Balance,
    IReadOnlyList<StepDetail> Details)
{
    private const decimal NextSlice = 1_000m;

    // The next 1,000's bar: the parts with a width, since one of none would still draw the gap that
    // separates segments. The legend lists every part.
    public IReadOnlyList<NextThousandPart> NextThousandBar => [.. NextThousand.Where(part => part.Width > 0m)];

    public bool TaxedApart => Balance.OnTaxedApart != 0m || Balance.WithheldBesidesPay != 0m;

    // A royalty's part taxed as pay, whose 30% limit moves with the pay: the next 1,000 holds it as it is.
    public bool WithRequalifiedPay { get; init; }

    // The fund's fee on the contributions, whose part the next 1,000 draws.
    public bool WithFundFee { get; init; }

    public static GrossToNetView From(GrossToNetResult result)
    {
        // The company's contributions are never nothing when it pays them: the minimum applies.
        var companyPays = result.ContributionsPaidByCompany > 0m;
        var gross = companyPays
            ? new Line("company_cost", RowKind.Gross, result.CompanyCost)
            : new Line("gross_pay", RowKind.Gross, result.GrossPay);

        // A royalty's part taxed as pay, added to the gross on both tracks.
        IReadOnlyList<Line> requalified = result.RequalifiedPay > 0m ? [new Line("requalified_pay", RowKind.Added, result.RequalifiedPay)] : [];

        // What leaves the pocket, in the engine's order, down to the net.
        var cash = new List<Line> { gross };
        cash.AddRange(requalified);
        cash.AddRange(result.Steps.Where(step => step.Track == Track.PaidOut).Select(Deduction));
        cash.Add(new Line("net_in_pocket", RowKind.NetInPocket, result.NetInPocket));

        // What lowers the tax base, in the engine's order, down to the taxable income it closes on, a
        // benefit in kind and a royalty's part taxed as pay added to the gross first.
        var taxBase = new List<Line> { gross };
        if (result.BenefitInKind > 0m)
            taxBase.Add(new Line("benefit_in_kind", RowKind.Added, result.BenefitInKind));
        taxBase.AddRange(requalified);
        taxBase.AddRange(result.Steps.TakeWhile(step => step.RuleKey != "taxable_income").Select(Deduction));
        taxBase.Add(new Line("taxable_income", RowKind.TaxableIncome, result.TaxableIncome));

        var tracks = new[] { (Key: "cash", Bars: Bars(cash)), (Key: "tax_base", Bars: Bars(taxBase)) };

        // From the lowest point, or zero, to the highest: below zero only when the net is negative.
        var ends = tracks.SelectMany(track => track.Bars).SelectMany(bar => new[] { bar.From, bar.To }).Append(0m).ToList();
        var (minimum, maximum) = (ends.Min(), ends.Max());
        decimal Percent(decimal amount) => maximum == minimum ? 0m : (amount - minimum) / (maximum - minimum) * 100m;

        return new GrossToNetView(
            companyPays,
            result.OtherIncomeTax.Income > 0m,
            tracks.Select(track => new FunnelTrack(
                track.Key,
                track.Bars.Select(bar => new FunnelRow(bar.Line.Key, bar.Line.Kind, bar.Line.Amount, Percent(bar.From), Percent(bar.To))
                {
                    Share = Shares.Of(bar.Line.Amount, gross.Amount + result.RequalifiedPay),
                }).ToList()))
                .ToList(),
            Parts(
            [
                ("social_contributions", result.NextThousand.SocialContributions),
                .. result.FundFee > 0m ? [("fund_fee", result.NextThousand.FundFee)] : Array.Empty<(string, decimal)>(),
                ("income_tax", result.NextThousand.IncomeTax),
                ("municipal_surcharge", result.NextThousand.MunicipalSurcharge),
                ("low_earned_income_credit_lost", result.NextThousand.LowEarnedIncomeCredit),
                ("income_tax_advance_payments", result.NextThousand.AdvancePayments),
                ("net_in_pocket", result.NextThousand.NetInPocket),
            ]),
            StatementView.From(result.Statement, companyPays),
            [.. result.Statement.Periods.Select(period => new WithholdingPeriodView(period.FirstMonth, period.LastMonth, WithholdingOf(period)))],
            AssessmentView.From(result.Assessment, result.OtherIncomeTax, result.TaxedApartTax),
            result.Steps.Select(StepDetail.From).ToList()) { WithRequalifiedPay = result.RequalifiedPay > 0m, WithFundFee = result.FundFee > 0m };
    }

    // A month's withholding in a period as a step: the formula's lines in its order, the key formula
    // as the rule, and the annex's amounts under the number each comes from. Under the marital quotient
    // the basic tax is the income imputed to the partner, the tax on it and the tax on the rest, and
    // what is taken off them both partners' tax-free amounts.
    private static StepDetail WithholdingOf(PayStatementPeriod period)
    {
        var formula = period.Withholding;
        IReadOnlyList<StepLine> basicTax = formula.Household switch
        {
            WithholdingHousehold.AsSingle =>
            [
                new StepLine("basic_tax", formula.BasicTax),
                new StepLine("tax_free_amount_reduction", formula.TaxFreeAmountReduction),
            ],
            WithholdingHousehold.PartnerWithoutIncome or WithholdingHousehold.PartnerWithoutIncomeBothAged66 =>
            [
                new StepLine("imputed_to_partner", formula.ImputedToPartner),
                new StepLine("partner_tax", formula.PartnerTax),
                new StepLine("rest_tax", formula.BasicTax),
                new StepLine("joint_tax_free_amount_reduction", formula.TaxFreeAmountReduction),
            ],
            _ => throw new ArgumentOutOfRangeException(nameof(period), formula.Household, "No withholding lines for this household."),
        };

        return StepDetail.From("withholding", formula.MonthlyWithholding, period.Source, period.AmountsUsed) with
        {
            Lines =
            [
                new StepLine("monthly_taxable_pay", formula.MonthlyTaxablePay),
                new StepLine("reduction", formula.Reduction),
                new StepLine("annual_gross", formula.AnnualGross),
                new StepLine("flat_rate_costs", formula.FlatRateCosts),
                new StepLine("annual_net_taxable", formula.AnnualNetTaxable),
                .. basicTax,
                new StepLine("annual_tax", formula.AnnualTax),
                new StepLine("monthly_withholding", formula.MonthlyWithholding),
            ],
        };
    }

    // A step paid out with a negative amount is money that comes back: the tax credit for low earned
    // income. The engine negates it, so a credit of nothing is a negative zero and still comes back.
    private static Line Deduction(Step step) => step.Track switch
    {
        Track.PaidOut when decimal.IsNegative(step.Amount) => new(step.RuleKey, RowKind.Back, -step.Amount),
        Track.PaidOut => new(step.RuleKey, RowKind.PaidOut, step.Amount),
        _ => new(step.RuleKey, RowKind.TaxBaseOnly, step.Amount),
    };

    // Where the credit rises, the next 1,000 gains some of it: that part is negative and the pocket's is
    // more than the 1,000. The bar then draws only the parts that take money, each as wide as its share
    // of them; where none is negative, that is its share of the 1,000.
    private static List<NextThousandPart> Parts(IReadOnlyList<(string Key, decimal Amount)> parts)
    {
        var drawn = parts.Sum(part => Math.Max(0m, part.Amount));
        return parts
            .Select(part => new NextThousandPart(part.Key, part.Amount, part.Amount / NextSlice, drawn == 0m ? 0m : Math.Max(0m, part.Amount) / drawn))
            .ToList();
    }

    // A total is drawn between zero and its amount, a deduction from the balance it leaves to the
    // balance before it, so each part paid out sits where the previous bar ended, and what comes back
    // from the balance it finds up to the balance it leaves.
    private static List<(Line Line, decimal From, decimal To)> Bars(IReadOnlyList<Line> lines)
    {
        var bars = new List<(Line, decimal, decimal)>();
        var balance = 0m;
        foreach (var line in lines)
        {
            if (line.IsDeduction)
            {
                bars.Add((line, balance - line.Amount, balance));
                balance -= line.Amount;
            }
            else if (line.Kind is RowKind.Back or RowKind.Added)
            {
                bars.Add((line, balance, balance + line.Amount));
                balance += line.Amount;
            }
            else
            {
                bars.Add((line, Math.Min(0m, line.Amount), Math.Max(0m, line.Amount)));
                balance = line.Amount;
            }
        }

        return bars;
    }

    private sealed record Line(string Key, RowKind Kind, decimal Amount)
    {
        public bool IsDeduction => Kind.IsDeduction();
    }
}

internal sealed record FunnelTrack(string Key, IReadOnlyList<FunnelRow> Rows);

// One bar of a track. BarStart and BarEnd are percentages of the shared scale's width; Share is the
// row's share of the gross the track starts from, none when there is no pay.
internal sealed record FunnelRow(string Key, RowKind Kind, decimal Amount, decimal BarStart, decimal BarEnd)
{
    public decimal? Share { get; init; }

    public bool IsDeduction => Kind.IsDeduction();
}

internal enum RowKind
{
    Gross,

    // Leaves the pocket: social contributions, income tax, municipal surcharge.
    PaidOut,

    // Comes back to the pocket after the tax: the tax credit for low earned income.
    Back,

    // Joins the tax base without ever reaching the pocket: a benefit in kind.
    Added,

    // Only lowers the tax base and is never paid out: the flat-rate costs.
    TaxBaseOnly,

    TaxableIncome,
    NetInPocket,
}

internal static class RowKindExtensions
{
    public static bool IsDeduction(this RowKind kind) => kind is RowKind.PaidOut or RowKind.TaxBaseOnly;
}

// Share is a fraction of the next 1,000: 0.16588 for 165.88, negative for credit gained. Width is the
// fraction of the bar it takes, the same as its share unless a part is negative.
internal sealed record NextThousandPart(string Key, decimal Amount, decimal Share, decimal Width);

// The monthly statement as the slip draws it: the engine's twelve months and whether the column of
// contributions the company pays is shown. The column of a benefit in kind shows once the pay holds
// one (HasBenefit).
internal sealed record StatementView(
    IReadOnlyList<PayStatementMonth> Months,
    bool CompanyPaysContributions)
{
    public bool HasBenefit => Months.Any(month => month.BenefitInKind > 0m);

    public static StatementView From(PayStatement statement, bool companyPays) => new(statement.Months, companyPays);
}

// The withholding of the months FirstMonth to LastMonth (1 is January), withheld on by one period of
// the year's rules, as a step; Details is that step alone, as the slip's list of steps takes it.
internal sealed record WithholdingPeriodView(int FirstMonth, int LastMonth, StepDetail Detail)
{
    public IReadOnlyList<StepDetail> Details => [Detail];
}

// The balance on the assessment as the slip draws it: the engine's lines, how much of the final tax is
// on the income taxed with the pay (OnOtherIncome, the rent's) and on an income taxed apart
// (OnTaxedApart, a copyright royalty's), what the company withheld at source besides the pay
// (WithheldBesidesPay, on the royalty), which way the balance goes, and the balance as a step citing
// the article it follows, with no amount of the year.
internal sealed record AssessmentView(
    decimal FinalTax,
    decimal OnOtherIncome,
    decimal Withheld,
    bool WithheldGiven,
    decimal AdvancePayments,
    decimal Balance,
    BalanceKind Kind,
    StepDetail Detail)
{
    public decimal OnTaxedApart { get; init; }

    public decimal WithheldBesidesPay { get; init; }

    // The balance as the slip shows it, without its sign: the kind says which way it goes.
    public decimal Amount => Math.Abs(Balance);

    public static AssessmentView From(Assessment assessment, OtherIncomeTax otherIncomeTax, OtherIncomeTax taxedApartTax) => new(
        assessment.FinalTax,
        otherIncomeTax.Total,
        assessment.Withheld,
        assessment.WithheldGiven,
        assessment.AdvancePayments,
        assessment.Balance,
        assessment.Balance switch
        {
            > 0m => BalanceKind.ToPay,
            < 0m => BalanceKind.Refunded,
            _ => BalanceKind.Settled,
        },
        StepDetail.From("assessment_balance", assessment.Balance, assessment.Source, []))
    {
        OnTaxedApart = taxedApartTax.Total,
        WithheldBesidesPay = assessment.WithheldBesidesPay,
    };
}

// Which way the balance on the assessment goes: the engine's balance is positive to pay, negative
// to be refunded.
internal enum BalanceKind
{
    ToPay,

    Refunded,

    Settled,
}
