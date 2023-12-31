using StarTrad.Helper.ComboxList;
using StarTrad.Tool;
using System.Diagnostics;

namespace StarTrad.Helper;
internal class ProcessWatcher
{
    private readonly string rsiLaucnherProcessName = "RSI Launcher";
    private static bool _rsiStarted;
    private static bool _isProcessWatcherRunning = false;

    #region Public

    public static bool IsProcessWatcherRunning() => _isProcessWatcherRunning;

    public static void StopProcessWatcher() => _isProcessWatcherRunning = false;

    public static async Task StartProcessWatcher()
    {
        _isProcessWatcherRunning = true;
        using (var watcher = new ManagementEventWatcher(
            //Récup de toute la Trace des process qui start
            new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace")))
        {
            //Ajout du process start handler sur l'event de nouveau process
            watcher.EventArrived += processStartHandler;
            watcher.Start();

            //faire fonctionner le process watcher tant que _isProcessWatcherRunning est à true
            while (_isProcessWatcherRunning)
            {
                // Ajouter une petite pause pour éviter une utilisation excessive du processeur
                await Task.Delay(TimeSpan.FromSeconds(1));
            }

            watcher.Stop();
        }
    }


    #endregion

    #region Private
    private readonly EventArrivedEventHandler processStartHandler = async (sender, e) =>
    {
        if (IsProcessRunning(rsiProcessName) && !_rsiStarted)
        {
            LoggerFactory.LogInformation($"Le processus {rsiProcessName} a été ouvert");
            _rsiStarted = true;

            if (Properties.Settings.Default.TranslationUpdateMethod == nameof(TranslationUpdateMethodEnum.StartRsiLauncher))
                TranslationInstaller.Run();

        }
    };


    private static bool IsProcessRunning(string processName) => Process.GetProcessesByName(processName).Any();
    #endregion
}
