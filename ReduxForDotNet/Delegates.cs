using System;
using System.Collections.Generic;
using System.Text;

namespace ReduxForDotNet
{
    public delegate void Dispatcher<TAction>(TAction action);
    public delegate Func<Dispatcher<TAction>, Dispatcher<TAction>> MiddleWare<TAction, TState>(Store<TState> store);
    public delegate void SelectorCallback<TReturn>(TReturn previousValue, TReturn newValue);
    public delegate TState Reducer<TState, TAction>(TState state, TAction action);
}
