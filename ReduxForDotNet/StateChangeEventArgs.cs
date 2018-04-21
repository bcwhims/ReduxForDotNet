using System;
using System.Collections.Generic;
using System.Text;

namespace ReduxForDotNet
{
    public class StateChangeEventArgs<TState> : EventArgs
    {
        public StateChangeEventArgs(TState previousState, TState newState)
        {
            PreviousState = previousState;
            NewState = newState;
        }

        public TState PreviousState { get; set; }
        public TState NewState { get; set; }
    }
}
