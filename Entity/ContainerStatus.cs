using System.ComponentModel;

namespace Client.Containers;

public enum ContainerStatus
{
    [Description("created")]
    Created = 1,

    [Description("running")]
    Running = 2,

    [Description("exited")]
    Exited = 0,

    [Description("created")]
    Removing = 3,

    [Description("paused")]
    Paused= 4,

    [Description("restarting")]
    Restarting = 5,

    [Description("dead")]
    Dead = 6
}
