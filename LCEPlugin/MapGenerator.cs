using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LCEPlugin
{
    internal class MapGenerator : ServerPlugin
    {
        #region Plugin Metadata

        /// <summary>
        /// Gets the name of the plugin.
        /// </summary>
        public string GetName() => "MapGenerator";

        /// <summary>
        /// Gets the current version of the plugin.
        /// </summary>
        public string GetVersion() => "1.0.0";

        /// <summary>
        /// Gets the author of the plugin.
        /// </summary>
        public string GetAuthor() => "effery";

        #endregion

        #region Lifecycle Methods

        /// <summary>
        /// Called when the plugin is enabled.
        /// Registers the tree listener for block break events.
        /// </summary>
        public void OnEnable()
        {
            //FourKit.addListener();
            FourKit.getCommand("startmap").setExecutor(new startCommand());
            FourKit.getCommand("stopmap").setExecutor(new stopCommand());
        }

        /// <summary>
        /// Called when the plugin is disabled.
        /// Performs cleanup operations.
        /// </summary>
        public void OnDisable()
        {
        }

        #endregion
    }

    public class startCommand : CommandExecutor
    {
        public bool onCommand(CommandSender sender, global::Command command, string label, string[] args)
        {
            if (!(sender is Player))
            {
                sender.sendMessage("This command can only be used by players.");
                return true;
            }

            Player player = (Player)sender;

            if (MapGenerationManager.Instance.IsGenerating(player))
            {
                player.sendMessage("Map generation already in progress!");
                return true;
            }

            int radius = (1024*5);
            int stepSize = 50;
            int delay = 1000;

            if (args.Length > 0 && int.TryParse(args[0], out int customRadius))
            {
                radius = customRadius;
            }

            if (args.Length > 1 && int.TryParse(args[1], out int customStep))
            {
                stepSize = customStep;
            }

            if (args.Length > 2 && int.TryParse(args[2], out int customDelay))
            {
                delay = customDelay;
            }

            player.sendMessage(string.Format("Starting map generation - Radius: {0}, Step: {1}, Delay: {2}ms", radius, stepSize, delay));
            MapGenerationManager.Instance.StartGeneration(player, radius, stepSize, delay);

            return true;
        }
    }

    public class stopCommand : CommandExecutor
    {
        public bool onCommand(CommandSender sender, global::Command command, string label, string[] args)
        {
            if (!(sender is Player))
            {
                sender.sendMessage("This command can only be used by players.");
                return true;
            }

            Player player = (Player)sender;

            if (!MapGenerationManager.Instance.IsGenerating(player))
            {
                player.sendMessage("No map generation in progress.");
                return true;
            }

            MapGenerationManager.Instance.StopGeneration(player);
            player.sendMessage("Map generation stopped.");

            return true;
        }
    }

    /// <summary>
    /// Manages map generation tasks for multiple players.
    /// </summary>
    public class MapGenerationManager
    {
        private static MapGenerationManager _instance;
        public static MapGenerationManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new MapGenerationManager();
                }
                return _instance;
            }
        }

        private Dictionary<Player, MapGenerationTask> activeTasks = new Dictionary<Player, MapGenerationTask>();

        public bool IsGenerating(Player player)
        {
            return activeTasks.ContainsKey(player);
        }

        public void StartGeneration(Player player, int worldSize, int stepSize, int delay)
        {
            if (activeTasks.ContainsKey(player))
            {
                return;
            }

            MapGenerationTask task = new MapGenerationTask(player, worldSize, stepSize, delay);
            activeTasks[player] = task;
            task.Start(() => activeTasks.Remove(player));
        }

        public void StopGeneration(Player player)
        {
            if (activeTasks.TryGetValue(player, out MapGenerationTask task))
            {
                task.Stop();
                activeTasks.Remove(player);
            }
        }
    }

    /// <summary>
    /// Represents a map generation task that teleports a player in a grid pattern across the entire world.
    /// </summary>
    public class MapGenerationTask
    {
        private Player player;
        private int worldSize;
        private int stepSize;
        private int delay;
        private bool isRunning;
        private double originalX;
        private double originalY;
        private double originalZ;
        private bool hasOriginalLocation;
        private Task generationTask;
        private CancellationTokenSource cancellationTokenSource;

        public MapGenerationTask(Player player, int worldSize, int stepSize, int delay)
        {
            this.player = player;
            this.worldSize = worldSize;
            this.stepSize = stepSize;
            this.delay = delay;
            this.isRunning = false;

            try
            {
                this.originalX = player.getX();
                this.originalY = player.getY();
                this.originalZ = player.getZ();
                this.hasOriginalLocation = true;
            }
            catch
            {
                this.hasOriginalLocation = false;
            }
        }

        public void Start(Action onComplete)
        {
            if (isRunning)
            {
                return;
            }

            isRunning = true;
            cancellationTokenSource = new CancellationTokenSource();

            generationTask = Task.Run(() =>
            {
                try
                {
                    GenerateMap(cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    player.sendMessage("Map generation cancelled.");
                }
                catch (Exception ex)
                {
                    player.sendMessage("Error during map generation: " + ex.Message);
                }
                finally
                {
                    isRunning = false;
                    ReturnToOriginalLocation();
                    onComplete?.Invoke();
                }
            });
        }

        public void Stop()
        {
            if (!isRunning)
            {
                return;
            }

            cancellationTokenSource?.Cancel();
        }

        private void GenerateMap(CancellationToken token)
        {
            if (!hasOriginalLocation)
            {
                player.sendMessage("Could not get original location!");
                return;
            }

            const int MAP_SIZE = 1024;
            int halfWorld = worldSize / 2;
            double yLevel = originalY;

            int mapsPerSide = worldSize / MAP_SIZE;
            int totalMaps = mapsPerSide * mapsPerSide;
            int completedMaps = 0;
            int totalSteps = 0;
            int completedSteps = 0;

            // Calculate total steps across all maps
            for (int mapX = 0; mapX < mapsPerSide; mapX++)
            {
                for (int mapZ = 0; mapZ < mapsPerSide; mapZ++)
                {
                    int mapStartX = -halfWorld + (mapX * MAP_SIZE);
                    int mapStartZ = -halfWorld + (mapZ * MAP_SIZE);
                    int mapEndX = mapStartX + MAP_SIZE;
                    int mapEndZ = mapStartZ + MAP_SIZE;

                    for (int x = mapStartX; x < mapEndX; x += stepSize)
                    {
                        for (int z = mapStartZ; z < mapEndZ; z += stepSize)
                        {
                            totalSteps++;
                        }
                    }
                }
            }

            player.sendMessage(string.Format("Generating {0}x{0} world ({1} maps of {2}x{2}), {3} teleport points...", worldSize, totalMaps, MAP_SIZE, totalSteps));

            // Process each map one at a time
            for (int mapX = 0; mapX < mapsPerSide; mapX++)
            {
                for (int mapZ = 0; mapZ < mapsPerSide; mapZ++)
                {
                    if (token.IsCancellationRequested)
                    {
                        throw new OperationCanceledException();
                    }

                    int mapStartX = -halfWorld + (mapX * MAP_SIZE);
                    int mapStartZ = -halfWorld + (mapZ * MAP_SIZE);
                    int mapEndX = mapStartX + MAP_SIZE;
                    int mapEndZ = mapStartZ + MAP_SIZE;

                    player.sendMessage(string.Format("Starting map {0}/{1} - Region ({2}, {3}) to ({4}, {5})", 
                        completedMaps + 1, totalMaps, mapStartX, mapStartZ, mapEndX - 1, mapEndZ - 1));

                    // Generate all points within this map
                    for (int x = mapStartX; x < mapEndX; x += stepSize)
                    {
                        for (int z = mapStartZ; z < mapEndZ; z += stepSize)
                        {
                            if (token.IsCancellationRequested)
                            {
                                throw new OperationCanceledException();
                            }

                            try
                            {
                                player.teleport(x, yLevel, z);

                                completedSteps++;

                                if (completedSteps % 10 == 0)
                                {
                                    int progress = (completedSteps * 100) / totalSteps;
                                    player.sendMessage(string.Format("Progress: {0}% ({1}/{2}) - Map {3}/{4} - Position: ({5}, {6})", 
                                        progress, completedSteps, totalSteps, completedMaps + 1, totalMaps, x, z));
                                }
                            }
                            catch
                            {
                                // Continue on teleport failure
                            }

                            InterruptibleDelay(delay, token);
                        }
                    }

                    completedMaps++;
                    player.sendMessage(string.Format("Completed map {0}/{1}", completedMaps, totalMaps));
                }
            }

            player.sendMessage(string.Format("Map generation complete! Visited {0} points across {1} maps in {2}x{2} world.", completedSteps, totalMaps, worldSize));
        }

        private void InterruptibleDelay(int milliseconds, CancellationToken token)
        {
            int elapsed = 0;
            int checkInterval = 100;

            while (elapsed < milliseconds)
            {
                if (token.IsCancellationRequested)
                {
                    throw new OperationCanceledException();
                }

                int sleepTime = Math.Min(checkInterval, milliseconds - elapsed);
                Thread.Sleep(sleepTime);
                elapsed += sleepTime;
            }
        }

        private void ReturnToOriginalLocation()
        {
            if (hasOriginalLocation)
            {
                try
                {
                    player.teleport(originalX, originalY, originalZ);
                    player.sendMessage("Returned to original location.");
                }
                catch
                {
                    // Silently fail
                }
            }
        }
    }
}
