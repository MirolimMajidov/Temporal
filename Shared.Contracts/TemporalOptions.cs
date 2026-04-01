namespace Shared.Contracts;

public class TemporalOptions
{
    private const bool RunInLocalMachine = false;
    public string Host { get; set; } = RunInLocalMachine ? "localhost:7233" : "test-cbs-temporal-grpc.alif.tj:443";
    public string Namespace { get; set; } = "default";
    public bool UseTls { get; set; } = !RunInLocalMachine;
}