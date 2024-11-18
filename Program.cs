using Client.Containers;
using Client.ImagesRegistry;
using Client.Services;
using Docker.DotNet;
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

        // Configure the Docker client
        DockerClient client = new DockerClientConfiguration()
            .CreateClient();

        // Create and start the containers
        var nginx = await Registry.Nginx(client); // http://localhost:8080
        await Registry.RabbitMQ(client); // http://localhost:15672
        await Registry.GeradorCaosNet(client); // http://localhost:5284/

        //// uncomment to test the stop container
        //await SdkServices.StopContainerAsync(nginx.Id!, nginx.Name!);

        //// uncomment to test the delete container
        //await SdkServices.DeleteContainerAsync(nginx.Id!);


        //// uncomment to test the build image and push image to docker hub
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


        // Dispose the client
        client.Dispose();

        Console.ReadKey();
    }

}