using StarTrad.Helper;
using System;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Threading.Tasks;

namespace StarTrad.Tool;
internal class ProcessHandler
{
    private static bool IsRunning = false;
    private readonly static string rsiProcessName = "RSI Launcher.exe";
    private readonly static string rsiShortProcessName = "RSI Launcher";
    private static bool IsRsiStarted = false;

    public static void StartProcessHandler()
    {
        LoggerFactory.LogInformation("Lancement du processHandler");
        IsRunning = true;
        WatchRsiProcess();
    }
    public static void StopProcessWatcher()
    {
        LoggerFactory.LogInformation("Arrêt du processHandler");
        IsRunning = false;
    }
    public static bool IsProcessHandlerRunning() => IsRunning;

    private static async void WatchRsiProcess()
    {
        // Créer un événement pour surveiller le lancement et l'arrêt de nouveaux processus
        WqlEventQuery startQuery = new WqlEventQuery("SELECT * FROM __InstanceCreationEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_Process' AND TargetInstance.Name = 'RSI Launcher.exe'");
        WqlEventQuery stopQuery = new WqlEventQuery("SELECT * FROM __InstanceDeletionEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_Process' AND TargetInstance.Name = 'RSI Launcher.exe'");

        // Créer un gestionnaire d'événements pour la création et la suppréssion de nouveaux processus
        ManagementEventWatcher startWatcher = new ManagementEventWatcher(startQuery);
        ManagementEventWatcher stopWatcher = new ManagementEventWatcher(stopQuery);

        // Attacher le gestionnaire d'événements
        startWatcher.EventArrived += ProcessStarted;
        stopWatcher.EventArrived += ProcessStoped;

        // Démarrer la surveillance
        startWatcher.Start();
        stopWatcher.Start();

        while (IsRunning)
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
        }
        startWatcher.Stop();
        stopWatcher.Stop();
    }


    private static bool IsProcessRunning(string processName) => Process.GetProcessesByName(processName).Any();

    private static void ProcessStarted(object sender, EventArrivedEventArgs e)
    {
        //Récupération du nom du processus
        string processName = (e.NewEvent["TargetInstance"] as ManagementBaseObject)?["Name"]?.ToString() ?? throw new NullReferenceException();

        //Vérification si le process run déjà
        if (IsProcessRunning(rsiShortProcessName) && !IsRsiStarted)
        {
            LoggerFactory.LogInformation($"Le processus {rsiProcessName} a été ouvert");
            IsRsiStarted = true;
            StartTranslationUpdate();
        }
    }

    private static void ProcessStoped(object sender, EventArrivedEventArgs e)
    {
        //Récupération du nom du processus
        string processName = (e.NewEvent["TargetInstance"] as ManagementBaseObject)?["Name"]?.ToString() ?? throw new NullReferenceException();

        //Vérification si le process run déjà
        if (!IsProcessRunning(rsiShortProcessName) && IsRsiStarted)
        {
            IsRsiStarted = false;
            LoggerFactory.LogInformation($"Le processus {rsiProcessName} a été fermé");
        }
    }

    private static void StartTranslationUpdate()
    {
        TranslationInstaller.Install(true);
    }
}
