using CampusConnect.Application.Common.Interfaces;
using CampusConnect.Application.Features.Auth;
using CampusConnect.Application.Features.Calendar;
using CampusConnect.Application.Features.Feed;
using CampusConnect.Application.Features.Groups;
using CampusConnect.Application.Features.Grades;
using CampusConnect.Application.Features.Admin;
using CampusConnect.Domain.Interfaces;
using CampusConnect.Infrastructure.ExternalServices;
using CampusConnect.Infrastructure.Persistence;
using CampusConnect.Infrastructure.Repositories;
using CampusConnect.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CampusConnect.Infrastructure;

public static class ServiceExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<CampusConnectDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("CampusConnect") ?? "Data Source=campusconnect.db"));

        services.Configure<AdminOptions>(configuration.GetSection(AdminOptions.SectionName));

        services.AddScoped<IUserRepository, EntityUserRepository>();
        services.AddSingleton<IFeedRepository, InMemoryFeedRepository>();
        services.AddSingleton<IGroupRepository, InMemoryGroupRepository>();
        services.AddSingleton<IGradeRepository, InMemoryGradeRepository>();
        services.AddSingleton<IExamRepository, InMemoryExamRepository>();

        services.AddSingleton<IJwtService, JwtService>();
        services.Configure<MensaOptions>(configuration.GetSection(MensaOptions.SectionName));
        services.AddHttpClient<IMensaService, MensaApiClient>();
        services.AddHttpClient<ITimetableService, DhbwTimetableService>();

        services.AddScoped<AuthService>();
        services.AddScoped<FeedService>();
        services.AddScoped<GroupsService>();
        services.AddScoped<GradesService>();
        services.AddScoped<CalendarService>();
        services.AddScoped<AdminUsersService>();
        services.AddScoped<DatabaseInitializer>();

        return services;
    }

    public static async Task InitializeInfrastructureAsync(this IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
        await initializer.InitializeAsync(cancellationToken);
    }
}
