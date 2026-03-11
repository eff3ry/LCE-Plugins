using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LCEPlugin
{
    internal class Util
    {

        /// <summary>
        /// Represents a 3D coordinate in the game world.
        /// </summary>
        public class Coordinate
        {
            #region Properties

            /// <summary>
            /// Gets the X coordinate.
            /// </summary>
            public int X { get; }

            /// <summary>
            /// Gets the Y coordinate (vertical).
            /// </summary>
            public int Y { get; }

            /// <summary>
            /// Gets the Z coordinate.
            /// </summary>
            public int Z { get; }

            #endregion

            #region Constructor

            /// <summary>
            /// Initializes a new instance of the <see cref="Coordinate"/> class.
            /// </summary>
            /// <param name="x">The X coordinate.</param>
            /// <param name="y">The Y coordinate.</param>
            /// <param name="z">The Z coordinate.</param>
            public Coordinate(int x, int y, int z)
            {
                X = x;
                Y = y;
                Z = z;
            }

            #endregion

            #region Equality Methods

            /// <summary>
            /// Determines whether the specified object is equal to the current coordinate.
            /// </summary>
            /// <param name="obj">The object to compare.</param>
            /// <returns>True if the objects are equal; otherwise, false.</returns>
            public override bool Equals(object obj)
            {
                if (obj is Coordinate other)
                {
                    return X == other.X && Y == other.Y && Z == other.Z;
                }
                return false;
            }

            /// <summary>
            /// Returns a hash code for this coordinate.
            /// </summary>
            /// <returns>A hash code for the current coordinate.</returns>
            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 17;
                    hash = hash * 31 + X;
                    hash = hash * 31 + Y;
                    hash = hash * 31 + Z;
                    return hash;
                }
            }

            #endregion

            #region String Representation

            /// <summary>
            /// Returns a string representation of the coordinate.
            /// </summary>
            /// <returns>A string in the format "(X, Y, Z)".</returns>
            public override string ToString()
            {
                return string.Format("({0}, {1}, {2})", X, Y, Z);
            }

            #endregion
        }
    }
}
