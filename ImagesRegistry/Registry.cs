using Client.Containers;
using Client.Services;
using Docker.DotNet;
using Docker.DotNet.Models;
using Serilog;

namespace Client.ImagesRegistry;

internal static class Registry
{
    private static readonly ILogger Logger = new LoggerConfiguration()
        .MinimumLevel.Debug()
        .WriteTo.Console()
        .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
        .CreateLogger();

    public static async Task<ContainerInfo> Nginx()
    {
        // You can retrive this info from a database or a configuration file
        var container = new ContainerInfo
        {
            Image = "nginx",
            Tag = "latest",
            Name = "nginx-demo",
            Ports = new Dictionary<string, EmptyStruct>
            {
                { "80/tcp", default }
            },
            PortBindings = new Dictionary<string, IList<PortBinding>>
            {
                {
                    "80/tcp",
                    new List<PortBinding>
                    {
                        new PortBinding { HostPort = "8080" }
                    }
                }
            }
        };

        try
        {
            var response = await SdkServices.RunContainerAsync(container);
            return response;
        }
        catch (Exception ex)
        {
            Logger.Fatal(ex, "An error occour during create and run container {Container} - {Error}!", container.Name, ex.Message);
            return null!;
        }

    }

    public static async Task<ContainerInfo> RabbitMQ()
    {

        var container = new ContainerInfo
        {
            Image = "rabbitmq",
            Tag = "management",
            Name = "my-rabbitmq",
            Ports = new Dictionary<string, EmptyStruct>
            {
               { "15672/tcp", default }, // RabbitMQ management panel
               { "5672/tcp", default }   // AMQP connection
            },
            PortBindings = new Dictionary<string, IList<PortBinding>>
            {
               {
                   "15672/tcp",
                   new List<PortBinding>
                   {
                       new PortBinding { HostPort = "15672" }
                   }
               },
               {
                   "5672/tcp",
                   new List<PortBinding>
                   {
                       new PortBinding { HostPort = "5672" }
                   }
               }
            }
        };

        try
        {
            var response = await SdkServices.RunContainerAsync(container);
            return response;
        }
        catch (Exception ex)
        {
            Logger.Fatal(ex, "An error occour during create and run container {Container} - {Error}!", container.Name, ex.Message);
            return null!;
        }
    }

    public static async Task<ContainerInfo> GeradorCaosNet()
    {
        var container = new ContainerInfo
        {
            Image = "cesarags/gerador-caos-net",
            Tag = "v3",
            Name = "gerador-caos-net",
            Ports = new Dictionary<string, EmptyStruct>
            {
                { "8080/tcp", default }
            },
            PortBindings = new Dictionary<string, IList<PortBinding>>
            {
                {
                    "8080/tcp",
                    new List<PortBinding>
                    {
                        new PortBinding { HostPort = "5284" }
                    }
                }
            }
        };

        try
        {
            var response = await SdkServices.RunContainerAsync(container);
            return response;
        }
        catch (Exception ex)
        {
            Logger.Fatal(ex, "An error occour during create and run container {Container} - {Error}!", container.Name, ex.Message);
            return null!;
        }
    }

}
