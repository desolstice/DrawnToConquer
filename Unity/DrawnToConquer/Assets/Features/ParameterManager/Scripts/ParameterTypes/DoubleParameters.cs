public enum DoubleParameters
{
    DefaultWorldScale,
    DefaultWorldWidth,
    DefaultWorldHeight,
    DefaultWorldOctaves,
    DefaultWorldLacunarity,
    DefaultWorldPersistence
}

public static class DoubleParametersExtensions
{
    public static double GetValue(this DoubleParameters parameter)
    {
        return ParameterManager.DoubleParametersValues.Value[parameter];
    }
}