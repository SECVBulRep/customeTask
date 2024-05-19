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

/// <summary>
/// Реализация меоге таска. ТАск это всего лишь  структра данных которая храниться в памяти и содержит некторую инфу.
/// </summary>
class MyTask
{
    /// <summary>
    /// Проверка завершился ли мой таск 
    /// </summary>
    public bool IsCompleted
    {
        get { }
    }

    /// <summary>
    /// Мну нужно так метод которым я помечу что мой  таск выполнен
    /// </summary>
    public void SetResult()
    {
    }

    /// <summary>
    /// метод что бы поменить таск как Failed
    /// </summary>
    /// <param name="exception"></param>
    public void SetFailed(Exception exception)
    {
    }

    /// <summary>
    /// методы что бы подождать пока он не выполниться 
    /// </summary>
    public void Wait()
    {
    }

    /// <summary>
    /// или задать  метод котоырм я продолжу его выполнение 
    /// </summary>
    /// <param name="action"></param>
    public void ContinueWith(Action action)
    {
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