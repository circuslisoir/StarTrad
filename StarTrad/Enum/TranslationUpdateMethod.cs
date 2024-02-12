using System.ComponentModel;

namespace StarTrad.Enum
{
    public enum TranslationUpdateMethod
    {
        [Description("Jamais")]
        Never,

        [Description("Au lancement du launcher RSI")]
        StartRsiLauncher,

        [Description("Toutes les 6h")]
        EverySixHours,

        [Description("Toutes les 12h")]
        EveryTwelveHours,

        [Description("Toutes les 24h")]
        EveryTwentyFourHours,

        [Description("Toutes les 48h")]
        EveryFourtyEightHours,

        [Description("Toutes les semaines")]
        EverySevenDays
    }
}