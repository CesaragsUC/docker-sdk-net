using Client.Configuration;
using Client.Containers;
using Docker.DotNet;
using Docker.DotNet.Models;
using ICSharpCode.SharpZipLib.Tar;
using Microsoft.Extensions.Configuration;
using Serilog;
using System.Text;
using ContainerStatus = Client.Containers.ContainerStatus;

namespace Client.Services;

internal static class SdkServices
{

    private static readonly ILogger Logger = new LoggerConfiguration()
        .MinimumLevel.Debug()
        .WriteTo.Console()
        .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
        .CreateLogger();

    private static readonly DockerClient _client = new DockerClientConfiguration().CreateClient();

    private static readonly DockerHubSetting _dockerHubSetting = new DockerHubSetting();

    static SdkServices()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddUserSecrets<Program>()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        configuration.GetSection("DockerHubSetting").Bind(_dockerHubSetting);

    }



    public static async Task DownloadImage(ContainerInfo container)
    {
        try
        {
            if (!await IsImageAlreadyExists(container))
            {
                Logger.Debug("Downloading image {Image}...", container.Image);

                await _client.Images.CreateImageAsync(
                    new ImagesCreateParameters
                    {
                        FromImage = container.Image,
                        Tag = container.Tag
                    },
                    null,
                    new Progress<JSONMessage>(m => Console.WriteLine(m.Status))
                );

                Logger.Information("Image {Image} downloaded.", container.Image);
            }
        }
        catch (Exception ex)
        {
            Logger.Fatal(ex, "Error during download image: {Message}!", ex.Message);
        }

    }

    public static async Task<bool> IsContainerRunning(ContainerInfo container)
    {
        try
        {
            // Get all containers running
            var existingContainers = await _client.Containers.ListContainersAsync(new ContainersListParameters
            {
                All = false //False only running containers, True all containers
            });

            // Check if the container already exists and is running
            var existingContainer = existingContainers
            .FirstOrDefault(c => c.Image == $"{container.Image}:{container.Tag}" && c.Names.Contains($"/{container.Name}"));

            if (existingContainer == null)
            {
                Logger.Warning("Container {Message} its stopped or do not exist ", container.Name);
                return false;
            }

            Logger.Warning("Container {Message} already running.", container.Name);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Fatal(ex, "Error during check container status: {Message}!", ex.Message);

            return false;
        }

    }


    public static async Task<ContainerInfo> RunContainerAsync(ContainerInfo container)
    {
        try
        {
            if (!await DockerIsContainerExist(container))
            {
                container = await DockerCreateAndRunContainerAsync(container);
            }

            if (!await IsContainerRunning(container))
            {
                container = await DockerGetContainerStoped(container);
                await RunExistentContainerAsync(container);
            }
            else
            {
                container = await DockerGetContainerInfo(container);
            }

            return container;
        }
        catch (Exception ex)
        {
            Logger.Fatal(ex, "Error during create and run container: {Message}!", ex.Message);

            return null!;
        }

    }

    public static async Task<ContainerInfo> DockerCreateAndRunContainerAsync(ContainerInfo container)
    {
        try
        {
            Logger.Information("Creating {Message} container...", container.Name);

            var response = await _client.Containers.CreateContainerAsync(new CreateContainerParameters
            {
                Image = $"{container.Image}:{container.Tag}",
                Name = container.Name,
                ExposedPorts = container.Ports,
                HostConfig = new HostConfig
                {
                    PortBindings = container.PortBindings
                }
            });

            container.Id = response.ID;
            container.Status = ContainerStatus.Created;

            Logger.Information("Container {Message} successfully created.", container.Name);
            Console.WriteLine("");

            Logger.Debug("Starting...");
            await _client.Containers.StartContainerAsync(response.ID, new ContainerStartParameters());
            Logger.Information("Container {Message} its running...", container.Name);

            return container;
        }
        catch (Exception ex)
        {
            Logger.Fatal(ex, "Error during create and run container: {Message}!", ex.Message);

            return null!;
        }

    }

    public static async Task<ContainerInfo> RunExistentContainerAsync(ContainerInfo container)
    {

        try
        {
            Logger.Information("Starting {Message} container...", container.Name);

            await _client.Containers.StartContainerAsync(container.Id, new ContainerStartParameters());

            Logger.Information("Container {Message} started successfully.", container.Name);

            return container;
        }
        catch (Exception ex)
        {
            Logger.Fatal(ex, "Error during start container: {Message}!", ex.Message);

            return null!;
        }
    }
    public static async Task<ContainerInfo> DockerGetContainerInfo(ContainerInfo container)
    {
        try
        {
            var containers = await _client.Containers.ListContainersAsync(new ContainersListParameters
            {
                All = false
            });

            var existingContainer = containers
            .FirstOrDefault(c => c.Image == $"{container.Image}:{container.Tag}" && c.Names.Contains($"/{container.Name}"));

            container.Id = existingContainer?.ID;
            container.Status = existingContainer != null ? MapContainerState(existingContainer.State) : ContainerStatus.Dead;

            return container;
        }
        catch (Exception ex)
        {
            Logger.Fatal(ex, "Error during check container info: {Message}!", ex.Message);
            return null!;
        }

    }

    public static async Task DockerAllContainerAlreadyRunning()
    {
        try
        {
            var containers = await _client.Containers.ListContainersAsync(new ContainersListParameters
            {
                All = true
            });

            foreach (var container in containers)
            {
                Logger.Information("Container {ContainerId} is running.", container.ID);
            }
        }
        catch (Exception ex)
        {
            Logger.Fatal(ex, "Error during check container status: {Message}!", ex.Message);
        }
    }

    public static async Task<ContainerInfo> DockerContainerAlreadyRunning(ContainerInfo container)
    {
        try
        {
            var containers = await _client.Containers.ListContainersAsync(new ContainersListParameters
            {
                All = false
            });

            var existingContainer = containers
            .FirstOrDefault(c => c.Image == $"{container.Image}:{container.Tag}" && c.Names.Contains($"/{container.Name}"));

            container.Id = existingContainer?.ID;
            container.Status = existingContainer != null ? MapContainerState(existingContainer.State) : ContainerStatus.Dead;

            return container;
        }
        catch (Exception ex)
        {
            Logger.Fatal(ex, "Error during check container status: {Message}!", ex.Message);
            return null!;
        }
    }

    public static async Task<ContainerInfo> DockerGetContainerStoped(ContainerInfo container)
    {
        try
        {
            var containers = await _client.Containers.ListContainersAsync(new ContainersListParameters
            {
                All = true
            });

            var existingContainer = containers
            .FirstOrDefault(c => c.Image == $"{container.Image}:{container.Tag}" && c.Names.Contains($"/{container.Name}"));

            container.Id = existingContainer?.ID;
            container.Status = existingContainer != null ? MapContainerState(existingContainer.State) : ContainerStatus.Dead;

            return container;
        }
        catch (Exception ex)
        {
            Logger.Fatal(ex, "Error during check container status: {Message}!", ex.Message);
            return null!;
        }
    }

    public static async Task<bool> DockerIsContainerExist(ContainerInfo container)
    {
        try
        {
            var containers = await _client.Containers.ListContainersAsync(new ContainersListParameters
            {
                All = true
            });

            var existingContainer = containers
            .FirstOrDefault(c => c.Image == $"{container.Image}:{container.Tag}" && c.Names.Contains($"/{container.Name}"));

            return existingContainer != null;
        }
        catch (Exception ex)
        {
            Logger.Fatal(ex, "Error during check container status: {Message}!", ex.Message);
            return false;
        }
    }

    public static async Task<bool> IsImageAlreadyExists(ContainerInfo container)
    {
        try
        {
            var images = await _client.Images.ListImagesAsync(new ImagesListParameters
            {
                All = true
            });

            var imageWithTag = $"{container.Image}:{container.Tag}";

            var existingImage = images.FirstOrDefault(i => i.RepoTags.Contains(imageWithTag));

            if (existingImage != null)
            {
                Logger.Information("Image {Image} already exists.", imageWithTag);
                return true;
            }
            else
            {
                Logger.Information("Image {Image} does not exist.", imageWithTag);
                return false;
            }
        }
        catch (Exception ex)
        {
            Logger.Fatal(ex, "Error during check image status: {Message}!", ex.Message);
            return false;
        }
    }



    public static async Task PushDockerImage(ContainerInfo container)
    {
        try
        {
            var authConfig = new AuthConfig
            {
                Username = _dockerHubSetting.Login,
                Password = _dockerHubSetting.Password,
                ServerAddress = _dockerHubSetting.ServerAddress,
            };

            var pushParams = new ImagePushParameters
            {
                ImageID = container.Image,
                Tag = container.Tag
            };

            var progress = new Progress<JSONMessage>(message =>
            {
                if (!string.IsNullOrEmpty(message.Status))
                {
                    Console.WriteLine(message.Status);
                }
                if (message.Error != null)
                {
                    Logger.Fatal("Push Failed: : {ErrorMessage}", message.ErrorMessage);
                }
            });


            Logger.Debug("Starting image push...");

            await _client.Images.PushImageAsync(
                container.Image,
                pushParams,
                authConfig,
                progress,
                CancellationToken.None);

            Logger.Information("Image {Image} pushed successfully!", container.Image);
        }
        catch (Exception ex)
        {
            Logger.Fatal(ex, "Error during pushing image: {Message}!", ex.Message);
        }

    }

    public static async Task BuildDockerImage(string dockerfilePath, string image, string tag = "")
    {
        try
        {

            string contextPath = AppContext.BaseDirectory;
            string dockerFilePath = Path.Combine(contextPath, "Dockerfile");

            if (!File.Exists(dockerFilePath))
            {
                Logger.Fatal("Dockerfile not found at {Path}", dockerFilePath);
                throw new FileNotFoundException($"Dockerfile not found at {dockerFilePath}");
            }

            using (var tarStream = CreateTarballForDockerfile(contextPath))
            {
                var buildParams = new ImageBuildParameters
                {
                    Dockerfile = Path.GetFileName(dockerfilePath),
                    Tags = new List<string> { $"{image}:{tag}" }
                };

                await _client.Images.BuildImageFromDockerfileAsync(
                    buildParams,
                    tarStream, null, null, new Progress<JSONMessage>(), CancellationToken.None);

                Logger.Information("Build successfully");
                Logger.Information("Image {Message} create successfully", image);
            }
        }
        catch (Exception ex)
        {
            Logger.Fatal(ex, "Error during build image: {Message}!", ex.Message);
        }

    }

    public static Stream CreateTarballForDockerfile(string contextPath)
    {
        try
        {
            Logger.Information("Creating Tar Ball...");

            var tarStream = new MemoryStream();
            using (var tar = new TarOutputStream(tarStream, Encoding.UTF8))
            {
                tar.IsStreamOwner = false; // Não feche o stream principal

                // Inclua o Dockerfile no tarball
                var dockerfilePath = Path.Combine(contextPath, "Dockerfile");
                if (!File.Exists(dockerfilePath))
                {
                    throw new FileNotFoundException($"Dockerfile not found at {dockerfilePath}");
                }

                AddFileToTar(tar, dockerfilePath, "Dockerfile");

                // Adicione outros arquivos necessários ao contexto do build
                foreach (var file in Directory.GetFiles(contextPath))
                {
                    if (Path.GetFileName(file) != "Dockerfile") // Evite duplicar o Dockerfile
                    {
                        AddFileToTar(tar, file, Path.GetFileName(file));
                    }
                }
            }

            tarStream.Seek(0, SeekOrigin.Begin);
            Logger.Information("Tar Ball created successfully!");
            return tarStream;
        }
        catch (Exception ex)
        {
            Logger.Fatal(ex, "Error during Create Tarball: {Message}!", ex.Message);
            return null!;
        }
    }

    private static void AddFileToTar(TarOutputStream tar, string filePath, string entryName)
    {
        var fileInfo = new FileInfo(filePath);
        var entry = new ICSharpCode.SharpZipLib.Tar.TarEntry(new TarHeader
        {
            Name = entryName,
            Size = fileInfo.Length,
            Mode = Convert.ToInt32("644", 8),
        });
        tar.PutNextEntry(entry);

        using (var fileStream = File.OpenRead(filePath))
        {
            fileStream.CopyTo(tar);
        }

        tar.CloseEntry();
    }


    public static async Task StartContainerAsync(ContainerInfo container)
    {
        try
        {
            await DownloadImage(container);
            await RunContainerAsync(container);
        }
        catch (Exception ex)
        {
            Logger.Fatal(ex, "Error: {Message}", ex.Message);
        }
    }

    public static async Task StopContainerAsync(string containerId, string name = "")
    {
        try
        {
            // Attempts to stop the container
            bool stopped = await _client.Containers.StopContainerAsync(
                containerId,
                new ContainerStopParameters() // Does not include Timeout here
            );

            if (stopped)
            {
                Logger.Information("Container {Name} - {ContainerId} stopped successfully.", name, containerId);
            }
            else
            {
                Logger.Information("Failed to stop container {Name} - {ContainerId}. It might already be stopped.", name, containerId);
            }
        }
        catch (Exception ex)
        {
            Logger.Fatal(ex, "Error stopping container: {Message}", ex.Message);
        }
    }

    public static async Task DeleteContainerAsync(string containerId)
    {
        try
        {
            await StopContainerAsync(containerId); // Stops the container before deleting it (if it is running

            await _client.Containers.RemoveContainerAsync(containerId, new ContainerRemoveParameters
            {
                Force = true // Removes the container even if it is running
            });

            Logger.Information("Container Id: {ContainerId} deleted successfully.", containerId);
        }
        catch (Exception ex)
        {
            Logger.Fatal(ex, "Error deleting container {ContainerId}", containerId);
        }
    }

    private static ContainerStatus MapContainerState(string state)
    {
        if (Enum.TryParse<ContainerStatus>(state, true, out var status))
        {
            return status;
        }
        return ContainerStatus.Dead; // Retorna Dead como fallback
    }
}
