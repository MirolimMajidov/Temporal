namespace Shared.Contracts;

public class TemporalOptions
{
    public string Host { get; set; } = "localhost:7233"; //test-cbs-temporal.alif.tj:7233, test-cbs-temporal.alif.tj:443/grpc
    public string Namespace { get; set; } = "default";
    public bool UseTls { get; set; } = false;
}