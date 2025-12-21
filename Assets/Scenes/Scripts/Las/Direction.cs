using UnityEngine;

namespace LaserSystem
{
    public enum Direction
    {
        Up,
        Down,
        Left,
        Right
    }

    public static class DirectionExtensions
    {
        public static Vector2Int ToVector2Int(this Direction dir)
        {
            switch (dir)
            {
                case Direction.Up:    return Vector2Int.up;
                case Direction.Down:  return Vector2Int.down;
                case Direction.Left:  return Vector2Int.left;
                case Direction.Right: return Vector2Int.right;
                default:              return Vector2Int.zero;
            }
        }

        public static Direction Opposite(this Direction dir)
        {
            switch (dir)
            {
                case Direction.Up:    return Direction.Down;
                case Direction.Down:  return Direction.Up;
                case Direction.Left:  return Direction.Right;
                case Direction.Right: return Direction.Left;
                default:              return dir;
            }
        }

        public static Direction TurnLeft(this Direction dir)
        {
            switch (dir)
            {
                case Direction.Up:    return Direction.Left;
                case Direction.Left:  return Direction.Down;
                case Direction.Down:  return Direction.Right;
                case Direction.Right: return Direction.Up;
                default:              return dir;
            }
        }

        public static Direction TurnRight(this Direction dir)
        {
            switch (dir)
            {
                case Direction.Up:    return Direction.Right;
                case Direction.Right: return Direction.Down;
                case Direction.Down:  return Direction.Left;
                case Direction.Left:  return Direction.Up;
                default:              return dir;
            }
        }
    }
}
