using System.ComponentModel;

namespace StarTrad.Helper.ComboxList
{
	public enum TranslationUpdateMethodEnum
    {
        [Description("Au lancement du launcher RSI")]
        StartRsiLauncher,

        [Description("Toutes les 2h")]
        EveryTwoHours,

        [Description("Toutes les 6h")]
        EverySixHours,

        [Description("Toutes les 12h")]
        EveryTwelveHours,

        [Description("Jamais")]
        Never
    }
}
