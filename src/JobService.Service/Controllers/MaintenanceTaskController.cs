namespace JobService.Service.Controllers;

using JobService.Components;
using MassTransit;
using Microsoft.AspNetCore.Mvc;


[ApiController]
[Route("[controller]")]
public class MaintenanceTaskController :
	ControllerBase
{
	readonly ILogger<MaintenanceTaskController> _logger;

	public MaintenanceTaskController(ILogger<MaintenanceTaskController> logger)
	{
		_logger = logger;
	}

	[HttpPost("{scheduleMinutes}/{taskId:guid}")]
	public async Task<IActionResult> CreateOrUpdateRecurringJobAsync(int scheduleMinutes, Guid? taskId, [FromServices] IPublishEndpoint publishEndpoint)
	{
		var task = new MaintenanceTask
		{
			Id = taskId ?? Guid.NewGuid(), // Use the provided taskId or generate a new one  
			Name = "Sample Task",
			Description = "This is a sample maintenance task.",
			IsActive = true
		};

		_logger.LogInformation("Sending MaintenanceTask job creation/updation: {TaskId}", task.Id);

		var jobName = $"MaintenanceTask_{task.Id}";

		await publishEndpoint.AddOrUpdateRecurringJob(
			jobName,
			new MaintenanceTask
			{
				Id = task.Id,
				Name = task.Name,
				Description = task.Description,
				IsActive = task.IsActive
			},
			x => x.Every(minutes: scheduleMinutes));

		_logger.LogInformation("Created/Updated recurring job {JobName} with cron {CronExpression}", jobName, scheduleMinutes);

		return Ok(new
		{
			task.Id,
			task.Name
		});
	}

	[HttpDelete("{taskId:guid}")]
	public async Task<IActionResult> RemoveRecurringJobAsync(Guid taskId, [FromServices] IPublishEndpoint publishEndpoint)
	{
		var jobName = $"MaintenanceTask_{taskId}";

		_logger.LogInformation("CancelJob job: {TaskId}", taskId);
		try
		{
			// Specify the type argument explicitly to resolve CS0411  
			await publishEndpoint.CancelRecurringJob<MaintenanceTask>(jobName, "user cancelled the recurring job");

			_logger.LogInformation("Removed recurring job {JobName}", jobName);
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
			throw;
		}

		return Ok();
	}
}