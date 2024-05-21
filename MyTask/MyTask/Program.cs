using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;


Console.WriteLine("Hello, ");

MyTask.Delay(1000).ContinueWith(delegate
{
    Console.WriteLine("Bulat");
});


Console.ReadLine();
// AsyncLocal<int> myValue = new();
//
// List<MyTask> tasks = new();
//
// for (int i = 0; i < 100; i++)
// {
//     myValue.Value = i;
//
//     tasks.Add(MyTask.Run(delegate
//     {
//         Console.WriteLine(myValue.Value);
//         Thread.Sleep(1000);
//     }));
//
//     // MyThreadPool.QueueUserWorkItem(delegate
//     // {
//     //     Console.WriteLine(myValue.Value);
//     //     Thread.Sleep(1000);
//     // });
// }
//
// // foreach (var myTask in tasks)
// // {
// //     myTask.Wait();
// // }
//
// MyTask.WhenAll(tasks).Wait();
//
//
// Console.WriteLine("-- end --." +
//                   "press any key ...");
//
// Console.ReadKey();

/// <summary>
/// Реализация меоге таска. ТАск это всего лишь  структура данных которая храниться в памяти и содержит некторую инфу.
/// </summary>
class MyTask
{
    private bool _isCompleted;
    private Exception? _exception;
    private Action? _continuation;
    private ExecutionContext? _executionContext;

    /// <summary>
    /// Проверка завершился ли мой таск 
    /// </summary>
    public bool IsCompleted
    {
        get
        {
            // плохо блокировать весь объекь. В общем случае лучше так не делать.  А бликровать через какую то переменнную.
            lock (this)
            {
                return _isCompleted;
            }
        }
    }

    /// <summary>
    /// Мну нужно так метод которым я помечу что мой  таск выполнен
    /// </summary>
    public void SetResult()
    {
        Complete(null);
    }

    /// <summary>
    /// метод что бы поменить таск как Failed
    /// </summary>
    /// <param name="exception"></param>
    public void SetExceptio(Exception exception)
    {
        Complete(exception);
    }

    private void Complete(Exception? exception)
    {
        lock (this)
        {
            if (_isCompleted) throw new InvalidOperationException("пнх");

            _isCompleted = true;

            _exception = exception;

            if (_continuation is not null)
            {
                MyThreadPool.QueueUserWorkItem(delegate
                {
                    if (_executionContext is null)
                    {
                        _continuation();
                    }
                    else
                    {
                        ExecutionContext.Run(_executionContext, state => ((Action)state!).Invoke(), _continuation);
                    }
                });
            }
        }
    }


    /// <summary>
    /// или задать  метод котоырм я продолжу его выполнение 
    /// </summary>
    /// <param name="action"></param>
    public void ContinueWith(Action action)
    {
        lock (this)
        {
            if (_isCompleted)
            {
                MyThreadPool.QueueUserWorkItem(action);
            }
            else
            {
                _continuation = action;
                _executionContext = ExecutionContext.Capture();
            }
        }
    }

    /// <summary>
    /// методы что бы подождать пока он не выполниться 
    /// </summary>
    public void Wait()
    {
        ManualResetEventSlim? mres = null;

        lock (this)
        {
            if (!_isCompleted)
            {
                mres = new ManualResetEventSlim();
                ContinueWith(mres.Set);
            }
        }

        mres?.Wait();

        if (_exception is not null)
        {
            //не делаем  просто  throw  потому что мы просто передаем уже существующий exception ,  но при этом мы не потеряем  трейс 
            //throw new Exception("", _exception);  раскажи про такой способ  отправки экспешена с сохранением трассировки
            //throw new AggregateException(_exception); //  но скажи что вот такой спосбо тоже будет рабаотьт
            ExceptionDispatchInfo.Throw(_exception); // но самый пацанский способ этот
        }
    }

    public static MyTask Run(Action action)
    {
        MyTask task = new();

        MyThreadPool.QueueUserWorkItem(() =>
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                task.SetExceptio(e);
                return;
            }

            task.SetResult();
        });

        return task;
    }

    public static MyTask WhenAll(List<MyTask> tasks)
    {
        MyTask t = new MyTask();

        if (tasks.Count == 0)
            t.SetResult();
        else
        {
            int remaning = tasks.Count;

            Action continuation = () =>
            {
                if (Interlocked.Decrement(ref remaning) == 0)
                {
                    t.SetResult();
                }
            };

            foreach (var myTask in tasks)
            {
                myTask.ContinueWith(continuation);
            }
        }
        return t;
    }


    public static MyTask Delay(int timeout)
    {
        MyTask t = new MyTask();

        new Timer(_ => t.SetResult()).Change(timeout, -1);

        // почему  же тут не написать просто Thread.Sleep().  Потому что мы выкючаем целый Thread из нашего  ThreadPool,  при этом у нас полно работы!!!
        return t;
    }
}


static class MyThreadPool
{
    private static readonly BlockingCollection<(Action, ExecutionContext?)> s_workItems = new();

    public static void QueueUserWorkItem(Action action)
    {
        s_workItems.Add((action, ExecutionContext.Capture()));
    }

    static MyThreadPool()
    {
        for (int i = 0; i < Environment.ProcessorCount; i++)
        {
            new Thread(() =>
            {
                while (true)
                {
                    var (action, context) = s_workItems.Take();

                    if (context is null)
                    {
                        action();
                    }
                    else
                    {
                        //ExecutionContext.Run(context, delegate { action();},null);
                        // так эффективнее , потому что выше это рабоатет через замыкание, а для того что бы все это поддержваить компилятор
                        // несколько дополнительных переменных
                        ExecutionContext.Run(context, state => ((Action)state!).Invoke(), action);
                    }
                }
            })
            {
                IsBackground = false
            }.Start();
        }
    }
}