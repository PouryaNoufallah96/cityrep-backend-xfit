using Autofac;
using static XFit.Utilities.Constants.RegisterMode;
using System.Reflection;
using Xfit.Domain.Repositories.Contracts;
using XFit.Services._Client;

namespace XFit.Utilities.Configurations
{
    public static class ControllerAutofacConfigurationExtensions
    {
        public static void AddControllerServices(this ContainerBuilder containerBuilder)
        {
            var assembliesToRegister = new Assembly[]
            {
                typeof(IClientRepository).Assembly,
                typeof(IClientService).Assembly,
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
}
