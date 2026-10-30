using Netto.Core.Rules;
using Shouldly;

namespace Netto.Tests.Rules;

// An article of CIR 92, or of its royal decree of execution (AR/CIR 92), as the administration coordinates
// it on Fisconetplus for one income year: its title, its address by the record's guid, the file
// official-docs/ keeps it under, named by the day the record was last modified, the article it is, and the
// few words a list cites it by.
public class SourceTests
{
    [Fact]
    public void Fisconet_article_citesTheCoordinatedTextOfItsIncomeYear()
    {
        var source = Source.Fisconet("personal-tax", "157", 2026, new DateOnly(2026, 7, 30), "c6e47940-53f0-4608-b567-ae774014a621", "The surcharge");

        (source.Title, source.Reference, source.Url, source.Document, source.Article).ShouldBe((
            "CIR 92, art. 157 (revenus 2026), coordinated text on Fisconetplus, last modified 30.07.2026",
            "The surcharge",
            new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/c6e47940-53f0-4608-b567-ae774014a621"),
            "personal-tax/spf-finances_cir92-art-157-revenus-2026_2026-07-30.html",
            new FisconetArticle(FisconetCode.Cir92, "157", 2026, new DateOnly(2026, 7, 30))));
    }

    [Fact]
    public void Fisconet_article_isCitedInAListByItsArticle()
    {
        Source.Fisconet("corporate-tax", "159", 2026, new DateOnly(2026, 2, 9), "ed182474-e88a-4931-91a3-51286d1dc47a", "The surcharge").Label.ShouldBe("CIR 92, art. 159");
    }

    // Art. 38/1 is filed as art-38-1: a file name holds no slash.
    [Fact]
    public void Fisconet_articleWithASlash_isFiledUnderItsNumberDashed()
    {
        var source = Source.Fisconet("personal-tax", "38/1", 2026, new DateOnly(2026, 1, 14), "6a028d41-f93b-43f0-85ce-ec749722675a", "Meal vouchers");

        (source.Document, source.Label).ShouldBe(("personal-tax/spf-finances_cir92-art-38-1-revenus-2026_2026-01-14.html", "CIR 92, art. 38/1"));
    }

    [Fact]
    public void Fisconet_articleOfTheRoyalDecree_citesTheArCir92()
    {
        var source = Source.Fisconet("personal-tax", "18", 2026, new DateOnly(2026, 8, 10), "f3ebfcea-9d61-4d4a-bd76-33f13ef93825", "Reference emission", FisconetCode.ArCir92);

        (source.Title, source.Document, source.Label, source.Article?.Code).ShouldBe((
            "AR/CIR 92, art. 18 (revenus 2026), coordinated text on Fisconetplus, last modified 10.08.2026",
            "personal-tax/spf-finances_arcir92-art-18-revenus-2026_2026-08-10.html",
            "AR/CIR 92, art. 18",
            FisconetCode.ArCir92));
    }
}
