using Netto.Core.Allowances;
using Netto.Core.Rules;

namespace Netto.Core.Equipment;

public static class EquipmentCalculator
{
    public static readonly Refusal PcsNegative = new("equipment_pcs_negative", RefusedInput.EquipmentPcs);
    public static readonly Refusal TabletsNegative = new("equipment_tablets_negative", RefusedInput.EquipmentTablets);
    public static readonly Refusal MobilePhonesNegative = new("equipment_mobile_phones_negative", RefusedInput.EquipmentMobilePhones);
    public static readonly Refusal PhoneSubscriptionsNegative = new("equipment_phone_subscriptions_negative", RefusedInput.EquipmentPhoneSubscriptions);
    public static readonly Refusal YearlyCostsNegative = new("equipment_yearly_costs_negative", RefusedInput.EquipmentYearlyCosts);
    public static readonly Refusal ContributionNegative = new("equipment_contribution_negative", RefusedInput.EquipmentContribution);
    public static readonly Refusal ContributionAboveBenefit = new("equipment_contribution_above_benefit", RefusedInput.EquipmentContribution);

    // Beside a PC or a tablet the company provides, it pays no flat computer allowance, and beside an internet
    // connection no flat internet allowance (circular 2021/C/20, §52 and §48).
    public static readonly Refusal PcWithComputerAllowance = new("pc_with_computer_allowance", RefusedInput.EquipmentPcs);
    public static readonly Refusal TabletWithComputerAllowance = new("tablet_with_computer_allowance", RefusedInput.EquipmentTablets);
    public static readonly Refusal InternetWithInternetAllowance = new("internet_with_internet_allowance", RefusedInput.EquipmentInternet);

    // A span in which the set is provided on no day of the year: from after its end, given back on or before
    // its first day, or on or before the day it is provided from.
    public static readonly Refusal ProvidedAfterYear = new("equipment_provided_after_year", RefusedInput.EquipmentProvidedFrom);
    public static readonly Refusal ReturnedBeforeYear = new("equipment_returned_before_year", RefusedInput.EquipmentReturnedOn);
    public static readonly Refusal ReturnedBeforeProvided = new("equipment_returned_before_provided", RefusedInput.EquipmentReturnedOn);

    // What the same things would cost privately cannot be below nothing. No rule reads it, so the engine never
    // refuses it: the card does, weighing the set against it.
    public static readonly Refusal PrivateCostNegative = new("equipment_private_cost_negative", RefusedInput.EquipmentPrivateCost);

    // The set as the rules take it, beside the allowances the company pays, or what they refuse. Each kind's
    // benefit is one piece's flat yearly value for the days provided, rounded to the cent, times how many: the
    // administration gives no worked example with cents, so rounding each piece is Netto's reading. Their sum
    // less the contribution, which may not exceed it, is taxed as pay. A span holding no day of the year is
    // refused on its date.
    public static (EquipmentResult? Value, Refusal? Error) Compute(EquipmentRules rules, EquipmentFacts set, ExpenseAllowances allowances)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(set);
        ArgumentNullException.ThrowIfNull(allowances);

        if (set.Pcs < 0)
            return (null, PcsNegative);
        if (set.Tablets < 0)
            return (null, TabletsNegative);
        if (set.MobilePhones < 0)
            return (null, MobilePhonesNegative);
        if (set.PhoneSubscriptions < 0)
            return (null, PhoneSubscriptionsNegative);
        if (set.YearlyCosts < 0m)
            return (null, YearlyCostsNegative);
        if (set.Contribution < 0m)
            return (null, ContributionNegative);
        if (!set.Any)
            return (EquipmentResult.None, null);
        if (set.Pcs > 0 && allowances.Computer > 0m)
            return (null, PcWithComputerAllowance);
        if (set.Tablets > 0 && allowances.Computer > 0m)
            return (null, TabletWithComputerAllowance);
        if (set.Internet && allowances.Internet > 0m)
            return (null, InternetWithInternetAllowance);

        var (days, fault) = DaysHeld.Of(rules.IncomeYear, set.ProvidedFrom, set.ReturnedOn);
        if (days is null)
        {
            return (null, fault switch
            {
                DaysHeldFault.StartsAfterYear => ProvidedAfterYear,
                DaysHeldFault.ReturnedBeforeYear => ReturnedBeforeYear,
                DaysHeldFault.ReturnedBeforeStart => ReturnedBeforeProvided,
                _ => throw new InvalidOperationException($"No refusal names the provided span's fault {fault}."),
            });
        }

        EquipmentPiece[] pieces =
        [
            Piece(EquipmentKind.Pc, set.Pcs, rules.PcBenefit, days),
            Piece(EquipmentKind.Tablet, set.Tablets, rules.TabletBenefit, days),
            Piece(EquipmentKind.MobilePhone, set.MobilePhones, rules.MobilePhoneBenefit, days),
            Piece(EquipmentKind.Internet, set.Internet ? 1 : 0, rules.InternetBenefit, days),
            Piece(EquipmentKind.PhoneSubscription, set.PhoneSubscriptions, rules.PhoneSubscriptionBenefit, days),
        ];
        var provided = pieces.Where(piece => piece.Count > 0).ToList();
        var beforeContribution = provided.Sum(piece => piece.Benefit);
        if (set.Contribution > beforeContribution)
            return (null, ContributionAboveBenefit);

        return (new EquipmentResult(provided, days, beforeContribution, set.Contribution, Money.Minus(beforeContribution, set.Contribution), set.YearlyCosts), null);
    }

    private static EquipmentPiece Piece(EquipmentKind kind, int count, RuleAmount value, DaysHeld days) =>
        new(kind, count, value, Money.RoundToCent(days.Prorate(value.Value)) * count);
}
