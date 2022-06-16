using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SseNamedPipeTest
{
    public class MessageWorker
    {
        private readonly BlockingCollection<Action> _queue;

        public MessageWorker()
        {
            _queue = new BlockingCollection<Action>();
            Thread thread = new Thread(() =>
                {
                    foreach (var action in _queue.GetConsumingEnumerable())
                    {
                        try
                        {
                            action.Invoke();
                        }
                        catch (Exception ex)
                        {
                            //
                        }
                    }
                })
                { IsBackground = true };
            thread.Start();
        }

        public void InsertWorkItem(Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            _queue.TryAdd(action);
        }
    }
}
