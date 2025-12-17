using Autofac;
using Microsoft.Extensions.Hosting;
using System.Reflection;
using XFit.Utilities.Enums;
using XFit.Utilities.Services;
using static XFit.Utilities.Constants.RegisterMode;

namespace XFit.Utilities.Configuration;
public static class AutofacConfigurationExtensions
{
    public static void AddServices(this ContainerBuilder containerBuilder)
    {
        var assembliesToRegister = new Assembly[]
        {
                typeof(ApiResultStatusCode).Assembly,
                typeof(JwtService).Assembly,
        };

        containerBuilder.RegisterAssemblyTypes(assembliesToRegister)
            .AssignableTo<IScopedDependency>()
            .AsImplementedInterfaces()
            .InstancePerLifetimeScope();

        containerBuilder.RegisterAssemblyTypes(assembliesToRegister)
            .AssignableTo<ITransientDependency>()
            .AsImplementedInterfaces()
            .InstancePerDependency();

        containerBuilder.RegisterAssemblyTypes(assembliesToRegister)
            .AssignableTo<ISingletonDependency>()
            .AsImplementedInterfaces()
            .SingleInstance();

        containerBuilder.RegisterAssemblyTypes(assembliesToRegister)
           .AssignableTo<ISelfSingletonDependency>()
           .AsSelf()
           .SingleInstance();

        containerBuilder.RegisterAssemblyTypes(assembliesToRegister)
          .AssignableTo<IHostedDependency>()
          .As<IHostedService>()
          .SingleInstance();
    }
}