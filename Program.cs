using AISandbox.Game;

namespace AISandbox
{
    static class Program
    {
        static void Main()
        {
            using (var game = new SandboxGame())
            {
                game.Run();
            }
        }
    }
}
