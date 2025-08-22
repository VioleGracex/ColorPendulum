public enum BallColor
{
    Red,
    Green,
    Blue,
    Magenta,
    None,
}

public static class BallColorExtensions
{
    public static UnityEngine.Color ToColor(this BallColor color)
    {
        switch (color)
        {
            case BallColor.Red:
                return UnityEngine.Color.red;
            case BallColor.Green:
                return UnityEngine.Color.green;
            case BallColor.Blue:
                return UnityEngine.Color.blue;
            case BallColor.Magenta:
                return UnityEngine.Color.magenta;
            default:
                return UnityEngine.Color.magenta;
        }
    }
}