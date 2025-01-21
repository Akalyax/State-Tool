using UnityEngine;

namespace StateSystem
{
    public class StateManager : MonoBehaviour
    {
        #region Public Properties for State Interfaces
        public float ElapsedTime { get => m_elapsedTime; set => m_elapsedTime = value; }

        public KeyCode ActivationKey => m_activationKey;
        public bool IsActivated => m_isActivated;
        #endregion

        [SerializeField] protected KeyCode m_activationKey;

        #region Private Parameters
        private float m_elapsedTime;
        private bool m_isActivated;
        #endregion

        private IState m_currentState;

        protected virtual void Update()
        {
            // Start the effect on key press if not already activated
            if (Input.GetKeyDown(m_activationKey) && !m_isActivated)
                StartEffect();

            // Update the current state
            m_currentState?.Update();
        }

        /// <summary>
        /// Starts the effect, setting it as activated.
        /// </summary>
        protected virtual void StartEffect()
        {
            m_isActivated = true;
        }

        /// <summary>
        /// Resets parameters at the end of the effect.
        /// </summary>
        public virtual void EndEffect()
        {
            m_isActivated = false;
        }

        /// <summary>
        /// Changes the current state to a new one and resets elapsed time.
        /// </summary>
        /// <param name="newState">The new state to transition to.</param>
        public void SetState(IState newState)
        {
            m_currentState = newState;
            m_elapsedTime = 0;
            m_currentState?.Enter();
        }
    }
}

