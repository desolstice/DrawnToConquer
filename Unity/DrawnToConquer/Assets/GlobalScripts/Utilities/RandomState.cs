using System;
using Random = UnityEngine.Random;

public class RandomState : IDisposable
{

    Random.State state;

    public RandomState(int seed)
    {
        state = Random.state;
        Random.InitState(seed);
    }

    public void Dispose()
    {
        Random.state = state;
    }
}
