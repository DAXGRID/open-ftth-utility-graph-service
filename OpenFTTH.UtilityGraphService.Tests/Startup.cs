using DAX.EventProcessing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenFTTH.CQRS;
using OpenFTTH.EventSourcing;
using OpenFTTH.EventSourcing.InMem;
using System;
using System.Reflection;

namespace OpenFTTH.UtilityGraphService.Tests
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            // Event producer
            services.AddSingleton<IExternalEventProducer, FakeExternalEventProducer>();

            // ES and CQRS stuff
            services.AddSingleton<IEventStore, InMemEventStore>();

            services.AddSingleton<IQueryDispatcher, QueryDispatcher>();
            services.AddSingleton<ICommandDispatcher, CommandDispatcher>();

            var businessAssemblies = new Assembly[] { AppDomain.CurrentDomain.Load("OpenFTTH.UtilityGraphService.Business") };

            services.AddCQRS(businessAssemblies);

            services.AddProjections(businessAssemblies);
        }
    }
}
