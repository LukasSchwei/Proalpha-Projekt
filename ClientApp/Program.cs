namespace ClientApp;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // Create and run the application
        using (var form = new MainMenu())
        {
            Application.Run(form);
        }
    }
}