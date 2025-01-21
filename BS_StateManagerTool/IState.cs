namespace StateSystem
{
    /// <summary>
    /// Interface for states.
    /// Contains methods for entering and updating a state.
    /// </summary>
    public interface IState
    {
        void Enter();
        void Update();
    }
}