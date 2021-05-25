using System.Collections.Generic;
using System.Linq;
using RLNET;
using RogueSharp;
using sharpRogue;
using sharpRogue.Core;

namespace Core
{
    // Our custom DungeonMap class extends the base RogueSharp Map class
    public class DungeonMap : Map
    {
        public List<Rectangle> Rooms;
        private readonly List<Monster> _monsters;
        public List<Doors> Doors { get; set; }
        public DungeonMap()
        {
            // Initialize all the lists when we create a new DungeonMap
            Rooms = new List<Rectangle>();
            _monsters = new List<Monster>();
            Doors = new List<Doors>();
        }
        // The Draw method will be called each time the map is updated
        // It will render all of the symbols/colors for each cell to the map sub console
        public void Draw(RLConsole mapConsole, RLConsole statConsole)
        {
            foreach (Cell cell in GetAllCells())
            {
                SetConsoleSymbolForCell(mapConsole, cell);
            }
            // Keep an index so we know which position to draw monster stats at
            int i = 0;
            // Iterate through each monster on the map and draw it after drawing the Cells
            foreach (Monster monster in _monsters)
            {
                monster.Draw(mapConsole, this);
                // When the monster is in the field-of-view also draw their stats
                if (IsInFov(monster.X, monster.Y))
                {
                    // Pass in the index to DrawStats and increment it afterwards
                    monster.DrawStats(statConsole, i);
                    i++;
                }
            }
            foreach (Doors door in Doors)
            {
                door.Draw(mapConsole, this);
            }
        }

        private void SetConsoleSymbolForCell(RLConsole console, Cell cell)
        {
            // When we haven't explored a cell yet, we don't want to draw anything
            if (!cell.IsExplored)
            {
                return;
            }

            // When a cell is currently in the field-of-view it should be drawn with lighter colors
            if (IsInFov(cell.X, cell.Y))
            {
                // Choose the symbol to draw based on if the cell is walkable or not
                // '.' for floor and '#' for walls
                if (cell.IsWalkable)
                {
                    console.Set(cell.X, cell.Y, Colors.FloorFov, Colors.FloorBackgroundFov, '.');
                }
                else
                {
                    console.Set(cell.X, cell.Y, Colors.WallFov, Colors.WallBackgroundFov, '#');
                }
            }
            // When a cell is outside of the field of view draw it with darker colors
            else
            {
                if (cell.IsWalkable)
                {
                    console.Set(cell.X, cell.Y, Colors.Floor, Colors.FloorBackground, '.');
                }
                else
                {
                    console.Set(cell.X, cell.Y, Colors.Wall, Colors.WallBackground, '#');
                }
            }
        }
        // Called by MapGenerator after we generate a new map to add the player to the map
        public void AddPlayer(Player player)
        {
            Game.Player = player;
            SetIsWalkable(player.X, player.Y, false);
            UpdatePlayerFieldOfView();
            Game.SchedulingSystem.Add(player);
        }
        // This method will be called any time we move the player to update field-of-view
        public void UpdatePlayerFieldOfView()
        {
            Player player = Game.Player;
            // Compute the field-of-view based on the player's location and awareness
            ComputeFov(player.X, player.Y, player.Awareness, true);
            // Mark all cells in field-of-view as having been explored
            foreach (Cell cell in GetAllCells())
            {
                if (IsInFov(cell.X, cell.Y))
                {
                    SetCellProperties(cell.X, cell.Y, cell.IsTransparent, cell.IsWalkable, true);
                }
            }
        }
        // Returns true when able to place the Actor on the cell or false otherwise
        public bool SetActorPosition(Actor actor, int x, int y)
        {
            // Only allow actor placement if the cell is walkable
            if (GetCell(x, y).IsWalkable)
            {
                // The cell the actor was previously on is now walkable
                SetIsWalkable(actor.X, actor.Y, true);
                // Update the actor's position
                actor.X = x;
                actor.Y = y;
                // The new cell the actor is on is now not walkable
                SetIsWalkable(actor.X, actor.Y, false);
                OpenDoor(actor, x, y);
                // Don't forget to update the field of view if we just repositioned the player
                if (actor is Player)
                {
                    UpdatePlayerFieldOfView();
                }
                return true;
            }
            return false;
        }

        // A helper method for setting the IsWalkable property on a Cell
        public void SetIsWalkable(int x, int y, bool isWalkable)
        {
            ICell cell = GetCell(x, y);
            SetCellProperties(cell.X, cell.Y, cell.IsTransparent, isWalkable, cell.IsExplored);
        }
        public void AddMonster(Monster monster)
        {
            _monsters.Add(monster);
            // After adding the monster to the map make sure to make the cell not walkable
            SetIsWalkable(monster.X, monster.Y, false);
            Game.SchedulingSystem.Add(monster);
        }
        public void RemoveMonster(Monster monster)
        {
            _monsters.Remove(monster);
            // After removing the monster from the map, make sure the cell is walkable again
            SetIsWalkable(monster.X, monster.Y, true);
            Game.SchedulingSystem.Remove(monster);
        }
        public Monster GetMonsterAt(int x, int y)
        {
            return _monsters.FirstOrDefault(m => m.X == x && m.Y == y);
        }
        // Look for a random location in the room that is walkable.
        public Point GetRandomWalkableLocationInRoom(Rectangle room)
        {
            if (DoesRoomHaveWalkableSpace(room))
            {
                for (int i = 0; i < 100; i++)
                {
                    int x = Game.Random.Next(1, room.Width - 2) + room.X;
                    int y = Game.Random.Next(1, room.Height - 2) + room.Y;
                    if (IsWalkable(x, y))
                    {
                        return new Point(x, y);
                    }
                }
            }
            // If we didn't find a walkable location in the room return null
            return default(Point);
        }

        // Iterate through each Cell in the room and return true if any are walkable
        public bool DoesRoomHaveWalkableSpace(Rectangle room)
        {
            for (int x = 1; x <= room.Width - 2; x++)
            {
                for (int y = 1; y <= room.Height - 2; y++)
                {
                    if (IsWalkable(x + room.X, y + room.Y))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        // Return the door at the x,y position or null if one is not found.
        public Doors GetDoor(int x, int y)
        {
            return Doors.SingleOrDefault(d => d.X == x && d.Y == y);
        }

        // The actor opens the door located at the x,y position
        private void OpenDoor(Actor actor, int x, int y)
        {
            Doors door = GetDoor(x, y);
            if (door != null && !door.IsOpen)
            {
                door.IsOpen = true;
                var cell = GetCell(x, y);
                // Once the door is opened it should be marked as transparent and no longer block field-of-view
                SetCellProperties(x, y, true, cell.IsWalkable, cell.IsExplored);

                Game.MessageLog.Add($"{actor.Name} opened a door");
            }
        }
    }
}