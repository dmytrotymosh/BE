using API.Services.Interfaces;
using API.Services.Realisations;

namespace API.Services;

public class DependencyRegistration
{
    public static IServiceCollection RegisterDependency(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IBookService, BookService>();

        return services;
    }
}
