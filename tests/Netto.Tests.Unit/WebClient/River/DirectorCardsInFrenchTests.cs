using System.Text.RegularExpressions;
using Netto.Web.Client.DirectorPay;
using Netto.Web.Client.River;
using Netto.Web.Client.River.CompanyCar;
using Netto.Web.Client.River.Copyright;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// What the director's French files hold that ResxTranslationTests cannot see: a value the code fills in is
// never put behind a word it cannot follow (the French glossary). A municipality's name follows no « de » or
// « à » (« de Ath », « à Le Roeulx »), a month no « de » (« de avril »), and a rate the copyright card may
// fill with the words « le taux de l'année » no preposition at all (« à le taux de l'année »).
public class DirectorCardsInFrenchTests
{
    private const string Municipality = @"\b(?:de|du|à|au)\s+|d'";

    private const string Month = @"\b(?:de|du)\s+|d'";

    private const string RateOrItsWords = @"\b(?:de|du|des|à|au|aux|par|pour|sur|en|avec|sans|dans)\s+|d'";

    public static TheoryData<Type, string, int, string> FilledByCode => new()
    {
        { typeof(DirectorPayCard), "Situation_Rate_Official", 0, Municipality },
        { typeof(DirectorPayCard), "Situation_Rate_Overridden", 1, Municipality },
        { typeof(MunicipalityField), "RateNote_official", 0, Municipality },
        { typeof(MunicipalityField), "RateNote_official_earlier", 0, Municipality },
        { typeof(MunicipalityField), "RateNote_overridden", 0, Municipality },
        { typeof(PaySlip), "WithholdingMonths", 0, Month },
        { typeof(PaySlip), "WithholdingMonths", 1, Month },
        { typeof(PaySlip), "WithholdingMonth", 0, Month },
        { typeof(CarCard), "Figure_Period", 0, Month },
        { typeof(CarCard), "Figure_Period", 1, Month },
        { typeof(CarCard), "Figure_Period_Month", 0, Month },
        { typeof(CarCard), "Car_Registered", 0, Month },
        { typeof(CarCard), "Car_Acquired", 0, Month },
        { typeof(CopyrightCard), "Hint", 0, RateOrItsWords },
        { typeof(CopyrightCard), "Hint", 1, RateOrItsWords },
        { typeof(CopyrightCard), "PolicyHint", 0, RateOrItsWords },
        { typeof(CopyrightCard), "FixedRoyaltyHint", 0, RateOrItsWords },
        { typeof(CopyrightCard), "CreativeShareHint", 0, RateOrItsWords },
        { typeof(CopyrightCard), "Readings", 1, RateOrItsWords },
        { typeof(CopyrightCard), "Readings", 2, RateOrItsWords },
    };

    [Theory]
    [MemberData(nameof(FilledByCode))]
    public void FrenchText_aValueTheCodeFillsIn_followsNoWordItCannotFollow(Type component, string key, int placeholder, string before)
    {
        var text = ResxText.French(component).Get(key).ShouldNotBeNull();

        text.ShouldContain($"{{{placeholder}}}");
        Regex.IsMatch(text, $@"(?:{before})\{{{placeholder}\}}", RegexOptions.IgnoreCase).ShouldBeFalse(text);
    }

    // The copyright card fills a rate it lacks with these words, which the sentences above are written around.
    [Fact]
    public void CopyrightCard_yearsRateInFrench_isTheWordsTheSentencesTake()
    {
        ResxText.French<CopyrightCard>().Get("YearsRate").ShouldBe("le taux de l'année");
    }
}
