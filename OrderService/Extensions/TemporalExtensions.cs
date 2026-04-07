using System.Reflection;
using OrderService.Attributes;
using Shared.Contracts;
using Temporalio.Client;
using Temporalio.Extensions.Hosting;
using Temporalio.Extensions.OpenTelemetry;
using Temporalio.Workflows;

namespace OrderService.Extensions;

public static class TemporalExtensions
{
    extension(IServiceCollection services)
    {
        public void RegisterTemporalWithWorkflowsAndWorkers(IConfiguration configuration)
        {
            services.AddTemporalClient(configuration);
            services.RegisterWorkflowsAndWorkers();
        }

        private void AddTemporalClient(IConfiguration configuration)
        {
            var temporalOptions = configuration.GetSection(nameof(TemporalOptions)).Get<TemporalOptions>() ??
                                  new TemporalOptions();
            services.AddTemporalClient(options =>
            {
                options.TargetHost = temporalOptions.Host;
                options.Namespace = temporalOptions.Namespace;

                if (temporalOptions.UseTls)
                    options.Tls = new TlsOptions();

                options.Interceptors = [new TracingInterceptor()];
            });
        }
        
        private void RegisterWorkflowsAndWorkers()
        {
            Assembly[] assemblies = [typeof(Program).Assembly];
            var allTemporalClassTypes = assemblies
                .SelectMany(a => a.GetTypes())
                .Where(t => t is { IsClass: true, IsAbstract: false } && t.GetCustomAttribute<TemporalTaskQueueAttribute>() != null)
                .ToArray();

            foreach (var type in allTemporalClassTypes)
            {
                var temporalTaskQueueAttribute = type.GetCustomAttribute<TemporalTaskQueueAttribute>()!;
                var workflowAttribute = type.GetCustomAttribute<WorkflowAttribute>();
                if (workflowAttribute != null)
                {
                    services.AddHostedTemporalWorker(temporalTaskQueueAttribute.Name)
                        .AddWorkflow(type);
                }
                else
                {
                    services.AddHostedTemporalWorker(temporalTaskQueueAttribute.Name)
                        .AddScopedActivities(type);
                }
            }
        }
    }
}