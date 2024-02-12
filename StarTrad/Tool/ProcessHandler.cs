using StarTrad.Enum;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Threading.Tasks;

namespace StarTrad.Tool;

internal class ProcessHandler
{
    private static bool isRunning = false;
    private readonly static string rsiProcessName = "RSI Launcher.exe";
    private readonly static string rsiShortProcessName = "RSI Launcher";
    private static bool isRsiStarted = false;

    /*
    Public
    */

    public static void StartProcessHandler()
    {
        Logger.LogInformation("Lancement du processHandler");
        isRunning = true;
        WatchRsiProcess();
    }
    public static void StopProcessWatcher()
    {
        Logger.LogInformation("Arrêt du processHandler");
        isRunning = false;
    }

    public static void StartExternalProcess()
    {
        Debug.WriteLine("ENTER StartExternalProcess()");

        List<string> processToStartList = Properties.Settings.Default.ExternalTools.Split(";").ToList();

        if (processToStartList.Count < 1) {
            return;
        }

        List<ProcessStartInfo> processStartInfoList = new();

        processStartInfoList.AddRange(
            processToStartList.Select(processPath => new ProcessStartInfo {
                FileName = processPath,
                UseShellExecute = true,
                CreateNoWindow = true,
            })
        );

        try {
            processStartInfoList.ForEach(process => {
                string processName = process.FileName.Split("\\").Last().Replace(".exe", string.Empty).Trim();

                if (!IsProcessRunning(processName)) {
                    Process.Start(process);
                }
            });
        } catch (Exception ex) {
            Logger.LogWarning($"Erreur lors du lancement du processus : {ex.Message}");
        }

    }

    /*
    Private
    */

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

        while (isRunning)
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
        //string processName = (e.NewEvent["TargetInstance"] as ManagementBaseObject)?["Name"]?.ToString() ?? throw new NullReferenceException();

        //Vérification si le process run déjà
        if (IsProcessRunning(rsiShortProcessName) && !isRsiStarted)
        {
            Logger.LogInformation($"Le processus {rsiProcessName} a été ouvert");
            isRsiStarted = true;

            if ((TranslationUpdateMethod)Properties.Settings.Default.TranslationUpdateMethod == TranslationUpdateMethod.StartRsiLauncher)
                TranslationInstaller.Install(true);

            if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.ExternalTools))
                StartExternalProcess();
        }
    }

    private static void ProcessStoped(object sender, EventArrivedEventArgs e)
    {
        //Récupération du nom du processus
        //string processName = (e.NewEvent["TargetInstance"] as ManagementBaseObject)?["Name"]?.ToString() ?? throw new NullReferenceException();

        //Vérification si le process run déjà
        if (!IsProcessRunning(rsiShortProcessName) && isRsiStarted)
        {
            isRsiStarted = false;
            Logger.LogInformation($"Le processus {rsiProcessName} a été fermé");
        }
    }

    /*
    Accessor
    */

    public static bool IsProcessHandlerRunning
    {
        get { return isRunning; }
    }
}
