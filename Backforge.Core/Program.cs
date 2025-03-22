namespace Backforge.Core;

public class Program
{
    public static async Task Main(string[] args)
    {
        var service = new LlamaService("C:\\Users\\gsilv\\.ollama\\models\\blobs/sha256-dde5aa3fc5ffc17176b5e8bdc82f587b24b2678c6c66101bf7da77af9f7ccdff");
        var prompt = "Faça uma api em .NET completa para integração com stripe";

        Thread.Sleep(5000);
        
        await service.ExecuteTaskAsync(prompt);

        Console.WriteLine(); 
    }
}