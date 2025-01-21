using StateSystem;
using UnityEngine;

public class ExampleState : IState
{
    private readonly StateManager manager;

    public ExampleState(StateManager manager) => this.manager = manager;

    private float m_timeBetweenActions;

    public void Enter()
    {
        // Initialization when entering the state
    }

    public void Update()
    {
        // Logic to be executed during the state
    }
}