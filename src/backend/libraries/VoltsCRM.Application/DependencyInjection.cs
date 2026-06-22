using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using VoltsCRM.Application.Common.Behaviors;

namespace VoltsCRM.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));

        // Register every FluentValidation validator in this assembly using the core AssemblyScanner
        // (avoids taking a dependency on FluentValidation.DependencyInjectionExtensions).
        foreach (var result in AssemblyScanner.FindValidatorsInAssembly(assembly))
            services.AddScoped(result.InterfaceType, result.ValidatorType);

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
