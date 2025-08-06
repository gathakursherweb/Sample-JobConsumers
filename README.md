# Job Consumer Sample

Generate certificate and configure local machine:

PC:

```PowerShell
dotnet dev-certs https --clean
dotnet dev-certs https -ep "$env:USERPROFILE\.aspnet\https\aspnetapp.pfx" -p Passw0rd
dotnet dev-certs https --trust
```

MAC:

```
dotnet dev-certs https --clean
dotnet dev-certs https -ep ~/.aspnet/https/aspnetapp.pfx -p Passw0rd
dotnet dev-certs https --trust
```

Read more here:
https://learn.microsoft.com/en-us/aspnet/core/security/docker-compose-https?view=aspnetcore-6.0#starting-a-container-with-https-support-using-docker-compose

Use the `docker-compose.yml` file to startup JobService, and PostgreSQL. Or run `docker compose up --build`.    
Navigate to `https://localhost:5003/swagger` to post a video to convert.

The SQL transport is using PostgreSQL in this scenario. To use SQL Server, switch to the `sql-server` branch.

Supports local debugging from VS/Rider. Ctrl + F5 and navigate to:  
`http://localhost:5002/swagger`  
`https://localhost:5003/swagger`


You need to create 4-5 recurring jobs making requests to MaintenanceTask controller as in below screenshot with schdule of 2, 5,10, 30, 50 minutes which I did. 

<img width="721" height="267" alt="image" src="https://github.com/user-attachments/assets/21d4ebe2-f532-401d-aa2b-48b07dc2e14b" />

For some time jobs runs fine and then it start giving errors, I got below error and job never reschduled again and went to error state forever. I even tried to :

1) MassTransit.UnhandledEventException
•
Aug 06 09:21:39 (failed about 1 hour ago)
The JobSlotAllocated event is not handled during the WaitingForSlot state for the JobStateMachine state machine
   at MassTransit.MassTransitStateMachine`1.DefaultUnhandledEventCallback(UnhandledEventContext`1 context) in /_/src/MassTransit/SagaStateMachine/MassTransitStateMachine.cs:line 222
   at MassTransit.MassTransitStateMachine`1.UnhandledEvent(BehaviorContext`1 context, State state) in /_/src/MassTransit/SagaStateMachine/MassTransitStateMachine.cs:line 1266
   at MassTransit.MassTransitStateMachine`1.<DeclareState>b__90_0(BehaviorContext`1 c, State s) in /_/src/MassTransit/SagaStateMachine/MassTransitStateMachine.cs:line 729
   at MassTransit.MassTransitStateMachine`1.StateMachineState.MassTransit.State<TInstance>.Raise[T](BehaviorContext`2 context) in /_/src/MassTransit/SagaStateMachine/SagaStateMachine/StateMachineState.cs:line 165
   at MassTransit.MassTransitStateMachine`1.MassTransit.StateMachine<TInstance>.RaiseEvent[T](BehaviorContext`2 context) in /_/src/MassTransit/SagaStateMachine/MassTransitStateMachine.cs:line 135
   at MassTransit.Middleware.StateMachineSagaMessageFilter`2.Send(SagaConsumeContext`2 context, IPipe`1 next) in /_/src/MassTransit/Middleware/StateMachineSagaMessageFilter.cs:line 66

2) MassTransit.UnhandledEventException

The JobSlotUnavailable event is not handled during the WaitingForSlot state for the JobStateMachine state machine. I am using SampleJobConsumer application and dynamically creating job.
