using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace InvoiceBuilder.Users;

public static class UsersModule
{
    public static IServiceCollection AddUsersModule(this IServiceCollection services)
    {
        return services;
    }

    public static IEndpointRouteBuilder MapUsersModule(this IEndpointRouteBuilder app)
    {
        return app;
    }
}
