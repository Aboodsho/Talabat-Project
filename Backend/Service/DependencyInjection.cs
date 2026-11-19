using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Service.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Service
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddService(this IServiceCollection services)
        {
            var assembly = Assembly.GetExecutingAssembly(); // Get the assembly where the service classes are defined

            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly)); // any class that implement IRequestHandler<TRequest, TResponse>
            services.AddValidatorsFromAssembly(assembly); // any class that implement AbstractValidator<T>
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));   

            return services;
        }
    }
}
