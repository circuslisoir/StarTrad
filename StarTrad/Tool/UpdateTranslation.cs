using StarTrad.Helper.ComboxList;


namespace StarTrad.Tool;
internal static class UpdateTranslation
{
    private static System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();

    public static void StartAutoUpdate()
    {
        if (Properties.Settings.Default.TranslationUpdateMethod == nameof(TranslationUpdateMethodEnum.Never) ||
            Properties.Settings.Default.TranslationUpdateMethod == nameof(TranslationUpdateMethodEnum.StartRsiLauncher))
            return;
        else if (Properties.Settings.Default.TranslationUpdateMethod == nameof(TranslationUpdateMethodEnum.EveryTwelveHours) ||
                Properties.Settings.Default.TranslationUpdateMethod == nameof(TranslationUpdateMethodEnum.EveryTwelveHours) ||
                Properties.Settings.Default.TranslationUpdateMethod == nameof(TranslationUpdateMethodEnum.EveryTwelveHours))
        {

            StartTimer();
        }
        else return;

    }

    public static void StopAutoUpdate()
    {
        if (Properties.Settings.Default.TranslationUpdateMethod == nameof(TranslationUpdateMethodEnum.Never) ||
            Properties.Settings.Default.TranslationUpdateMethod == nameof(TranslationUpdateMethodEnum.StartRsiLauncher))
            return;
        else if (Properties.Settings.Default.TranslationUpdateMethod == nameof(TranslationUpdateMethodEnum.EveryTwelveHours) ||
                Properties.Settings.Default.TranslationUpdateMethod == nameof(TranslationUpdateMethodEnum.EveryTwelveHours) ||
                Properties.Settings.Default.TranslationUpdateMethod == nameof(TranslationUpdateMethodEnum.EveryTwelveHours))
        {
            StopTimer();
        }
        else return;
    }

    private static void StartTimer()
    {


        switch (Properties.Settings.Default.TranslationUpdateMethod)
        {
            case nameof(TranslationUpdateMethodEnum.EveryTwoHours):
                timer.Interval = (int)TimeSpan.FromHours(2).TotalMilliseconds;
                break;
            case nameof(TranslationUpdateMethodEnum.EverySixHours):
                timer.Interval = (int)TimeSpan.FromHours(6).TotalMilliseconds;
                break;
            case nameof(TranslationUpdateMethodEnum.EveryTwelveHours):
                timer.Interval = (int)TimeSpan.FromHours(12).TotalMilliseconds;
                break;
        }

        timer.Tick += Timer_Tick;
        timer.Start();
    }

    private static void Timer_Tick(object sender, EventArgs e)
    {
        TranslationInstaller.Run();
    }

    private static void StopTimer()
    {
        timer.Stop();
        timer.Dispose();
        timer.Tick -= Timer_Tick;
        timer.Interval = -1;
    }

}
