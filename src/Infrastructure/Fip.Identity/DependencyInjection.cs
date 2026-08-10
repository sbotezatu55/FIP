using Microsoft.Extensions.DependencyInjection;

namespace Fip.Identity;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentity(this IServiceCollection services) => services;
}
