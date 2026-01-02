using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using System.Net.Sockets;

namespace Mediconnet_Backend.Infrastructure.HealthChecks;

/// <summary>
/// Health Check pour le service email (SMTP)
/// </summary>
public class EmailServiceHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;

    public EmailServiceHealthCheck(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var smtpServer = _configuration["EmailSettings:SmtpServer"] ?? "localhost";
            var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "1025");

            using var tcpClient = new TcpClient();
            var connectTask = tcpClient.ConnectAsync(smtpServer, smtpPort);
            
            // Timeout de 5 secondes
            if (await Task.WhenAny(connectTask, Task.Delay(5000, cancellationToken)) == connectTask)
            {
                if (tcpClient.Connected)
                {
                    return HealthCheckResult.Healthy($"SMTP server {smtpServer}:{smtpPort} is reachable");
                }
            }

            return HealthCheckResult.Degraded($"SMTP server {smtpServer}:{smtpPort} is not reachable");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded("Email service health check failed", ex);
        }
    }
}
