namespace Camera.Wpf.Models
{
    public class Axis2D
    {
        public int X { get; private set; }
        public int Y { get; private set; }

        public void SetX(int value)
        {
            X = value;
        }

        public void SetY(int value)
        {
            Y = value;
        }

        public int GetX()
        {
            return X;
        }

        public int GetY()
        {
            return Y;
        }
    }
}