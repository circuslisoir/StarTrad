using System.ComponentModel;

namespace StarTrad.Helper.ComboxList;

public enum ChanelVersionEnum
{
    [Description("Persistant Univers")]
    LIVE,

    [Description("Public Test Universe")]
    PTU,

    [Description("Evocati Public Test Universe")]
    EPTU,

    [Description("TECH-PREVIEW")]
    TECH_PREVIEW
}