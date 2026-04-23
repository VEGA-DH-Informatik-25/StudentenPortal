using CampusConnect.Application.Common.Interfaces;
using CampusConnect.Application.Features.Auth;
using CampusConnect.Application.Features.Calendar;
using CampusConnect.Application.Features.Feed;
using CampusConnect.Application.Features.Grades;
using CampusConnect.Domain.Interfaces;
using CampusConnect.Infrastructure.ExternalServices;
using CampusConnect.Infrastructure.Repositories;
using CampusConnect.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CampusConnect.Infrastructure;

public static class ServiceExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IUserRepository, InMemoryUserRepository>();
        services.AddSingleton<IFeedRepository, InMemoryFeedRepository>();
        services.AddSingleton<IGradeRepository, InMemoryGradeRepository>();
        services.AddSingleton<IExamRepository, InMemoryExamRepository>();

        services.AddSingleton<IJwtService, JwtService>();
        services.AddSingleton<IMensaService, MockMensaService>();

        services.AddScoped<AuthService>();
        services.AddScoped<FeedService>();
        services.AddScoped<GradesService>();
        services.AddScoped<CalendarService>();

        return services;
    }
}
