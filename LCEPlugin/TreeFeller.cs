using Minecraft.Server.FourKit;
using Minecraft.Server.FourKit.Block;
using Minecraft.Server.FourKit.Entity;
using Minecraft.Server.FourKit.Event;
using Minecraft.Server.FourKit.Event.Block;
using Minecraft.Server.FourKit.Plugin;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using static LCEPlugin.Util;

namespace LCEPlugin
{
    /// <summary>
    /// TreeFeller plugin that automatically breaks entire trees when a log is broken.
    /// </summary>
    public class TreeFeller : ServerPlugin
    {
        #region Plugin Metadata

        /// <summary>
        /// Gets the name of the plugin.
        /// </summary>
        public string GetName() => "TreeFeller";

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
            FourKit.addListener(new TreeListener());
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

    /// <summary>
    /// Listens for block break events and implements tree felling logic.
    /// </summary>
    public class TreeListener : Listener
    {
        #region Constants

        private static readonly int[] LOG_BLOCK_TYPES = new int[] { 17 }; //Currenly all logs are under 1 types with additional data.
        private const int MAX_LOGS_TO_BREAK = 64;

        #endregion

        #region Event Handlers

        /// <summary>
        /// Called when a block is broken in the game.
        /// If the block is a log, breaks all connected logs in the tree.
        /// </summary>
        /// <param name="e">The event data for the block break event.</param>
        [EventHandler]
        public void onBlockBreak(BlockBreakEvent e)
        {
            Block block = e.getBlock();
            Player player = e.getPlayer();

            if (block == null || player == null)
            {
                return;
            }

            if (Array.IndexOf(LOG_BLOCK_TYPES, block.getType()) == -1)
            {
                return;
            }

            //if (!player.isSneaking())
            //{
            //    return;
            //}

            int logsBroken = FellTree(block, player);

            if (logsBroken > 1)
            {
                player.sendMessage(string.Format("Tree felled! Broke {0} logs.", logsBroken));
            }
        }

        #endregion

        #region Tree Felling Logic

        /// <summary>
        /// Breaks all logs connected to the initial log using a flood-fill algorithm.
        /// </summary>
        /// <param name="initialBlock">The first log block that was broken.</param>
        /// <param name="player">The player who broke the block.</param>
        /// <returns>The total number of logs broken (including the initial block).</returns>
        private int FellTree(Block initialBlock, Player player)
        {
            HashSet<Coordinate> visited = new HashSet<Coordinate>();
            Queue<Coordinate> toCheck = new Queue<Coordinate>();

            Coordinate startCoord = new Coordinate(initialBlock.getX(), initialBlock.getY(), initialBlock.getZ());
            visited.Add(startCoord);

            // Add adjacent blocks to the initial block (don't re-break the initial block)
            CheckAdjacentBlocks(startCoord, visited, toCheck);

            int logsBroken = 1; // Count the initial block the player broke

            while (toCheck.Count > 0 && logsBroken < MAX_LOGS_TO_BREAK)
            {
                Coordinate current = toCheck.Dequeue();

                Block currentBlock = GetBlockAt(player, current);
                if (currentBlock == null || currentBlock.getType() != initialBlock.getType() || currentBlock.getData() != initialBlock.getData()) //Issue block type and orientation are within the same value
                {
                    continue;
                }

                BreakBlock(currentBlock, player);
                logsBroken++;

                CheckAdjacentBlocks(current, visited, toCheck);
            }

            return logsBroken;
        }

        /// <summary>
        /// Checks all adjacent blocks and adds unvisited log blocks to the queue.
        /// </summary>
        /// <param name="coord">The coordinate to check around.</param>
        /// <param name="visited">Set of already visited coordinates.</param>
        /// <param name="toCheck">Queue of coordinates to check.</param>
        private void CheckAdjacentBlocks(Coordinate coord, HashSet<Coordinate> visited, Queue<Coordinate> toCheck)
        {
            Coordinate[] adjacentOffsets = GetAdjacentOffsets();

            foreach (Coordinate offset in adjacentOffsets)
            {
                Coordinate neighbor = new Coordinate(
                    coord.X + offset.X,
                    coord.Y + offset.Y,
                    coord.Z + offset.Z
                );

                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    toCheck.Enqueue(neighbor);
                }
            }
        }

        /// <summary>
        /// Gets the offsets for all 26 adjacent blocks (including diagonals).
        /// </summary>
        /// <returns>Array of coordinate offsets.</returns>
        private Coordinate[] GetAdjacentOffsets()
        {
            return new Coordinate[]
            {
                // Same Y level (8 blocks)
                new Coordinate(-1, 0, 0), new Coordinate(1, 0, 0),
                new Coordinate(0, 0, -1), new Coordinate(0, 0, 1),
                new Coordinate(-1, 0, -1), new Coordinate(-1, 0, 1),
                new Coordinate(1, 0, -1), new Coordinate(1, 0, 1),

                // Y+1 level (9 blocks)
                new Coordinate(-1, 1, 0), new Coordinate(1, 1, 0),
                new Coordinate(0, 1, -1), new Coordinate(0, 1, 1),
                new Coordinate(-1, 1, -1), new Coordinate(-1, 1, 1),
                new Coordinate(1, 1, -1), new Coordinate(1, 1, 1),
                new Coordinate(0, 1, 0),

                // Y-1 level (9 blocks)
                new Coordinate(-1, -1, 0), new Coordinate(1, -1, 0),
                new Coordinate(0, -1, -1), new Coordinate(0, -1, 1),
                new Coordinate(-1, -1, -1), new Coordinate(-1, -1, 1),
                new Coordinate(1, -1, -1), new Coordinate(1, -1, 1),
                new Coordinate(0, -1, 0)
            };
        }

        /// <summary>
        /// Gets the block at the specified coordinate.
        /// </summary>
        /// <param name="player">The player's world context.</param>
        /// <param name="coord">The coordinate to get the block from.</param>
        /// <returns>The block at the coordinate, or null if not accessible.</returns>
        private Block GetBlockAt(Player player, Coordinate coord)
        {
            try
            {
                Block block = player.getWorld().getBlockAt(coord.X, coord.Y, coord.Z);
                return block;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Breaks a block in the world.
        /// </summary>
        /// <param name="block">The block to break.</param>
        /// <param name="player">The player context.</param>
        private void BreakBlock(Block block, Player player)
        {
            //TODO add gamemode check and change behavior accordingly (creative should just set to air, survival should drop items and play break animation)
            try
            {
                block.breakNaturally();
            }
            catch
            {
                // Silently fail if block cannot be broken
            }
        }

        #endregion

    }

}
