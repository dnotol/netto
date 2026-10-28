namespace Netto.Core.PersonalTax;

// Every amount is rounded to the cent where the administration's calculation detail shows it
// rounded, which is what makes the results match Tax-Calc to the cent.
public static class PersonalTaxCalculator
{
    // The other income is the net income of another category taxed with the pay, the net rent of a
    // home office let to the company (art. 7 and 13), never negative. None unless given. It is
    // globalised with the pay's net professional income, as Tax-Calc T13 and T14 show: the scale, the
    // tax-free amount and so the municipal surcharge apply to their total. The income taxed apart, a
    // copyright royalty within its limits, is taxed at its own rate, never on the scale nor against the
    // tax-free amount (art. 171), and that tax joins the State tax before the autonomy factor, so the
    // split and the municipal surcharge are on everything (Tax-Calc T16 to T18); none unless given.
    // A household taxed jointly is taxed in two columns (Joint), the director alone otherwise, a year the
    // couple starts or ends included; alone unless given. The household's dependants raise the tax-free
    // amount by their supplements (art. 132 and 133), and what the tax cannot absorb of the children's
    // turns into the refundable credit for children (art. 134 §3).
    public static PersonalTaxResult Compute(
        PersonalTaxRules rules, decimal directorPay, decimal ownContributions, decimal municipalRate, decimal otherIncome = 0m, IncomeTaxedApart? taxedApart = null,
        Household? household = null) =>
        Compute(rules, directorPay, ownContributions, municipalRate, otherIncome, taxedApart, household, withQuotient: true);

    // The same household's tax as if the marital quotient attributed nothing, what tells the quotient's
    // worth; the director's tax alone when the household is not taxed jointly.
    internal static PersonalTaxResult WithoutQuotient(
        PersonalTaxRules rules, decimal directorPay, decimal ownContributions, decimal municipalRate, decimal otherIncome, IncomeTaxedApart? taxedApart,
        Household? household) =>
        Compute(rules, directorPay, ownContributions, municipalRate, otherIncome, taxedApart, household, withQuotient: false);

    private static PersonalTaxResult Compute(
        PersonalTaxRules rules, decimal directorPay, decimal ownContributions, decimal municipalRate, decimal otherIncome, IncomeTaxedApart? taxedApart,
        Household? household, bool withQuotient)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(otherIncome, 0m);
        ArgumentOutOfRangeException.ThrowIfLessThan(taxedApart?.Income ?? 0m, 0m);

        var flatRateCosts = FlatRateCosts(rules, directorPay - ownContributions);
        var taxableIncome = Math.Max(0m, directorPay - ownContributions - flatRateCosts);
        if (household is { TaxedJointly: true })
            return Joint(rules, flatRateCosts, taxableIncome, municipalRate, otherIncome, taxedApart, household, withQuotient);

        // Taxed alone, the director is one column with no quotient: a tax-free amount larger than the
        // income only frees what there is, a single person passing the unused part on to no one but the
        // credit for children, and the dependants' supplements climb the scale of the tax on it beyond the
        // single person's first bracket.
        var dependants = household?.Dependants ?? Dependants.None;
        var supplements = rules.Supplements.Of(dependants, taxedJointly: false);
        var incomeTaxedApart = taxedApart?.Income ?? 0m;
        var director = Column(
            rules,
            municipalRate,
            professionalIncome: taxableIncome,
            quotient: 0m,
            otherIncome: otherIncome,
            taxableIncome: taxableIncome + otherIncome,
            taxFreeAmount: rules.TaxFreeAmount.Value + supplements.Total,
            taxApart: TaxApart(taxedApart),
            childSupplements: supplements.ForChildren);

