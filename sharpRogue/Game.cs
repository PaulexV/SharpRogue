using System;
using System.Data;
using System.Threading;
using Core;
using Database;
using MySql.Data.MySqlClient;
using RLNET;
using RogueSharp.Random;
using ServerCommunication;
using sharpRogue.Core;
using Systems;

namespace sharpRogue
{
    public class Game
    {
        // The screen height and width are in number of tiles
        private static readonly int _screenWidth = 180;
        private static readonly int _screenHeight = 110;

        private static RLRootConsole _rootConsole;

        // The map console takes up most of the screen and is where the map will be drawn
        private static readonly int _mapWidth = 160;
        private static readonly int _mapHeight = 96;
        private static RLConsole _mapConsole;

        // Below the map console is the message console which displays attack rolls and other information
        private static readonly int _messageWidth = 160;
        private static readonly int _messageHeight = 22;
        private static RLConsole _messageConsole;

        // The stat console is to the right of the map and display player and monster stats
        private static readonly int _statWidth = 40;
        private static readonly int _statHeight = 140;
        private static RLConsole _statConsole;

        // Above the map is the inventory console which shows the players equipment, abilities, and items
        private static readonly int _inventoryWidth = 160;
        private static readonly int _inventoryHeight = 22;
        private static RLConsole _inventoryConsole;
        public static Player Player { get; set; }
        public static DungeonMap DungeonMap { get; private set; }
        public static MessageLog MessageLog { get; private set; }
        public static SchedulingSystem SchedulingSystem { get; private set; }

        private static bool _renderRequired = true;
        public static CommandSystem CommandSystem { get; private set; }
        // Singleton of IRandom used throughout the game when generating random numbers
        public static IRandom Random { get; private set; }
        private static int _mapLevel = 1;

        // Event handler for RLNET's Update event
        private static void OnRootConsoleUpdate(object sender, UpdateEventArgs e)
        {
            _inventoryConsole.SetBackColor(0, 0, _inventoryWidth, _inventoryHeight, Palette.DbWood);
            _inventoryConsole.Print(1, 1, "Inventory", Colors.TextHeading);
            bool didPlayerAct = false;
            RLKeyPress keyPress = _rootConsole.Keyboard.GetKeyPress();

            if (CommandSystem.IsPlayerTurn)
            {
                if (keyPress != null)
                {
                    if (keyPress.Key == RLKey.Up)
                    {
                        didPlayerAct = CommandSystem.MovePlayer(Directions.Up);
                    }
                    else if (keyPress.Key == RLKey.Down)
                    {
                        didPlayerAct = CommandSystem.MovePlayer(Directions.Down);
                    }
                    else if (keyPress.Key == RLKey.Left)
                    {
                        didPlayerAct = CommandSystem.MovePlayer(Directions.Left);
                    }
                    else if (keyPress.Key == RLKey.Right)
                    {
                        didPlayerAct = CommandSystem.MovePlayer(Directions.Right);
                    }
                    else if (keyPress.Key == RLKey.Escape)
                    {
                        _rootConsole.Close();
                    }
                    else if (keyPress.Key == RLKey.Period)
                    {
                        if (DungeonMap.CanMoveDownToNextLevel())
                        {
                            MapGenerator mapGenerator = new MapGenerator(_mapWidth, _mapHeight, 20, 13, 7, ++_mapLevel);
                            DungeonMap = mapGenerator.CreateMap();
                            MessageLog = new MessageLog();
                            CommandSystem = new CommandSystem();
                            _rootConsole.Title = $"SharpRogue - Level {_mapLevel}";
                            didPlayerAct = true;
                        }
                    }
                }
                if (didPlayerAct)
                {
                    _renderRequired = true;
                    CommandSystem.EndPlayerTurn();
                }
            }
            else
            {
                CommandSystem.ActivateMonsters();
                _renderRequired = true;
            }
        }
        // Event handler for RLNET's Render event
        private static void OnRootConsoleRender(object sender, UpdateEventArgs e)
        {
            if (_renderRequired)
            {
                _mapConsole.Clear();
                _statConsole.Clear();
                _messageConsole.Clear();

                DungeonMap.Draw(_mapConsole, _statConsole);
                Player.Draw(_mapConsole, DungeonMap);
                Player.DrawStats(_statConsole);
                MessageLog.Draw(_messageConsole);

                // Blit the sub consoles to the root console in the correct locations
                RLConsole.Blit(_mapConsole, 0, 0, _mapWidth, _mapHeight,
                  _rootConsole, 0, _inventoryHeight);
                RLConsole.Blit(_statConsole, 0, 0, _statWidth, _statHeight,
                  _rootConsole, _mapWidth, 0);
                RLConsole.Blit(_messageConsole, 0, 0, _messageWidth, _messageHeight,
                  _rootConsole, 0, _screenHeight - _messageHeight);
                RLConsole.Blit(_inventoryConsole, 0, 0, _inventoryWidth, _inventoryHeight,
                  _rootConsole, 0, 0);
                // Tell RLNET to draw the console that we set

                _rootConsole.Draw();

                _renderRequired = false;
            }
            _renderRequired = true;
        }

