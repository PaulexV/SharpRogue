using Core;
using sharpRogue;

namespace Systems
{
    public class CommandSystem
    {
        // Return value is true if the player was able to move
        // false when the player couldn't move, such as trying to move into a wall
        public bool MovePlayer(Directions direction)
        {
            int x = Game.Player.X;
            int y = Game.Player.Y;

            switch (direction)
            {
                case Directions.Up:
                    {
                        y = Game.Player.Y - 1;
                        break;
                    }
                case Directions.Down:
                    {
                        y = Game.Player.Y + 1;
                        break;
                    }
                case Directions.Left:
                    {
                        x = Game.Player.X - 1;
                        break;
                    }
                case Directions.Right:
                    {
                        x = Game.Player.X + 1;
                        break;
                    }
                default:
                    {
                        return false;
                    }
            }

            if (Game.DungeonMap.SetActorPosition(Game.Player, x, y))
            {
                return true;
            }

            return false;
        }
    }
}