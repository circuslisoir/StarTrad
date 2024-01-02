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
        if (Properties.Settings.Default.TranslationUpdateMethod == (byte)TranslationUpdateMethodEnum.Never)
            //Pas de MAJ auto
            return;
        else if (Properties.Settings.Default.TranslationUpdateMethod == (byte)TranslationUpdateMethodEnum.StartRsiLauncher)
            throw new NotImplementedException();
        //else if (Properties.Settings.Default.TranslationUpdateMethod == nameof(TranslationUpdateMethodEnum.StartRsiLauncher) && !ProcessWatcher.IsProcessWatcherRunning())
        //ProcessWatcher.StartProcessWatcher();
        else if (Properties.Settings.Default.TranslationUpdateMethod == (byte)TranslationUpdateMethodEnum.EverySixHours ||
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
            TranslationInstaller.InstallTranslationWithoutUI();
            StartTimer();
        }
        else
        {
            //Lancé un tempTimer sur le temps qui reste.
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
                //Toutes les 6 heurs
                timer.Interval = (int)TimeSpan.FromHours(6).TotalMilliseconds;
                break;
            case (byte)TranslationUpdateMethodEnum.EveryTwelveHours:
                //Toutes les 12 heurs
                timer.Interval = (int)TimeSpan.FromHours(12).TotalMilliseconds;
                break;
            case (byte)TranslationUpdateMethodEnum.EveryTwentyFourHours:
                //Toutes les 12 heurs
                timer.Interval = (int)TimeSpan.FromHours(24).TotalMilliseconds;
                break;
            case (byte)TranslationUpdateMethodEnum.EveryFourtyEightHours:
                //Toutes les 12 heurs
                timer.Interval = (int)TimeSpan.FromHours(48).TotalMilliseconds;
                break;
            case (byte)TranslationUpdateMethodEnum.EverySevenDays:
                //Toutes les 12 heurs
                timer.Interval = (int)TimeSpan.FromDays(7).TotalMilliseconds;
                break;
        }

        //A chaque itération du timer lancé Timer_Tick
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

    private static void TempTimer_Tick(object sender, EventArgs e)
    {
        //Lancement d'une maj
        TranslationInstaller.InstallTranslationWithoutUI();
        StopTimer();
        StartTimer();
    }
    private static void Timer_Tick(object sender, EventArgs e)
    {
        //Lancement d'une maj
        TranslationInstaller.InstallTranslationWithoutUI();
    }



    #endregion

}
