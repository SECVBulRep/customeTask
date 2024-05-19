using System.Collections.Concurrent;
using System.Runtime.CompilerServices;


AsyncLocal<int> myValue = new();

for (int i = 0; i < 100; i++)
{
    myValue.Value = i;
    MyThreadPool.QueueUserWorkItem(delegate
    {
        Console.WriteLine(myValue.Value);
        Thread.Sleep(1000);
    });
}


Console.WriteLine("-- end --." +
                  "press any key ...");

Console.ReadKey();


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