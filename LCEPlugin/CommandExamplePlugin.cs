using System;
using System.Collections.Generic;
using System.Linq;

namespace LCEPlugin
{
    /// <summary>
    /// Main plugin class for the LCE Server Plugin.
    /// Handles plugin lifecycle and initialization of command system.
    /// </summary>
    public class CommandExamplePlugin : ServerPlugin
    {
        #region Plugin Metadata

        /// <summary>
        /// Gets the name of the plugin.
        /// </summary>
        public string GetName() => "LCEPlugin";

        /// <summary>
        /// Gets the current version of the plugin.
        /// </summary>
        public string GetVersion() => "0.0.1";

        /// <summary>
        /// Gets the author of the plugin.
        /// </summary>
        public string GetAuthor() => "effery";

        #endregion

        #region Lifecycle Methods

        /// <summary>
        /// Called when the plugin is enabled.
        /// Initializes the command registry and registers event listeners.
        /// </summary>
        public void OnEnable()
        {
            CommandRegistry.Initialize();
            FourKit.addListener(new CommandListener());
        }

        /// <summary>
        /// Called when the plugin is disabled.
        /// Performs cleanup operations.
        /// </summary>
        public void OnDisable()
        {
            CommandRegistry.Cleanup();
        }

        #endregion
    }

    /// <summary>
    /// Listens for player chat events and processes commands.
    /// </summary>
    public class CommandListener : Listener
    {
        private const char COMMAND_PREFIX = '?';

        /// <summary>
        /// Handles player chat events and intercepts commands.
        /// </summary>
        /// <param name="e">The chat event.</param>
        [EventHandler]
        public void onChat(PlayerChatEvent e)
        {
            Player player = e.getPlayer();
            string message = e.getMessage()?.Trim();

            if (!IsCommand(message))
            {
                return;
            }

            e.setCancelled(true);

            ParseAndExecuteCommand(player, message);
        }

        /// <summary>
        /// Checks if the message is a command.
        /// </summary>
        private bool IsCommand(string message)
        {
            return !string.IsNullOrEmpty(message) && message[0] == COMMAND_PREFIX;
        }

        /// <summary>
        /// Parses the command message and executes the command.
        /// </summary>
        private void ParseAndExecuteCommand(Player player, string message)
        {
            string commandText = message.Substring(1);
            string[] parts = commandText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0)
            {
                return;
            }

            string commandName = parts[0].ToLower();
            string[] args = parts.Skip(1).ToArray();

