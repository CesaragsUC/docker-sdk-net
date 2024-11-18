using Docker.DotNet.Models;

namespace Client.Containers;

internal class ContainerInfo
{
    public string? Id { get; set; }
    public string? Image { get; set; }
    public string? Tag { get; set; }
    public string? Name { get; set; }
    public ContainerStatus? Status { get; set; }
    public Dictionary<string, EmptyStruct>? Ports  { get; set; }
    public Dictionary<string, IList<PortBinding>>? PortBindings { get; set; }
}
