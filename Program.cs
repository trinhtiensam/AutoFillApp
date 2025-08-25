
using System;
using System.IO;
using System.Text.Json;

class Program
{
    static void Main()
    {
        Console.WriteLine("=== AutoFillApp Demo ===");
        string profilePath = "profiles.json";
        
        if (!File.Exists(profilePath))
        {
            Console.WriteLine("profiles.json not found. Creating sample...");
            File.WriteAllText(profilePath, JsonSerializer.Serialize(new {
                Name = "Nguyen Van A",
                Email = "test@example.com",
                Phone = "0123456789"
            }, new JsonSerializerOptions { WriteIndented = true }));
        }

        string json = File.ReadAllText(profilePath);
        Console.WriteLine("Loaded profile:");
        Console.WriteLine(json);

        Console.WriteLine("\nSimulating autofill...");
        Console.WriteLine("Typing Name...");
        Console.WriteLine("Typing Email...");
        Console.WriteLine("Typing Phone...");
        Console.WriteLine("Done!");
    }
}
