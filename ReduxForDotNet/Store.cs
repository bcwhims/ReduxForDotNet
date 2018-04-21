using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;

namespace ReduxForDotNet
{
    public class Store<TState>
    {
        private ObservableQueue<Action> queue = new ObservableQueue<Action>();
        public delegate void StateChangeEventHandler(object sender, StateChangeEventArgs<TState> e);
        public event StateChangeEventHandler StateChanged;

        internal Store()
        {
            queue.CollectionChanged += Queue_CollectionChanged;
            StateChanged += Store_StateChanged;
        }

        public TState State { get; internal set; }

        private void Queue_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if(e.Action == NotifyCollectionChangedAction.Add)
            {
                if (queue.TryDequeue(out Action action))
                {
                    var prevState = State;
                    action();
                    var newState = State;
                    if (!EqualityComparer<TState>.Default.Equals(newState, prevState))
                    {
                        StateChanged(this, new StateChangeEventArgs<TState>(prevState, newState));
                    }
                }
            }
        }

        private void Store_StateChanged(object sender, StateChangeEventArgs<TState> e)
        {

        }

        public void Dispatch<TAction>(TAction action)
        {
            queue.Enqueue(() =>
            {
                MiddleWares<TAction>.GetDispatcher(this)(action);
            });
        }

        private void Reduce<TAction>(TAction action)
        {
            var prevStat = State;
            State = Reducers<TAction>.ReducerList[this].Aggregate(prevStat, (state, reducer) => reducer(state, action));
        }

        public bool RegisterMiddleWares<TAction>(params MiddleWare<TAction, TState>[] middleWares)
        {
            return MiddleWares<TAction>.RegisterMiddleWares(this, middleWares);
        }

        public bool RegisterReducers<TAction>(params Reducer<TState, TAction>[] reducers)
        {
            return Reducers<TAction>.RegisterReducers(this, reducers);
        }

        public bool RegisterSelector<TReturn>(string name, Func<TState, TReturn> selector, SelectorCallback<TReturn> valueChangedCallback)
        {
            return Selectors<TReturn>.RegisterSelector(this, name, selector, valueChangedCallback);
        }

        public Func<TState, TReturn> GetSelector<TReturn>(string name)
        {
            return Selectors<TReturn>.GetSelector(this, name);
        }

        private static class Reducers<TAction>
        {
            internal static ConcurrentDictionary<Store<TState>, List<Reducer<TState, TAction>>> ReducerList = new ConcurrentDictionary<Store<TState>, List<Reducer<TState, TAction>>>();
            internal static bool RegisterReducers(Store<TState> store, params Reducer<TState, TAction>[] reducers)
            {
                if (reducers == null)
                {
                    return false;
                }
                var reducerList = ReducerList.GetOrAdd(store, (a) => new List<Reducer<TState, TAction>> ());
                reducerList.AddRange(reducers.Where(a => a != null));
                return true;
            }
        }

        private static class MiddleWares<TAction>
        {
            internal static ConcurrentDictionary<Store<TState>, List<MiddleWare<TAction, TState>>> MiddleWareList = new ConcurrentDictionary<Store<TState>, List<MiddleWare<TAction, TState>>>();
            internal static bool RegisterMiddleWares(Store<TState> store, params MiddleWare<TAction, TState>[] middleWares)
            {
                if (middleWares == null)
                {
                    return false;
                }
                var middleWareList = GetStoreMiddleWares(store);
                middleWareList.AddRange(middleWares.Where(a => a != null));
                return true;
            }
            internal static Dispatcher<TAction> GetDispatcher(Store<TState> store)
            {
                return GetStoreMiddleWares(store)
                .Select(a => a(store))
                .Reverse()
                .Aggregate<Func<Dispatcher<TAction>, Dispatcher<TAction>>, Dispatcher<TAction>>(store.Reduce, (dispatch, mW) => mW(dispatch));
            }
            private static List<MiddleWare<TAction, TState>> GetStoreMiddleWares(Store<TState> store)
            {
                return MiddleWareList.GetOrAdd(store, (a) => new List<MiddleWare<TAction, TState>>());
            }
        }
        private static class Selectors<TReturn>
        {
            internal static ConcurrentDictionary<Store<TState>,ConcurrentDictionary<string, Func<TState, TReturn>>> SelectorDictionary = new ConcurrentDictionary<Store<TState>, ConcurrentDictionary<string, Func<TState, TReturn>>>();
            internal static ConcurrentDictionary<Store<TState>, ConcurrentDictionary<string, TReturn>> SelectorReturnValues = new ConcurrentDictionary<Store<TState>, ConcurrentDictionary<string, TReturn>>();

            internal static Func<TState, TReturn> GetSelector(Store<TState> store, string name)
            {
                SelectorDictionary[store].TryGetValue(name, out Func<TState, TReturn> function);
                return function;
            }

            internal static bool RegisterSelector(Store<TState> store, string name, Func<TState, TReturn> selector, SelectorCallback<TReturn> valueChangedCallback)
            {
                if (selector == null)
                {
                    return false;
                }
                var selectorDictionary = SelectorDictionary.GetOrAdd(store, (a) => new ConcurrentDictionary<string, Func<TState, TReturn>>());
                var selectorReturnValues = SelectorReturnValues.GetOrAdd(store, (a) => new ConcurrentDictionary<string, TReturn>());
                selectorDictionary[name] = (state) =>
                {
                    try
                    {
                        return selector(state);
                    }
                    catch (Exception)
                    {
                        return default(TReturn);
                    }
                };
                selectorReturnValues[name] = selectorDictionary[name](store.State);

                void handler(object sender, StateChangeEventArgs<TState> eventArgs)
                {
                    var newValue = selectorDictionary[name](eventArgs.NewState);
                    var previousValue = selectorReturnValues[name];
                    if (!EqualityComparer<TReturn>.Default.Equals(newValue, previousValue))
                    {
                        selectorReturnValues[name] = newValue;
                        try
                        {
                            valueChangedCallback?.Invoke(previousValue, newValue);
                        }
                        catch (Exception)
                        {
                            valueChangedCallback = null;
                        }
                    }
                }
                store.StateChanged += handler;
                return true;
            }
        }
    }
}
