using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.BackgroundJobs;
using RabbitMQ.Client;
using System.Text;

namespace Unity.Notifications.EmailNotifications;


[BackgroundJobName("emails")]
public class EmailSendingArgs
{
	public required string EmailAddress { get; set; }
	public required string Subject { get; set; }
	public required string Body { get; set; }
}

public class RegistrationService : ApplicationService
{
    private readonly IBackgroundJobManager _backgroundJobManager;

    public RegistrationService(IBackgroundJobManager backgroundJobManager)
    {
        _backgroundJobManager = backgroundJobManager;
    }

    public async Task RegisterAsync(string emailAddress, string subject, string body)
    {
        //TODO: Create new user in the database...
        var factory = new ConnectionFactory { HostName = "localhost" };
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.QueueDeclare(queue: "unity_jobs.emails",
                            durable: false,
                            exclusive: false,
                            autoDelete: false,
                            arguments: null);

        var emailArgs = new EmailSendingArgs
        {
            EmailAddress = emailAddress,
            Subject = subject,
            Body = body
        };

        var json = Newtonsoft.Json.JsonConvert.SerializeObject(emailArgs);
        var bodyPublish = Encoding.UTF8.GetBytes(json);

        channel.BasicPublish(exchange: string.Empty,
                            routingKey: "unity_jobs.emails",
                            basicProperties: null,
                            body: bodyPublish);


        await _backgroundJobManager.EnqueueAsync(
            emailArgs
        );
    }
}