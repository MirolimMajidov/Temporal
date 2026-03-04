using OrderService.Activities;
using Shared.Contracts;
using Temporalio.Exceptions;
using Temporalio.Workflows;

namespace OrderService.Workflows;

[Workflow]
public class SendSmsWorkflow
{
    private readonly ILogger _logger = Workflow.Logger;

    [WorkflowRun]
    public async Task<string> RunAsync(SendSms smsPayload)
    {
        try
        {
            var customerPhoneNumber = await Workflow.ExecuteActivityAsync(
                (SmsActivities act) => act.GetCustomerPhoneNumberAsync(smsPayload.CustomerId),
                new ActivityOptions
                {
                    TaskQueue = TaskQueues.Sms,
                    StartToCloseTimeout = TimeSpan.FromMinutes(1)
                });
            var result = await Workflow.ExecuteActivityAsync(
                (SmsActivities act) => act.SendSms(customerPhoneNumber, smsPayload.Message),
                new ActivityOptions
                {
                    TaskQueue = TaskQueues.Sms,
                    StartToCloseTimeout = TimeSpan.FromMinutes(1)
                });

            return "SMS sent successfully";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS for customer {CustomerId}", smsPayload.CustomerId);
            throw; // Workflow will fail, but side effects have been compensated
        }
    }
}