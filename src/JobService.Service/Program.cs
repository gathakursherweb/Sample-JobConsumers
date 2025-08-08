using System.Reflection;
using JobService.Components;
using JobService.Service;
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSwag;
using ResQueue;
using ResQueue.Enums;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
	.MinimumLevel.Information()
	.MinimumLevel.Override("MassTransit", LogEventLevel.Debug)
	.MinimumLevel.Override("MassTransit.Saga", LogEventLevel.Debug)  // Add saga-specific logging
	.MinimumLevel.Override("MassTransit.StateMachine", LogEventLevel.Debug)  // Add state machine logging
	    .MinimumLevel.Override("MassTransit.Scheduling", LogEventLevel.Debug)       // For scheduling/recurring jobs
    .MinimumLevel.Override("MassTransit.Messages", LogEventLevel.Debug)        // For job messages
    .MinimumLevel.Override("MassTransit.Consumers", LogEventLevel.Debug)       // For job consumers
    .MinimumLevel.Override("MassTransit.Context", LogEventLevel.Debug)         // For saga contexts

	.MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
	.MinimumLevel.Override("Microsoft.Hosting", LogEventLevel.Information)
	.MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
	.MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Information)
	    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Update", LogEventLevel.Information)  // Enable to see EF updates

	.Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")  // Better format
	.CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers();
builder.Services.AddOpenApiDocument(cfg => cfg.PostProcess = d =>
{
	d.Info.Title = "Job Consumer Sample";
	d.Info.Contact = new OpenApiContact
	{
		Name = "Job Consumer Sample using MassTransit",
		Email = "support@masstransit.io"
	};
});

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var connectionString = builder.Configuration.GetConnectionString("JobService");

builder.Services.AddOptions<SqlTransportOptions>()
	.Configure(options =>
	{
		options.ConnectionString = connectionString;
	});

builder.Services.AddPostgresMigrationHostedService();

// Add web-based dashboard
builder.Services.AddResQueue(opt =>
{
	opt.SqlEngine = ResQueueSqlEngine.Postgres;
});
builder.Services.AddResQueueMigrationsHostedService();

builder.Services.AddDbContext<JobServiceSagaDbContext>(optionsBuilder =>
{
	optionsBuilder.UseNpgsql(connectionString, m =>
	{
		m.MigrationsAssembly(Assembly.GetExecutingAssembly().GetName().Name);
		m.MigrationsHistoryTable($"__{nameof(JobServiceSagaDbContext)}");
		
		m.EnableRetryOnFailure();
	});
});

builder.Services.AddHostedService<MigrationHostedService<JobServiceSagaDbContext>>();


builder.Services.AddMassTransit(x =>
{
	x.AddSqlMessageScheduler();

	x.ConfigureUsageTelemetryOptions(options => options.ReportDelay = TimeSpan.FromSeconds(40));

	//x.AddConsumer<ConvertVideoJobConsumer, ConvertVideoJobConsumerDefinition>()
	//    .Endpoint(e => e.Name = "convert-job-queue");

	x.AddConsumer<MaintenanceJobConsumer, MaintenanceJobConsumerDefinition>()
		.Endpoint(e => e.Name = "maintenance-job-queue");

	//  x.AddConsumer<TrackVideoConvertedConsumer>();

  //	x.TryAddJobDistributionStrategy<DataCenterJobDistributionStrategy>();

	// x.AddConsumer<MaintenanceConsumer>();

    x.SetJobConsumerOptions(options => 
    {
        options.HeartbeatInterval = TimeSpan.FromMinutes(1);

    });
		x.AddJobSagaStateMachines(options =>
		{
			options.FinalizeCompleted = false;
			//options.SlotWaitTime = TimeSpan.FromSeconds(30);
		})
		.SetPartitionedReceiveMode()
		.EntityFrameworkRepository(r =>
		{
			r.ExistingDbContext<JobServiceSagaDbContext>();
			r.UsePostgres();
			r.ConcurrencyMode = ConcurrencyMode.Optimistic;
		});

	x.SetKebabCaseEndpointNameFormatter();

	x.UsingPostgres((context, cfg) =>
	{
		cfg.UseSqlMessageScheduler();
		cfg.UseJobSagaPartitionKeyFormatters();
	cfg.UseMessageRetry(r =>
	{
		r.Handle<UnhandledEventException>(ex =>
		{
			Log.Warning("Handling UnhandledEventException: {ExceptionMessage}", ex.Message);
			return ex.Message.Contains("JobSlotAllocated");
		});
		r.Handle<InvalidOperationException>(ex =>
		{
			Log.Warning("Handling InvalidOperationException: {ExceptionMessage}", ex.Message);
			return ex.Message.Contains("cannot be tracked");
		});
		r.Exponential(3, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(2));
	
		// Logging on retry is not directly supported here; consider using middleware or logging inside your consumer.
	});

		cfg.ConfigureEndpoints(context);
	});
});

builder.Services.AddOptions<MassTransitHostOptions>()
	.Configure(options =>
	{
		options.WaitUntilStarted = true;
		options.StartTimeout = TimeSpan.FromMinutes(1);
		options.StopTimeout = TimeSpan.FromMinutes(1);
	});

builder.Services.AddOptions<HostOptions>()
	.Configure(options => options.ShutdownTimeout = TimeSpan.FromMinutes(1));

//builder.Services.AddHostedService<RecurringJobConfigurationService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
	app.UseDeveloperExceptionPage();

app.UseOpenApi();
app.UseSwaggerUi();

app.UseRouting();
app.UseAuthorization();

app.UseResQueue();

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
	Predicate = check => check.Tags.Contains("ready"),
	ResponseWriter = HealthCheckResponseWriter
});

app.MapHealthChecks("/health/live", new HealthCheckOptions { ResponseWriter = HealthCheckResponseWriter });

app.MapControllers();

await app.RunAsync();

static Task HealthCheckResponseWriter(HttpContext context, HealthReport result)
{
	context.Response.ContentType = "application/json";

	return context.Response.WriteAsync(result.ToJsonString());
}