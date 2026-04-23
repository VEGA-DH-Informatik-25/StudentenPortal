using CampusConnect.Application.Common.Interfaces;

namespace CampusConnect.Infrastructure.ExternalServices;

public class MockMensaService : IMensaService
{
    public Task<IReadOnlyList<MensaDay>> GetWeekMenuAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var monday = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);

        var menu = new List<MensaDay>
        {
            new(monday, [
                new("Spaghetti Bolognese", "Pasta", 3.50m, "Gluten, Sellerie", false, false),
                new("Gemüse-Curry mit Reis", "Vegetarisch", 2.90m, "Keine", true, true),
                new("Schnitzel mit Pommes", "Fleisch", 4.20m, "Gluten", false, false),
            ]),
            new(monday.AddDays(1), [
                new("Hähnchen-Wrap", "Geflügel", 3.80m, "Gluten, Sesam", false, false),
                new("Linsensuppe", "Vegetarisch", 2.50m, "Sellerie", true, true),
                new("Lachs mit Kartoffeln", "Fisch", 4.80m, "Fisch", false, false),
            ]),
            new(monday.AddDays(2), [
                new("Rindergulasch mit Nudeln", "Fleisch", 4.50m, "Gluten", false, false),
                new("Käse-Spinat-Quiche", "Vegetarisch", 3.20m, "Gluten, Milch, Ei", true, false),
                new("Tomatensuppe mit Brot", "Vegan", 2.30m, "Gluten", true, true),
            ]),
            new(monday.AddDays(3), [
                new("Pizza Margherita", "Vegetarisch", 3.10m, "Gluten, Milch", true, false),
                new("Döner-Teller", "Fleisch", 4.00m, "Gluten, Milch, Sesam", false, false),
                new("Falafel mit Hummus", "Vegan", 3.50m, "Sesam", true, true),
            ]),
            new(monday.AddDays(4), [
                new("Currywurst mit Pommes", "Fleisch", 3.60m, "Gluten, Senf", false, false),
                new("Gemüse-Pfanne mit Couscous", "Vegan", 2.90m, "Gluten", true, true),
                new("Forelle mit Gemüse", "Fisch", 5.20m, "Fisch", false, false),
            ]),
        };

        return Task.FromResult<IReadOnlyList<MensaDay>>(menu);
    }
}
