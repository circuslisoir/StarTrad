using StarTrad.Helper;
using StarTrad.Helper.ComboxList;


namespace StarTrad.Tool;
internal static class UpdateTranslation
{
    private static System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();

    public static void StartAutoUpdate()
    {
        LoggerFactory.LogInformation($"Lancement de la mise a jour automatique toute les : ${Properties.Settings.Default.TranslationUpdateMethod}");
        //Vérification du type de MAJ 
        if (Properties.Settings.Default.TranslationUpdateMethod == nameof(TranslationUpdateMethodEnum.Never))
            //Pas de MAJ auto
            return;
        else if (Properties.Settings.Default.TranslationUpdateMethod == nameof(TranslationUpdateMethodEnum.StartRsiLauncher))
            //Maj à chaque lancement du launcher RSI
            throw new NotImplementedException();
        else if (Properties.Settings.Default.TranslationUpdateMethod == nameof(TranslationUpdateMethodEnum.EveryTwoHours) ||
                Properties.Settings.Default.TranslationUpdateMethod == nameof(TranslationUpdateMethodEnum.EverySixHours) ||
                Properties.Settings.Default.TranslationUpdateMethod == nameof(TranslationUpdateMethodEnum.EveryTwelveHours))
        {
            //MAJ auto préiodique 2,6 ou 12 heurs 
            StartTimer();
        }
        else return;

    }

    public static void StopAutoUpdate()
    {
        LoggerFactory.LogInformation("Arrêt de la mise a jour automatique");
        //Arrêt de la maj AUTO 
        if (Properties.Settings.Default.TranslationUpdateMethod == nameof(TranslationUpdateMethodEnum.Never) ||
            Properties.Settings.Default.TranslationUpdateMethod == nameof(TranslationUpdateMethodEnum.StartRsiLauncher))
            //Si la maj n'est pas périodique rien ne ce passe
            return;
        else if (Properties.Settings.Default.TranslationUpdateMethod == nameof(TranslationUpdateMethodEnum.EveryTwoHours) ||
                Properties.Settings.Default.TranslationUpdateMethod == nameof(TranslationUpdateMethodEnum.EverySixHours) ||
                Properties.Settings.Default.TranslationUpdateMethod == nameof(TranslationUpdateMethodEnum.EveryTwelveHours))
        {
            //Maj périodique donc arrêt du timer
            StopTimer();
        }
        else return;
    }

    private static void StartTimer()
    {
        //Configure le temps du timer en fonction de la périodicité de la maj
        switch (Properties.Settings.Default.TranslationUpdateMethod)
        {
            case nameof(TranslationUpdateMethodEnum.EveryTwoHours):
                //Toute les 2 heurs
                timer.Interval = (int)TimeSpan.FromHours(2).TotalMilliseconds;
                break;
            case nameof(TranslationUpdateMethodEnum.EverySixHours):
                //Toutes les 6 heurs
                timer.Interval = (int)TimeSpan.FromHours(6).TotalMilliseconds;
                break;
            case nameof(TranslationUpdateMethodEnum.EveryTwelveHours):
                //Toutes les 12 heurs
                timer.Interval = (int)TimeSpan.FromHours(12).TotalMilliseconds;
                break;
        }

        //A chaque itération du timer lancé Timer_Tick
        timer.Tick += Timer_Tick;
        timer.Start();
    }

    private static void Timer_Tick(object sender, EventArgs e)
    {
        //Lancement d'une maj
        TranslationInstaller.Run();
    }

    private static void StopTimer()
    {
        timer.Stop();
        timer.Dispose();
        timer.Tick -= Timer_Tick;
    }

}