        // The surcharge is a share of the tax before any credit comes off it (circular AFER 39/2008,
        // n° 51), so the credit for low earned income comes last and leaves it as it is. The dividend,
        // which bore its withholding, is not declared (art. 313) and the exempt part is exempt, so the
        // pay is all the professional income and all of it is activity income (art. 289ter, §2, al. 3
        // changes nothing); the rent and an income taxed apart are not professional income, but they
        // join the total net income that §1 and §2, al. 4 limit the credit by (Tax-Calc T17).
        return new PersonalTaxResult(
            flatRateCosts,
            taxableIncome,
            director.BasicTax,
            director.TaxFreeAmountReduction,
            director.StateTax,
            director.ReducedStateTax,
            director.RegionalAdditionalTax,
            director.IncomeTax,
            director.MunicipalSurcharge,
            LowEarnedIncomeCredit(rules, taxableIncome, taxableIncome + otherIncome + incomeTaxedApart),
            otherIncome,
            incomeTaxedApart,
            director.TaxedApart)
        {
            Supplements = supplements,
            ChildTaxCredit = Math.Min(director.ChildTaxCredit, rules.Supplements.CreditCap(dependants)),
        };
    }

    // A joint assessment (art. 126), each partner in their own column. The marital quotient moves a share
    // of one partner's professional income to the other (art. 87 and 88); the director's rent joins their
    // column, never the quotient's base, being no professional income, as an income taxed apart is not.
    // Each partner has the tax-free amount, the dependants' supplements go to the one with the higher
    // taxable income, the director at a tie (art. 134 §4, 2°), and the tax-free amounts pass between them
    // (SharedTaxFreeAmounts), the tax on them climbing the scale of §2 beyond its first bracket. Each
    // column then follows a single person's lines, rounded on its own, the credit for children computed in
    // it within the cap both share (ShareCap), and the household's lines are their sums. §4, al. 3, which
    // gives the supplements to the lower income when that lowers the couple's State tax with the tax on
    // some movable income, is not modelled: none of that income is on the scale here, and no case was found
    // where it binds, a tax-free amount passing to the higher income in full or up to the other's income. The credit for low earned income is the director's alone, on their own income
    // before the quotient (art. 289ter §1, al. 4): what the partner earns is typed without its kind, an
    // employee's pay taking art. 289ter/1 instead, so theirs is not counted.
    // The State tax, the autonomy factor and the regional tax are each partner's, rounded in their column;
    // the municipal surcharge is taken once on the household's income tax, as Tax-Calc's detail shows it
    // (« exercice 2026 », checked 2026-10-28), so the columns' own surcharges can sum to a cent more or less.
    // Art. 87's clause that the quotient does not apply when it raises the household's State tax is not
    // modelled: its current wording is unread (art. 87 and 88 not filed yet), and without supplements for
    // dependants no case was found where it would bind, the scale of the tax on the tax-free amount never
    // being below the income scale; with them, a quotient that moves the supplements to the other partner
    // was not searched for such a case.
    private static PersonalTaxResult Joint(
        PersonalTaxRules rules, decimal flatRateCosts, decimal directorIncome, decimal municipalRate, decimal otherIncome, IncomeTaxedApart? taxedApart,
        Household household, bool withQuotient)
    {
        var partnerIncome = household.PartnerIncome;
        ArgumentOutOfRangeException.ThrowIfLessThan(partnerIncome, 0m);

        var toPartner = withQuotient ? MaritalQuotient(rules.MaritalQuotient, partnerIncome, directorIncome, household.BothAged66) : 0m;
        var toDirector = withQuotient ? MaritalQuotient(rules.MaritalQuotient, directorIncome, partnerIncome, household.BothAged66) : 0m;
        var directorTaxable = directorIncome - toPartner + toDirector + otherIncome;
        var partnerTaxable = partnerIncome + toPartner - toDirector;

        var dependants = household.Dependants;
        var supplements = rules.Supplements.Of(dependants, taxedJointly: true);
        var directorHigher = directorTaxable >= partnerTaxable;
        var (withSupplements, other) = directorHigher
            ? SharedTaxFreeAmounts(rules.TaxFreeAmount.Value, supplements, directorTaxable, partnerTaxable)
            : SharedTaxFreeAmounts(rules.TaxFreeAmount.Value, supplements, partnerTaxable, directorTaxable);
        var (directorTaxFree, partnerTaxFree) = directorHigher ? (withSupplements, other) : (other, withSupplements);

        var incomeTaxedApart = taxedApart?.Income ?? 0m;
        var director = Column(
            rules,
            municipalRate,
            professionalIncome: directorIncome,
            quotient: toDirector - toPartner,
            otherIncome: otherIncome,
            taxableIncome: directorTaxable,
            taxFreeAmount: directorTaxFree.Total,
            taxApart: TaxApart(taxedApart),
            childSupplements: directorTaxFree.ForChildren)
            with
            {
                LowEarnedIncomeCredit = LowEarnedIncomeCredit(rules, directorIncome, directorIncome + otherIncome + incomeTaxedApart),
                Supplements = directorHigher ? supplements : TaxFreeAmountSupplements.None,
            };
        var partner = Column(
            rules,
            municipalRate,
            professionalIncome: partnerIncome,
            quotient: toPartner - toDirector,
            otherIncome: 0m,
            taxableIncome: partnerTaxable,
            taxFreeAmount: partnerTaxFree.Total,
            taxApart: 0m,
            childSupplements: partnerTaxFree.ForChildren)
            with { Supplements = directorHigher ? TaxFreeAmountSupplements.None : supplements };
        var (directorCredit, partnerCredit) = ShareCap(director.ChildTaxCredit, partner.ChildTaxCredit, rules.Supplements.CreditCap(dependants));
        director = director with { ChildTaxCredit = directorCredit };
        partner = partner with { ChildTaxCredit = partnerCredit };

        return new PersonalTaxResult(
            flatRateCosts,
            directorIncome,
            director.BasicTax + partner.BasicTax,
            director.TaxFreeAmountReduction + partner.TaxFreeAmountReduction,
            director.StateTax + partner.StateTax,
            director.ReducedStateTax + partner.ReducedStateTax,
            director.RegionalAdditionalTax + partner.RegionalAdditionalTax,
            director.IncomeTax + partner.IncomeTax,
            Money.RoundToCent((director.IncomeTax + partner.IncomeTax) * municipalRate),
            director.LowEarnedIncomeCredit,
            otherIncome,
            incomeTaxedApart,
            director.TaxedApart)
        {
            Joint = new JointAssessment(director, partner),
            Supplements = supplements,
            SupplementsTo = directorHigher ? Taxpayer.Director : Taxpayer.Partner,
            ChildTaxCredit = directorCredit + partnerCredit,
        };
    }

    // The two partners' tax-free amounts, the one with the supplements' first (art. 134 §4): each has the
    // base amount; the other passes all they cannot use to the one with the supplements (4°); what that one
    // cannot use of theirs then passes to the other, up to what the other's income leaves beyond their own
    // (3°), counted as the base amount and the other supplements first (al. 2). Each with the part of it
    // that is the children's supplements, whose tax the credit for children reads.
    private static (TaxFree WithSupplements, TaxFree Other) SharedTaxFreeAmounts(
        decimal baseAmount, TaxFreeAmountSupplements supplements, decimal withIncome, decimal otherIncome)
    {
        var fromOther = Math.Max(0m, baseAmount - otherIncome);
        var withOwn = baseAmount + supplements.Others + fromOther;
        var otherTaxFree = baseAmount - fromOther;
        var toOther = Math.Min(Math.Max(0m, withOwn + supplements.ForChildren - withIncome), Math.Max(0m, otherIncome - otherTaxFree));
        var childrenPassed = Math.Max(0m, toOther - withOwn);

        return (
            new TaxFree(withOwn + supplements.ForChildren - toOther, supplements.ForChildren - childrenPassed),
            new TaxFree(otherTaxFree + toOther, childrenPassed));
    }

    // The cap of the credit for children above the two partners' credits, shared in proportion to them (art.
    // 134 §4, al. 2, 2°): the director's share rounded to the cent, the partner's the rest.
    internal static (decimal Director, decimal Partner) ShareCap(decimal director, decimal partner, decimal cap)
    {
        var total = director + partner;
        if (total <= cap)
            return (director, partner);

        var directorShare = Money.RoundToCent(cap * director / total);
        return (directorShare, cap - directorShare);
    }

    // What the partner would owe filing alone, the river's baseline in a joint year (GrossToNet): a single
    // taxpayer on their own net professional income, typed net so no flat-rate costs come off it, with
    // their own tax-free amount, no quotient and no dependants, which are the household's, and no credit
    // for low earned income, which the joint assessment counts for the director alone (decided 2026-10-29).
    internal static PersonalTaxResult PartnerAlone(PersonalTaxRules rules, decimal partnerIncome, decimal municipalRate)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(partnerIncome, 0m);

        var partner = Column(
            rules, municipalRate, professionalIncome: partnerIncome, quotient: 0m, otherIncome: 0m, taxableIncome: partnerIncome,
            taxFreeAmount: rules.TaxFreeAmount.Value, taxApart: 0m);

        return new PersonalTaxResult(
            0m, partnerIncome, partner.BasicTax, partner.TaxFreeAmountReduction, partner.StateTax, partner.ReducedStateTax, partner.RegionalAdditionalTax,
            partner.IncomeTax, partner.MunicipalSurcharge, 0m);
    }

    // What the marital quotient attributes to a partner out of the other's professional income: what
    // tops their own up to the rate of the two together, never more than the cap. Two readings of art. 88
    // hold the cap differently: the law's words cap the share attributed (« une quote-part … sans pouvoir
    // excéder »), and the administration reads it as including the partner's own professional income
    // (DOC 56 1243/001, p. 16 and 17: « le revenu professionnel propre inclus »). The administration's
    // is applied, so the share is at most the cap less their own; with no own income both agree. Tax-Calc
    // « exercice 2026 » applies it too (2026-10-28: 8,610 to a partner earning 4,850, the cap 13,460). The
    // rate's share is rounded to the cent as the assessment shows it.
    internal static decimal MaritalQuotient(MaritalQuotientRules rules, decimal own, decimal other, bool bothAged66)
    {
        var share = Money.RoundToCent(rules.Rate.Value * (own + other)) - own;
        var withinCap = rules.CapFor(bothAged66).Value - own;
        return Math.Max(0m, Math.Min(share, withinCap));
    }

    // The tax on an income taxed apart at its own rate, nothing without one.
    private static decimal TaxApart(IncomeTaxedApart? taxedApart) => taxedApart is null ? 0m : Money.RoundToCent(taxedApart.Income * taxedApart.Rate.Value);

    // One taxpayer's lines on their taxable income and tax-free amount, the tax on the tax-free amount on
    // art. 134 §2's scale, up to what their income uses; no credit for low earned income until one is
    // given, and the credit for children before its cap (ChildCredit), on the part of the tax-free amount
    // that is the children's supplements. The federal
    // government keeps the State tax, with the tax apart, less the autonomy factor, and Wallonia adds its
    // regional tax on that share. The two rates are set to cancel out, but each part is rounded on its
    // own, so their sum can be a few cents off the State tax.
    private static TaxpayerColumn Column(
        PersonalTaxRules rules, decimal municipalRate, decimal professionalIncome, decimal quotient, decimal otherIncome, decimal taxableIncome, decimal taxFreeAmount,
        decimal taxApart, decimal childSupplements = 0m)
    {
        var basicTax = Money.RoundToCent(TaxOnScale(rules.Scale, taxableIncome));
        var reduction = Money.RoundToCent(TaxOnScale(rules.TaxFreeAmountScale, Math.Min(taxFreeAmount, taxableIncome)));
        var stateTax = Math.Max(0m, basicTax - reduction);
        var reducedStateTax = Money.RoundToCent((stateTax + taxApart) * (1 - rules.AutonomyFactor.Value));
        var regionalAdditionalTax = Money.RoundToCent(reducedStateTax * rules.RegionalAdditionalRate.Value);
        var incomeTax = reducedStateTax + regionalAdditionalTax;

        return new TaxpayerColumn(
            professionalIncome, quotient, otherIncome, taxableIncome, taxFreeAmount, basicTax, reduction, stateTax, taxApart,
            reducedStateTax, regionalAdditionalTax, incomeTax, Money.RoundToCent(incomeTax * municipalRate), 0m)
        {
            ChildTaxCredit = ChildCredit(rules, taxableIncome, basicTax, taxFreeAmount - childSupplements, childSupplements),
        };
    }

    // The part of the tax on the tax-free amount the basic tax cannot absorb, insofar as it is the
    // children's supplements, before the cap (art. 134 §3, al. 1 and 4): the tax-free amount is the base
    // amount and the other supplements first, counted only up to the taxable income (2°), the children's on
    // top; what §2's tax on it all exceeds the basic tax by, at most the tax on the children's layer, each
    // tax on §2's scale rounded to the cent as the reduction is. The same tax on the tax-free amount up to
    // the income leaves no State tax wherever this is above nothing, §2's scale never being below art.
    // 130's, so the credit never meets a municipal surcharge in its column.
    private static decimal ChildCredit(PersonalTaxRules rules, decimal taxableIncome, decimal basicTax, decimal baseAndOthers, decimal childSupplements)
    {
        if (childSupplements <= 0m)
            return 0m;

        var counted = Math.Min(baseAndOthers, taxableIncome);
        var onAll = Money.RoundToCent(TaxOnScale(rules.TaxFreeAmountScale, counted + childSupplements));
        var unabsorbed = Math.Max(0m, onAll - basicTax);
        var onChildren = onAll - Money.RoundToCent(TaxOnScale(rules.TaxFreeAmountScale, counted));

        return Math.Min(unabsorbed, onChildren);
    }

    // A tax-free amount and the part of it that is the children's supplements.
    private readonly record struct TaxFree(decimal Total, decimal ForChildren);

    // Art. 289ter, §2 on the activity income: a straight line up from the floor to the full amount,
    // then down from the end of the full amount to nothing at the ceiling, rounded to the cent as the
    // administration's examples and Tax-Calc round it. The total net income, the activity income and
    // any other, limits it: past the ceiling it is too high for any credit (§1), and past the end of
    // the full amount the credit is at most what the same falling line gives on that total (§2, al. 4),
    // rounded alike (derived: no Tax-Calc run lands there yet).
    public static decimal LowEarnedIncomeCredit(PersonalTaxRules rules, decimal activityIncome, decimal totalNetIncome)
    {
        var credit = rules.LowEarnedIncomeCredit;
        var (full, floor, fullFrom, fullUpTo, ceiling) =
            (credit.FullAmount.Value, credit.Floor.Value, credit.FullFrom.Value, credit.FullUpTo.Value, credit.Ceiling.Value);

        if (totalNetIncome > ceiling)
            return 0m;

        var amount = activityIncome switch
        {
            _ when activityIncome <= floor || activityIncome >= ceiling => 0m,
            _ when activityIncome < fullFrom => full * (activityIncome - floor) / (fullFrom - floor),
            _ when activityIncome <= fullUpTo => full,
            _ => full * (ceiling - activityIncome) / (ceiling - fullUpTo),
        };

        return totalNetIncome > fullUpTo
            ? Math.Min(Money.RoundToCent(amount), Money.RoundToCent(full * (ceiling - totalNetIncome) / (ceiling - fullUpTo)))
            : Money.RoundToCent(amount);
    }

    // A percentage of the pay left after the director's own social contributions, rounded to the
    // cent, up to a yearly maximum.
    internal static decimal FlatRateCosts(PersonalTaxRules rules, decimal payAfterContributions) =>
        Math.Clamp(
            Money.RoundToCent(rules.FlatRateCostsRate.Value * payAfterContributions),
            0m,
            rules.FlatRateCostsMaximum.Value);

    private static decimal TaxOnScale(IReadOnlyList<TaxBracket> scale, decimal taxableIncome)
    {
        var tax = 0m;
        var lowerLimit = 0m;
        foreach (var bracket in scale)
        {
            var upperLimit = bracket.UpperLimit?.Value ?? decimal.MaxValue;
            if (taxableIncome <= lowerLimit)
                break;

            tax += bracket.Rate.Value * (Math.Min(taxableIncome, upperLimit) - lowerLimit);
            lowerLimit = upperLimit;
        }

        return tax;
    }
}
