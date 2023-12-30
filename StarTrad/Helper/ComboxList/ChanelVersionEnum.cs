using System.ComponentModel;

namespace StarTrad.Helper.ComboxList;

public enum ChanelVersionEnum
{
    [Description("LIVE")]
    LIVE,

    [Description("PTU")]
    PTU,

    [Description("EPTU")]
    EPTU,

    [Description("TECH-PREVIEW")]
    TECH_PREVIEW
}