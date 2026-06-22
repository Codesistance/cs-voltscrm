using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using Microsoft.Extensions.Options;
using VoltsCRM.Application.Common.Interfaces;
using VoltsCRM.Application.Common.Options;

namespace VoltsCRM.Infrastructure.Email;

/// <summary>
/// Sends email via AWS SES (v2). Credentials and region resolve from the default AWS chain
/// (env vars / ECS task role in production).
/// </summary>
public sealed class SesEmailSender(IAmazonSimpleEmailServiceV2 ses, IOptions<EmailOptions> options) : IEmailSender
{
    private readonly EmailOptions _options = options.Value;

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        var request = new SendEmailRequest
        {
            FromEmailAddress = _options.FromAddress,
            Destination = new Destination { ToAddresses = [to] },
            Content = new EmailContent
            {
                Simple = new Message
                {
                    Subject = new Content { Data = subject },
                    Body = new Body { Html = new Content { Data = htmlBody } },
                },
            },
        };

        await ses.SendEmailAsync(request, ct);
    }
}