        static void StartServer()
        {
            string myIPAddress = "192.168.1.79";
            int port = 3000;

            Client client = new Client(myIPAddress, port);

            client.ConnectToServer();
            Console.WriteLine("Connected to server.");

            Thread.Sleep(1000);
            Console.Clear();

            client.ServerData();

            try
            {
                string messageToServer = "";

                while (client.clientStatus)
                {
                    messageToServer = Console.ReadLine();

                    if (messageToServer == "start game")
                    {
                        client.clientStatus = false;
                        client.streamWriter.Flush();
                    }
                    if (messageToServer != "start game")
                    {
                        client.streamWriter.WriteLine(messageToServer);
                        client.streamWriter.Flush();
                    }
                }
            }
            catch
            {
                Console.WriteLine("Problem reading from server");
            }
            client.Disconnect();
        }
        static void LastPlayed()
        {
            // Étabilissez de la connexion à la base de données. 
            MySqlConnection connection = DBUtils.GetDBConnection();
            connection.Open();
            try
            {
                // La commande Insert.
                string sql = "INSERT INTO sharpRogue.GameInfo(SELECT CURDATE());";

                MySqlCommand cmd = null;
                cmd = new MySqlCommand(sql, connection);

                // Exécutez la Commande (Utilisez pour supprimer, insérer, mettre à jour).
                int rowCount = cmd.ExecuteNonQuery();

                Console.WriteLine("Row Count affected = " + rowCount);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e);
                Console.WriteLine(e.StackTrace);
            }
            finally
            {
                connection.Close();
                connection.Dispose();
                connection = null;
            }
            Console.Read();
        }


        public static void Main()
        {
            StartServer();
            LastPlayed();

            // Establish the seed for the random number generator from the current time
            int seed = (int)DateTime.UtcNow.Ticks;
            Random = new DotNetRandom(seed);

            // The title will appear at the top of the console window 
            // also include the seed used to generate the level
            string consoleTitle = $"RogueSharp - Level {_mapLevel} - Seed {seed}"; ;

            // Create a new MessageLog and print the random seed used to generate the level
            MessageLog = new MessageLog();
            MessageLog.Add($"The rogue arrives on level '{_mapLevel}'");
            MessageLog.Add($"Level created with seed '{seed}'");

            CommandSystem = new CommandSystem();
            SchedulingSystem = new SchedulingSystem();
            // This must be the exact name of the bitmap font file we are using or it will error.
            string fontFileName = "terminal8x8.png";
            // Tell RLNet to use the bitmap font that we specified and that each tile is 8 x 8 pixels
            _rootConsole = new RLRootConsole(fontFileName, _screenWidth, _screenHeight,
              8, 8, 1f, consoleTitle);
            _mapConsole = new RLConsole(_mapWidth, _mapHeight);
            _messageConsole = new RLConsole(_messageWidth, _messageHeight);
            _statConsole = new RLConsole(_statWidth, _statHeight);
            _inventoryConsole = new RLConsole(_inventoryWidth, _inventoryHeight);
            // Set up a handler for RLNET's Update event
            _rootConsole.Update += OnRootConsoleUpdate;
            // Set up a handler for RLNET's Render event
            _rootConsole.Render += OnRootConsoleRender;
            // Begin RLNET's game loop
            MapGenerator mapGenerator = new MapGenerator(_mapWidth, _mapHeight, 20, 25, 8, _mapLevel);
            DungeonMap = mapGenerator.CreateMap();
            DungeonMap.UpdatePlayerFieldOfView();
            _rootConsole.Run();
        }
    }
}