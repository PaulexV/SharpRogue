using System;
using System.Linq;
using Core;
using RogueSharp;
using sharpRogue;

public class MapGenerator
{
    private readonly int _width;
    private readonly int _height;
    private readonly int _maxRooms;
    private readonly int _roomMaxSize;
    private readonly int _roomMinSize;

    private readonly DungeonMap _map;

    // Constructing a new MapGenerator requires the dimensions of the maps it will create
    // as well as the sizes and maximum number of rooms
    public MapGenerator(int width, int height,
    int maxRooms, int roomMaxSize, int roomMinSize)
    {
        _width = width;
        _height = height;
        _maxRooms = maxRooms;
        _roomMaxSize = roomMaxSize;
        _roomMinSize = roomMinSize;
        _map = new DungeonMap();
    }

    // Generate a new map that places rooms randomly
    public DungeonMap CreateMap()
    {
        // Set the properties of all cells to false
        _map.Initialize(_width, _height);

        // Try to place as many rooms as the specified maxRooms
        // Note: Only using decrementing loop because of WordPress formatting
        for (int r = _maxRooms; r > 0; r--)
        {
            // Determine the size and position of the room randomly
            int roomWidth = Game.Random.Next(_roomMinSize, _roomMaxSize);
            int roomHeight = Game.Random.Next(_roomMinSize, _roomMaxSize);
            int roomXPosition = Game.Random.Next(0, _width - roomWidth - 1);
            int roomYPosition = Game.Random.Next(0, _height - roomHeight - 31);

            // All of our rooms can be represented as Rectangles
            var newRoom = new Rectangle(roomXPosition, roomYPosition,
              roomWidth, roomHeight);

            // Check to see if the room rectangle intersects with any other rooms
            bool newRoomIntersects = _map.Rooms.Any(room => newRoom.Intersects(room));

            // As long as it doesn't intersect add it to the list of rooms
            if (!newRoomIntersects)
            {
                _map.Rooms.Add(newRoom);
            }
        }
        // Iterate through each room that we wanted placed 
        // call CreateRoom to make it
        foreach (Rectangle room in _map.Rooms)
        {
            CreateRoom(room);
            // Iterate through each room that was generated
            // Don't do anything with the first room, so start at r = 1 instead of r = 0
            for (int r = 1; r < _map.Rooms.Count; r++)
            {
                // For all remaing rooms get the center of the room and the previous room
                int previousRoomCenterX = _map.Rooms[r - 1].Center.X;
                int previousRoomCenterY = _map.Rooms[r - 1].Center.Y;
                int currentRoomCenterX = _map.Rooms[r].Center.X;
                int currentRoomCenterY = _map.Rooms[r].Center.Y;

                // Give a 50/50 chance of which 'L' shaped connecting hallway to tunnel out
                if (Game.Random.Next(1, 2) == 1)
                {
                    CreateHorizontalTunnel(previousRoomCenterX, currentRoomCenterX, previousRoomCenterY);
                    CreateVerticalTunnel(previousRoomCenterY, currentRoomCenterY, currentRoomCenterX);
                }
                else
                {
                    CreateVerticalTunnel(previousRoomCenterY, currentRoomCenterY, previousRoomCenterX);
                    CreateHorizontalTunnel(previousRoomCenterX, currentRoomCenterX, currentRoomCenterY);
                }
            }
        }
        PlacePlayer();

        return _map;
    }

    // Given a rectangular area on the map
    // set the cell properties for that area to true
    private void CreateRoom(Rectangle room)
    {
        for (int x = room.Left + 1; x < room.Right; x++)
        {
            for (int y = room.Top + 1; y < room.Bottom; y++)
            {
                _map.SetCellProperties(x, y, true, true, false);
            }
        }
    }
    // Find the center of the first room that we created and place the Player there
    private void PlacePlayer()
    {
        Player player = Game.Player;
        if (player == null)
        {
            player = new Player();
        }

        player.X = _map.Rooms[0].Center.X;
        player.Y = _map.Rooms[0].Center.Y;

        _map.AddPlayer(player);
    }
    // Carve a tunnel out of the map parallel to the x-axis
    private void CreateHorizontalTunnel(int xStart, int xEnd, int yPosition)
    {
        for (int x = Math.Min(xStart, xEnd); x <= Math.Max(xStart, xEnd); x++)
        {
            _map.SetCellProperties(x, yPosition, true, true);
        }
    }

    // Carve a tunnel out of the map parallel to the y-axis
    private void CreateVerticalTunnel(int yStart, int yEnd, int xPosition)
    {
        for (int y = Math.Min(yStart, yEnd); y <= Math.Max(yStart, yEnd); y++)
        {
            _map.SetCellProperties(xPosition, y, true, true);
        }
    }
}