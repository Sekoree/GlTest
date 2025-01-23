// See https://aka.ms/new-console-template for more information

using GlTest;

internal class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
        using Game game = new Game(1280, 720, "LearnOpenTK");
        game.Run();
    }
}