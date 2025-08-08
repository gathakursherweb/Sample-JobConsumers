namespace JobService.Components;

using MassTransit;


public class MaintenanceJobConsumerDefinition :
	ConsumerDefinition<MaintenanceJobConsumer>
{
	protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator,
		IConsumerConfigurator<MaintenanceJobConsumer> consumerConfigurator, IRegistrationContext context)
	{
		consumerConfigurator.Options<JobOptions<MaintenanceTask>>(options => options
			.SetRetry(r => r.Interval(3, TimeSpan.FromSeconds(30)))
			.SetJobTimeout(TimeSpan.FromMinutes(10))
			.SetConcurrentJobLimit(5));
		//	.SetJobTypeProperties(x => x.Set("DistributionStrategy", "DataCenter"))
		//	.SetInstanceProperties(x => x.Set("DataCenter", Environment.GetEnvironmentVariable("DATA_CENTER"))));
	}
}