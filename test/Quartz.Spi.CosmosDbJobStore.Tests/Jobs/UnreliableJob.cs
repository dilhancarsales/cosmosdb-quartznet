using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Quartz.Spi.CosmosDbJobStore.Tests.Jobs
{
    [DisallowConcurrentExecution]
    public class UnreliableJob : IJob
    {
        private static readonly ILogger<UnreliableJob> _logger = NullLogger<UnreliableJob>.Instance;
        
        public static int TimesRun;

        public static bool Finished;
        
        
        public async Task Execute(IJobExecutionContext context)
        {
            if (++TimesRun <= 1)
            {               
                var t = context.Trigger.GetTriggerBuilder()
                    .StartAt(DateTimeOffset.Now.AddSeconds(5))
                    .Build();

                await context.Scheduler.RescheduleJob(t.Key, t);
                
                _logger.LogInformation("I have died, but I will be resurrected in 5 seconds :-P");
                
                // throw new JobExecutionException(false); According to Quartz.NET best practices, we should handle retry ourselves 
            }
            else
            {
                Finished = true;                
            }
            
        }
    }
}