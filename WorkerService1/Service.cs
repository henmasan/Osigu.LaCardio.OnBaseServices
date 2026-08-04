public interface IService
{
    Task DoWorkAsync();
}

public class Service : IService
{
    public async Task DoWorkAsync()
    {
        // Simula una tarea asíncrona
        await Task.Delay(1000);
        Console.WriteLine("Trabajo realizado en MyService.");
    }
}