            CommandRegistry.ExecuteCommand(player, commandName, args);
        }
    }

    /// <summary>
    /// Manages command registration and execution.
    /// </summary>
    public static class CommandRegistry
    {
        #region Constants

        private const string ERROR_UNKNOWN_COMMAND = "Unknown command. Type /help for a list of commands.";
        private const string ERROR_COMMAND_EXECUTION = "Error executing command: {0}";

        #endregion

        #region Fields

        private static readonly Dictionary<string, Command> commands = new Dictionary<string, Command>();
        private static bool isInitialized = false;

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the command registry and registers all available commands.
        /// </summary>
        public static void Initialize()
        {
            if (isInitialized)
            {
                return;
            }

            RegisterBuiltInCommands();
            isInitialized = true;
        }

        /// <summary>
        /// Cleans up the command registry.
        /// </summary>
        public static void Cleanup()
        {
            commands.Clear();
            isInitialized = false;
        }

        #endregion

        #region Command Registration

        /// <summary>
        /// Registers all built-in commands.
        /// </summary>
        private static void RegisterBuiltInCommands()
        {
            RegisterCommand(new Command(
                "help",
                "Displays a list of available commands",
                OnHelpCommand
            ));

            RegisterCommand(new Command(
                "hello",
                "Sends a personalized greeting message",
                OnHelloCommand
            ));

            RegisterCommand(new Command(
                "ping",
                "Tests server responsiveness",
                OnPingCommand
            ));

            RegisterCommand(new Command(
                "echo",
                "Echoes back your message",
                OnEchoCommand
            ));

            RegisterCommand(new Command(
                "heal",
                "heals yourself",
                OnHealCommand
            ));
        }

        /// <summary>
        /// Registers a new command in the registry.
        /// </summary>
        /// <param name="command">The command to register.</param>
        public static void RegisterCommand(Command command)
        {
            if (command == null)
            {
                return;
            }

            string key = command.Name.ToLower();

            if (!commands.ContainsKey(key))
            {
                commands[key] = command;
            }
        }

        /// <summary>
        /// Unregisters a command from the registry.
        /// </summary>
        /// <param name="commandName">The name of the command to unregister.</param>
        /// <returns>True if the command was found and removed; otherwise, false.</returns>
        public static bool UnregisterCommand(string commandName)
        {
            if (string.IsNullOrEmpty(commandName))
            {
                return false;
            }

            return commands.Remove(commandName.ToLower());
        }

        #endregion

        #region Command Execution

        /// <summary>
        /// Executes a registered command.
        /// </summary>
        /// <param name="player">The player executing the command.</param>
        /// <param name="commandName">The name of the command.</param>
        /// <param name="args">The command arguments.</param>
        public static void ExecuteCommand(Player player, string commandName, string[] args)
        {
            if (commands.TryGetValue(commandName, out Command command))
            {
                try
                {
                    command.Execute(player, args);
                }
                catch (Exception ex)
                {
                    player.sendMessage(string.Format(ERROR_COMMAND_EXECUTION, ex.Message));
                }
            }
            else
            {
                player.sendMessage(ERROR_UNKNOWN_COMMAND);
            }
        }

        #endregion

        #region Command Handlers

        /// <summary>
        /// Handles the /help command.
        /// </summary>
        private static void OnHelpCommand(Player player, string[] args)
        {
            player.sendMessage("=== Available Commands ===");

            foreach (var command in commands.Values.OrderBy(c => c.Name))
            {
                player.sendMessage(string.Format("?{0} - {1}", command.Name, command.Description));
            }
        }

        /// <summary>
        /// Handles the /hello command.
        /// </summary>
        private static void OnHelloCommand(Player player, string[] args)
        {
            player.sendMessage(string.Format("Hello, {0}! Welcome to the server!", player.getName()));
        }

        /// <summary>
        /// Handles the /ping command.
        /// </summary>
        private static void OnPingCommand(Player player, string[] args)
        {
            player.sendMessage("Pong!");
        }

        /// <summary>
        /// Handles the /echo command.
        /// </summary>
        private static void OnEchoCommand(Player player, string[] args)
        {
            if (args.Length == 0)
            {
                player.sendMessage("Usage: /echo <message>");
                return;
            }

            player.sendMessage(string.Join(" ", args));
        }

        private static void OnHealCommand(Player player, string[] args)
        {
            player.setHealth(20);
        }

        #endregion

        #region Query Methods

        /// <summary>
        /// Gets a read-only collection of all registered commands.
        /// </summary>
        /// <returns>A collection of registered commands.</returns>
        public static IEnumerable<Command> GetRegisteredCommands()
        {
            return commands.Values.ToList().AsReadOnly();
        }

        /// <summary>
        /// Checks if a command is registered.
        /// </summary>
        /// <param name="commandName">The name of the command.</param>
        /// <returns>True if the command is registered; otherwise, false.</returns>
        public static bool IsCommandRegistered(string commandName)
        {
            return !string.IsNullOrEmpty(commandName) && commands.ContainsKey(commandName.ToLower());
        }

        #endregion
    }

    /// <summary>
    /// Represents a command that can be executed by players.
    /// </summary>
    public class Command
    {
        #region Properties

        /// <summary>
        /// Gets the name of the command.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the description of the command.
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// Gets the execution handler for the command.
        /// </summary>
        public Action<Player, string[]> Execute { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="Command"/> class.
        /// </summary>
        /// <param name="name">The name of the command.</param>
        /// <param name="description">A brief description of what the command does.</param>
        /// <param name="execute">The action to execute when the command is called.</param>
        /// <exception cref="ArgumentException">Thrown when the command name is null or empty.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the execute action is null.</exception>
        public Command(string name, string description, Action<Player, string[]> execute)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Command name cannot be null or empty.", nameof(name));
            }

            if (execute == null)
            {
                throw new ArgumentNullException(nameof(execute), "Command execute action cannot be null.");
            }

            Name = name;
            Description = description ?? string.Empty;
            Execute = execute;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Returns a string representation of the command.
        /// </summary>
        /// <returns>A string in the format "/{Name} - {Description}".</returns>
        public override string ToString()
        {
            return string.Format("/{0} - {1}", Name, Description);
        }

        #endregion
    }
}
