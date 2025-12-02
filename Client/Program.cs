namespace RealmOfReality.Client;

public static class Program
{
    [STAThread]
    public static void Main()
    {
        using var game = new RealmGame();
        game.Run();
    }
}
