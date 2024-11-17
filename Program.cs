using Docker.DotNet;
using Docker.DotNet.Models;

class Program
{
    static async Task Main(string[] args)
    {
        // Configure the Docker client
        DockerClient client = new DockerClientConfiguration()
            .CreateClient();

        // Create the NGINX container
        await CreateNginxContainer(client);

        // Dispose the client
        client.Dispose();

        System.Console.ReadKey();
    }

    private static async Task CreateNginxContainer(DockerClient client)
    {
        // Download the NGINX image if it doesn't exist
        Console.WriteLine("Downloading NGINX image...");
        await client.Images.CreateImageAsync(
            new ImagesCreateParameters
            {
                FromImage = "nginx",
                Tag = "latest"
            },
            null,
            new Progress<JSONMessage>(m => Console.WriteLine(m.Status))
        );

        // Create the container with basic configuration
        Console.WriteLine("Creating NGINX container...");
        var response = await client.Containers.CreateContainerAsync(new CreateContainerParameters
        {
            Image = "nginx:latest",
            Name = "my-nginx",
            ExposedPorts = new Dictionary<string, EmptyStruct>
            {
                { "80/tcp", default }
            },
            HostConfig = new HostConfig
            {
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
            }
        });

        Console.WriteLine($"Container created with ID: {response.ID}");

        // Start the container
        Console.WriteLine("Starting container...");
        await client.Containers.StartContainerAsync(response.ID, null);
        Console.WriteLine("NGINX container started successfully.");
    }

    private static async Task StartRabbitMQContainerAsync(DockerClient client)
    {
        string containerName = "my-rabbitmq"; // Container name
        string imageName = "rabbitmq";
        string imageTag = "management"; // RabbitMQ with management panel enabled

        try
        {
            // Check if the container already exists
            var existingContainers = await client.Containers.ListContainersAsync(new ContainersListParameters
            {
                All = true
            });

            var existingContainer = existingContainers
                .FirstOrDefault(c => c.Image == $"{imageName}:{imageTag}" && c.Names.Contains($"/{containerName}"));

            if (existingContainer != null)
            {
                Console.WriteLine($"Container {containerName} already exists. Starting...");
                await client.Containers.StartContainerAsync(existingContainer.ID, new ContainerStartParameters());
            }
            else
            {
                Console.WriteLine($"Creating and starting container {containerName}...");

                // Create the RabbitMQ container
                var response = await client.Containers.CreateContainerAsync(new CreateContainerParameters
                {
                    Image = $"{imageName}:{imageTag}",
                    Name = containerName,
                    ExposedPorts = new Dictionary<string, EmptyStruct>
                    {
                        { "15672/tcp", default }, // RabbitMQ management panel
                        { "5672/tcp", default }   // AMQP connection
                    },
                    HostConfig = new HostConfig
                    {
                        PortBindings = new Dictionary<string, IList<PortBinding>>
                        {
                            {
                                "15672/tcp",
                                new List<PortBinding>
                                {
                                    new PortBinding { HostPort = "15672" } // Management panel port on host
                                }
                            },
                            {
                                "5672/tcp",
                                new List<PortBinding>
                                {
                                    new PortBinding { HostPort = "5672" } // AMQP port on host
                                }
                            }
                        }
                    }
                });

                Console.WriteLine($"Container {containerName} created with ID: {response.ID}");

                // Start the container
                await client.Containers.StartContainerAsync(response.ID, new ContainerStartParameters());
                Console.WriteLine($"Container {containerName} started successfully.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting container {containerName}: {ex.Message}");
        }
    }

    private static async Task StartDotNetAppContainerAsync(DockerClient client)
    {
        string containerName = "my-dotnet-app"; // Container name
        string imageName = "cesarags/gerador-caos-net"; // Your Docker image name
        string imageTag = "v2"; // Your image tag

        try
        {
            // Check if the container already exists
            var existingContainers = await client.Containers.ListContainersAsync(new ContainersListParameters
            {
                All = true
            });

            var existingContainer = existingContainers
                .FirstOrDefault(c => c.Image == $"{imageName}:{imageTag}" && c.Names.Contains($"/{containerName}"));

            if (existingContainer != null)
            {
                Console.WriteLine($"Container {containerName} already exists. Starting...");
                await client.Containers.StartContainerAsync(existingContainer.ID, new ContainerStartParameters());
            }
            else
            {
                Console.WriteLine($"Creating and starting container {containerName}...");

                // Create the .NET application container
                var response = await client.Containers.CreateContainerAsync(new CreateContainerParameters
                {
                    Image = $"{imageName}:{imageTag}",
                    Name = containerName,
                    ExposedPorts = new Dictionary<string, EmptyStruct>
                    {
                        { "5007/tcp", default } // .NET app port in the container
                    },
                    HostConfig = new HostConfig
                    {
                        PortBindings = new Dictionary<string, IList<PortBinding>>
                        {
                            {
                                "5007/tcp",
                                new List<PortBinding>
                                {
                                    new PortBinding { HostPort = "5007" } // Mapped port on host
                                }
                            }
                        }
                    }
                });

                Console.WriteLine($"Container {containerName} created with ID: {response.ID}");

                // Start the container
                await client.Containers.StartContainerAsync(response.ID, new ContainerStartParameters());
                Console.WriteLine($"Container {containerName} started successfully.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting container {containerName}: {ex.Message}");
        }
    }
}