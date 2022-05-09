namespace Camera.Wpf.Models
{
    public class Axis3D : Axis2D
    {
        private float Z { get; set; }
        public void SetZ(float value)
        {
            Z = value;
        }

        public float GetZ()
        {
            return Z;
        }
    }
}