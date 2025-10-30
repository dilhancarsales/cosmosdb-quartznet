using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Quartz.Impl;

namespace Quartz.Spi.CosmosDbJobStore.Tests
{
    public abstract class BaseStoreTests
    {
        public const string Barrier = "BARRIER";
        public const string DateStamps = "DATE_STAMPS";
        public static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(125);

        protected static Task<IScheduler> CreateScheduler(string instanceName = "QUARTZ_TEST")
        {
            var properties = new NameValueCollection
            {
                ["quartz.serializer.type"] = "json",
                ["quartz.scheduler.instanceName"] = instanceName,
                ["quartz.scheduler.instanceId"] = $"{Environment.MachineName}-{Guid.NewGuid()}",
                ["quartz.jobStore.type"] = typeof(CosmosDbJobStore).AssemblyQualifiedName,
                ["quartz.jobStore.Endpoint"] = "https://localhost:8081/",
                ["quartz.jobStore.Key"] = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",
                ["quartz.jobStore.DatabaseId"] = "quartz-demo",
                ["quartz.jobStore.CollectionId"] = "Quartz",
                ["quartz.jobStore.clustered"] = "true"
            };

            var scheduler = new StdSchedulerFactory(properties);
            return scheduler.GetScheduler();
        }
    }
}