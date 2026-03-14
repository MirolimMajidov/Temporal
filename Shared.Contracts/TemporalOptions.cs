namespace Shared.Contracts;

public class TemporalOptions
{
    public string Host { get; set; } = "test-cbs-temporal-grpc.alif.tj:443";
    //public string Host { get; set; } = "localhost:7233";
    public string Namespace { get; set; } = "default";
    public bool UseTls { get; set; } = true;
}