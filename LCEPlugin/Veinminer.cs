using Minecraft.Server.FourKit;
using Minecraft.Server.FourKit.Block;
using Minecraft.Server.FourKit.Entity;
using Minecraft.Server.FourKit.Event;
using Minecraft.Server.FourKit.Event.Block;
using Minecraft.Server.FourKit.Plugin;
using System;
using System.Collections.Generic;
using System.Threading;
using static LCEPlugin.Util;

namespace LCEPlugin
{
    /// <summary>
    /// TreeFeller plugin that automatically breaks entire trees when a log is broken.
    /// </summary>
    public class VeinMiner : ServerPlugin
    {
        #region Plugin Metadata

        /// <summary>
        /// Gets the name of the plugin.
        /// </summary>
        public override string name => "VeinMiner";

        /// <summary>
        /// Gets the current version of the plugin.
        /// </summary>
        public override string version => "1.0.0";

        /// <summary>
        /// Gets the author of the plugin.
        /// </summary>
        public override string author => "effery";

        #endregion

        #region Lifecycle Methods

        /// <summary>
        /// Called when the plugin is enabled.
        /// Registers the tree listener for block break events.
        /// </summary>
        public void OnEnable()
        {
            FourKit.addListener(new VeinListener());
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
    /// Listens for block break events and implements Vein Mining Logic.
    /// </summary>
    public class VeinListener : Listener
    {
        #region Constants

        //private const int LOG_BLOCK_TYPE = 17; //redo with ore ids
        private static readonly int[] ORE_TYPES = new int[] { 14, 15, 16, 21, 56, 73, 74, 129, 153 }; // Gold, Iron, Coal, Lapis, Diamond, Redstone, Emerald, Nether Quartz 
        //redstone 73, lit redstone 74, need to add a special case for redstone to break both
        private const int MAX_ORES_TO_BREAK = 64;

        #endregion

        #region Event Handlers

        /// <summary>
        /// Called when a block is broken in the game.
        /// If the block is a ore, breaks all connected same ore in the vein.
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

            if (Array.IndexOf(ORE_TYPES, block.getType()) == -1)
            {
                return;
            }

            //if (!player.isSneaking())
            //{
            //    return;
            //}

            int oresBroken = VeinMine(block, player, block.getTypeId());

            if (oresBroken > 1)
            {
                player.sendMessage(string.Format("Vein Mined! Broke {0} ores.", oresBroken));
            }
        }

        #endregion

        #region VeinMining Logic


        //todo convert to blocktype checks
        /// <summary>
        /// Breaks all logs connected to the initial log using a flood-fill algorithm.
        /// </summary>
        /// <param name="initialBlock">The first log block that was broken.</param>
        /// <param name="player">The player who broke the block.</param>
        /// <returns>The total number of logs broken (including the initial block).</returns>
        private int VeinMine(Block initialBlock, Player player, int blockType)
        {

            HashSet<Coordinate> visited = new HashSet<Coordinate>();
            Queue<Coordinate> toCheck = new Queue<Coordinate>();

            Coordinate startCoord = new Coordinate(initialBlock.getX(), initialBlock.getY(), initialBlock.getZ());
            visited.Add(startCoord);

            // Add adjacent blocks to the initial block (don't re-break the initial block)
            CheckAdjacentBlocks(startCoord, visited, toCheck);

            int oresBroken = 1; // Count the initial block the player broke

            while (toCheck.Count > 0 && oresBroken < MAX_ORES_TO_BREAK)
            {
                Coordinate current = toCheck.Dequeue();

                Block currentBlock = GetBlockAt(player, current);
                if (blockType == 73 || blockType == 74) // Special case for redstone to break both lit and unlit
                {
                    if (currentBlock == null || (currentBlock.getTypeId() != 73 && currentBlock.getTypeId() != 74))
                    {
                        continue;
                    }
                }
                else
                {
                    if (currentBlock == null || currentBlock.getTypeId() != blockType)
                    {
                        continue;
                    }
                }

                BreakBlock(currentBlock, player);
                oresBroken++;

                CheckAdjacentBlocks(current, visited, toCheck);
            }

            return oresBroken;
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
            try
            {
                block.breakNaturally(); // We must suffer with lag util break async is implemented
            }
            catch
            {
                // Silently fail if block cannot be broken
            }
        }

        #endregion
    }

}
