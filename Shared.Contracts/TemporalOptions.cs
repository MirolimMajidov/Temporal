namespace Shared.Contracts;

public class TemporalOptions
{
    public string Host { get; set; } = "localhost:7233"; //test-cbs-temporal.alif.tj:7233
    public string Namespace { get; set; } = "default";
}