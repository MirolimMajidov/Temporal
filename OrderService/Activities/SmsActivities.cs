using Temporalio.Activities;

namespace OrderService.Activities;

public class SmsActivities
{
    [Activity]
    public async Task<string> GetCustomerPhoneNumberAsync(Guid customerId)
    {
        await Task.Delay(TimeSpan.FromSeconds(2));
        return Guid.NewGuid().ToString();
    }

    [Activity]
    public async Task<bool> SendSms(string phoneNumber, string message)
    {
        await Task.Delay(TimeSpan.FromSeconds(2));
        return true;
    }
}