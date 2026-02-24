namespace Shared.Contracts;

public class TemporalOptions
{
    public string Host { get; set; } = "test-cbs-temporal.alif.tj:443/grpc"; //test-cbs-temporal.alif.tj:7233
    public string Namespace { get; set; } = "default";
}