using CampusConnect.Infrastructure.ExternalServices;

namespace CampusConnect.API.Tests;

public sealed class DhbwStudyPlanParserTests
{
    [Fact]
    public void IndexParser_ShouldResolveCampusPdfLinksFromDhbwTable()
    {
        const string html = """
            <table>
              <tr><td>Informatik</td><td>DHBW</td><td>RV/FN</td><td>LÖ</td></tr>
              <tr><td>Informatik</td><td><a href="DHBW/Informatik/Informatik.pdf">•</a></td><td></td><td><a href="LOE/Informatik/Informatik.pdf">•</a></td></tr>
            </table>
            """;

        var sources = DhbwStudyPlanIndexParser.Parse(html, new Uri("https://www.dhbw.de/fileadmin/user/public/SP/Studienbereich_Technik.htm"), "LÖ");

        var source = Assert.Single(sources);
        Assert.Equal("Informatik", source.StudyArea);
        Assert.Equal("Informatik", source.PlanName);
        Assert.Equal("https://www.dhbw.de/fileadmin/user/public/SP/LOE/Informatik/Informatik.pdf", source.Url);
    }

    [Fact]
    public void TextParser_ShouldReadCurriculumModulesAndExamRows()
    {
        var pages = new[]
        {
            new DhbwPdfPage("""
                FESTGELEGTER MODULBEREICH
                NUMMER MODULBEZEICHNUNG VERORTUNG ECTS
                1. StudienjahrT4INF1001 Mathematik I 5
                2. StudienjahrT4INF2003 Software Engineering I 9
                Curriculum // Seite 2Stand vom 29.04.2026
                """, []),
            new DhbwPdfPage("""
                VARIABLER MODULBEREICH
                NUMMER MODULBEZEICHNUNG VERORTUNG ECTS
                3. StudienjahrT4INF3903 Grundlagen Digitaler Transformation 5
                Curriculum // Seite 3Stand vom 29.04.2026
                """, []),
            new DhbwPdfPage("""
                LÖRRACH
                Mathematik I (T4INF1001)
                Mathematics I
                FORMALE ANGABEN ZUM MODUL
                """, [new DhbwExamRow("Klausur", "Siehe Pruefungsordnung", true)])
        };

        var plan = DhbwStudyPlanTextParser.Parse("Informatik", "https://example.invalid/Informatik.pdf", DateTime.UtcNow, pages);

        Assert.Equal(3, plan.Modules.Count);
        var math = plan.Modules.Single(module => module.Code == "T4INF1001");
        Assert.Equal("Mathematik I", math.Name);
        Assert.Equal(1, math.StudyYear);
        Assert.Equal(5, math.Ects);
        Assert.True(math.IsRequired);
        var exam = Assert.Single(math.Exams);
        Assert.Equal("Klausur", exam.Name);
        Assert.True(exam.IsGraded);

        var variable = plan.Modules.Single(module => module.Code == "T4INF3903");
        Assert.False(variable.IsRequired);
    }
}