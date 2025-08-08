namespace JobService.Components;

using MassTransit;
using Microsoft.Extensions.Logging;


public class MaintenanceJobConsumer :
	IJobConsumer<MaintenanceTask>
{
	readonly ILogger<MaintenanceJobConsumer> _logger;

	public MaintenanceJobConsumer(ILogger<MaintenanceJobConsumer> logger)
	{
		_logger = logger;
	}

	public async Task Run(JobContext<MaintenanceTask> context)
	{
		//var variance = context.TryGetJobState(out ConsumerState? state)
		//    ? TimeSpan.FromMilliseconds(state!.Variance)
		//    : TimeSpan.FromMilliseconds(Random.Shared.Next(8399, 28377));

		//    _logger.LogInformation("GetJobState: {variance}", variance);

		_logger.LogInformation("Running MaintenanceTask: {Id} {Name}", context.Job.Id, context.Job.Name);

		try
		{
			// await context.SetJobProgress(0, (long)variance.TotalMilliseconds);

			await Task.Delay(30000, context.CancellationToken);

			//   await context.SetJobProgress((long)variance.TotalMilliseconds, (long)variance.TotalMilliseconds);

			//  await context.Publish<VideoConverted>(context.Job);

			_logger.LogInformation("MaintenanceTask completed: {Id} {Name}", context.Job.Id, context.Job.Name);
		}

		catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
		{
			_logger.LogInformation("MaintenanceTask  OperationCanceledException: {Id} {Name}", context.Job.Id, context.Job.Name);

		//	await context.SaveJobState(new ConsumerState { Variance = 10000 });

			throw;
		}
		catch (ExceptionInfoException ex)
		{
			_logger.LogInformation("MaintenanceTask exception: {Id} {Name} {ex}", context.Job.Id, context.Job.Name, ex);

			//	await context.SaveJobState(new ConsumerState { Variance = 10000 });

			throw;
		}
	}
}