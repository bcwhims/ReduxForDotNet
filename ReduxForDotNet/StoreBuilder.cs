using System;
using System.Collections.Generic;
using System.Text;

namespace ReduxForDotNet
{
    public class StoreBuilder<TState>
    {
        private Store<TState> store;

        private StoreBuilder()
        {
            store = new Store<TState>();
        }

        public StoreBuilder<TState> WithInitialState(TState state)
        {
            store.State = state;
            return this;
        }

        public StoreBuilder<TState> WithThunkMiddleWare()
        {
            store.RegisterMiddleWares<Action<Store<TState>>>(store => next => action => {
                action(store);
            });
            return this;
        }

        public StoreBuilder<TState> WithMiddleWares<TAction>(params MiddleWare<TAction, TState>[] middleWares)
        {
            store.RegisterMiddleWares(middleWares);
            return this;
        }

        public StoreBuilder<TState> WithReducers<TAction>(params Reducer<TState, TAction>[] reducers)
        {
            store.RegisterReducers(reducers);
            return this;
        }

        public StoreBuilder<TState> WithSelector<TReturn>(string name, Func<TState, TReturn> selector, SelectorCallback<TReturn> valueChangedCallback)
        {
            store.RegisterSelector(name, selector, valueChangedCallback);
            return this;
        }

        public StoreBuilder<TState> WithSelector<TReturn>(string name, Func<TState, TReturn> selector)
        {
            store.RegisterSelector(name, selector, null);
            return this;
        }

        public Store<TState> Build()
        {
            return store;
        }

        public static StoreBuilder<TState> Create()
        {
            return new StoreBuilder<TState>();
        }

    }
}
