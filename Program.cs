using Client.Containers;
using Client.ImagesRegistry;
using Client.Services;
using Serilog;

class Program
{

    static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        Log.Information("Wellcome to Docker SDK .NET by Cesarags");
        Console.WriteLine("");

        // Create and start the containers
        var nginx = await Registry.Nginx();
        await Registry.RabbitMQ();
        await Registry.GeradorCaosNet();

        //// uncomment to test the stop container
        await SdkServices.StopContainerAsync(nginx.Id!);

        //////// uncomment to test the delete container
        await SdkServices.DeleteContainerAsync(nginx.Id!);


        ////// uncomment to test the build image and push image to docker hub
        string dockerFile = "./Dockerfile";
        string imageName = "cesarags/my-image-test";
        string tag = "latest";
        await SdkServices.BuildDockerImage(dockerFile, imageName, tag);

        var containerInfo = new ContainerInfo
        {
            Image = imageName,
            Tag = tag
        };
        await SdkServices.PushDockerImage(containerInfo);

        Log.Information("All good.");
        Console.ReadKey();
    }

}