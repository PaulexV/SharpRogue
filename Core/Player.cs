using sharpRogue.Core;

namespace Core
{
    public class Player : Actor
    {
        public Player()
        {
            Awareness = 10;
            Name = "Rogue";
            Color = Colors.Player;
            Symbol = '@';
            X = 10;
            Y = 10;
        }
    }
}