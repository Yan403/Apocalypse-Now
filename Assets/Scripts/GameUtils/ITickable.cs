/// <summary>
/// Interface para efeitos passivos que funcionam por ticks (intervalos de tempo).
/// </summary>
public interface ITickable
{
    /// <summary> Intervalo em segundos entre cada tick. Deve ser > 0. </summary>
    float TickInterval { get; }

    /// <summary> Chamado pelo manager a cada tick. </summary>
    void OnTick();
}
