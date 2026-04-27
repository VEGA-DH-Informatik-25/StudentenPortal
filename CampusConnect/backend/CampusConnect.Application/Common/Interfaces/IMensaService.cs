namespace CampusConnect.Application.Common.Interfaces;

public record MensaDish(string Name, string Category, decimal PriceStudent, string? Allergens, bool IsVegetarian, bool IsVegan);
public record MensaDay(DateOnly Date, IReadOnlyList<MensaDish> Dishes);

public interface IMensaService
{
    Task<IReadOnlyList<MensaDay>> GetWeekMenuAsync(CancellationToken cancellationToken = default);
}
