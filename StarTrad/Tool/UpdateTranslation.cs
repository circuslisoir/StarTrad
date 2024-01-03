using StarTrad.Helper;
using StarTrad.Helper.ComboxList;
using System;

namespace StarTrad.Tool;

internal static class UpdateTranslation
{
    private static System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();

    #region Public

    public static void ReloadAutoUpdate()
    {
        StopAutoUpdate();
        StartAutoUpdate();
    }

    public static void StartAutoUpdate()
    {
        LoggerFactory.LogInformation($"Lancement de la mise a jour automatique; toute les : ${Properties.Settings.Default.TranslationUpdateMethod}");

        //Vérification du type de MAJ
        if (Properties.Settings.Default.TranslationUpdateMethod == (byte)TranslationUpdateMethodEnum.Never) {
            return;
        }

        // Not implemented
        /*if (Properties.Settings.Default.TranslationUpdateMethod == (byte)TranslationUpdateMethodEnum.StartRsiLauncher) {
            if (!ProcessWatcher.IsProcessWatcherRunning()) {
                ProcessWatcher.StartProcessWatcher();
            }

            return;
        }*/

        if (Properties.Settings.Default.TranslationUpdateMethod == (byte)TranslationUpdateMethodEnum.EverySixHours ||
            Properties.Settings.Default.TranslationUpdateMethod == (byte)TranslationUpdateMethodEnum.EveryTwelveHours ||
            Properties.Settings.Default.TranslationUpdateMethod == (byte)TranslationUpdateMethodEnum.EveryTwentyFourHours ||
            Properties.Settings.Default.TranslationUpdateMethod == (byte)TranslationUpdateMethodEnum.EveryFourtyEightHours ||
            Properties.Settings.Default.TranslationUpdateMethod == (byte)TranslationUpdateMethodEnum.EverySevenDays)
        {
            StartTempTimer();
        }
    }

    public static void StopAutoUpdate()
    {
        LoggerFactory.LogInformation("Arrêt de la mise a jour automatique");
        if (timer.Enabled)
            StopTimer();
    }

    #endregion

    #region Private

    private static void StartTempTimer()
    {
        DateTime lastUpdateDate = Properties.Settings.Default.LastUpdateDate;
        DateTime nextUpdateDate = lastUpdateDate;

        //Calcul de la date de prochaine maj
        switch (Properties.Settings.Default.TranslationUpdateMethod)
        {
            case (byte)TranslationUpdateMethodEnum.EverySixHours:
                nextUpdateDate.AddHours(6);
                break;
            case (byte)TranslationUpdateMethodEnum.EveryTwelveHours:
                nextUpdateDate.AddHours(12);
                break;
            case (byte)TranslationUpdateMethodEnum.EveryTwentyFourHours:
                nextUpdateDate.AddHours(24);
                break;
            case (byte)TranslationUpdateMethodEnum.EveryFourtyEightHours:
                nextUpdateDate.AddHours(48);
                break;
            case (byte)TranslationUpdateMethodEnum.EverySevenDays:
                nextUpdateDate.AddDays(7);
                break;
        }

        //Si la prochaine maj est à un instant T passé
        if (nextUpdateDate < DateTime.Now)
        {
            //Faire une maj puis lancé le timer
            TranslationInstaller.Install(true);
            StartTimer();
        }
        else
        {
            //Lancer un tempTimer sur le temps qui reste.
            TimeSpan difference = DateTime.Now - lastUpdateDate;
            timer.Interval = (int)difference.TotalMilliseconds;
            timer.Tick += TempTimer_Tick;
            timer.Start();
        }
    }

    private static void StartTimer()
    {
        //Configure le temps du timer en fonction de la périodicité de la maj
        switch (Properties.Settings.Default.TranslationUpdateMethod)
        {
            case (byte)TranslationUpdateMethodEnum.EverySixHours:
                //Toutes les 6 heures
                timer.Interval = (int)TimeSpan.FromHours(6).TotalMilliseconds;
                break;
            case (byte)TranslationUpdateMethodEnum.EveryTwelveHours:
                //Toutes les 12 heures
                timer.Interval = (int)TimeSpan.FromHours(12).TotalMilliseconds;
                break;
            case (byte)TranslationUpdateMethodEnum.EveryTwentyFourHours:
                //Toutes les 12 heures
                timer.Interval = (int)TimeSpan.FromHours(24).TotalMilliseconds;
                break;
            case (byte)TranslationUpdateMethodEnum.EveryFourtyEightHours:
                //Toutes les 12 heures
                timer.Interval = (int)TimeSpan.FromHours(48).TotalMilliseconds;
                break;
            case (byte)TranslationUpdateMethodEnum.EverySevenDays:
                //Toutes les 12 heures
                timer.Interval = (int)TimeSpan.FromDays(7).TotalMilliseconds;
                break;
        }

        //A chaque itération du timer lancer Timer_Tick
        timer.Tick += Timer_Tick;
        timer.Start();
    }

    private static void StopTimer()
    {
        timer.Stop();
        timer.Dispose();
        timer.Tick -= TempTimer_Tick;
        timer.Tick -= Timer_Tick;
    }

    private static void TempTimer_Tick(object? sender, EventArgs e)
    {
        //Lancement d'une maj
        TranslationInstaller.Install(true);
        StopTimer();
        StartTimer();
    }
    private static void Timer_Tick(object? sender, EventArgs e)
    {
        //Lancement d'une maj
        TranslationInstaller.Install(true);
    }

    #endregion
}
