namespace Common.Hosting.Worker.Options;

public class WorkerOptions
{
    public string Name { get; set; } = "Worker";
    public int DelayInSeconds { get; set; } = 60;
}
