using System.Collections.Concurrent;

for (int i = 0; i < 100; i++)
{
    var captured = i;
    MyThreadPool.QueueUserWorkItem(delegate
    {
        Console.WriteLine(captured);
        Thread.Sleep(1000);
    });
}


Console.WriteLine("-- end --." +
                  "press any key ...");

Console.ReadKey();


static class MyThreadPool
{
    private static readonly BlockingCollection<Action> s_workItems = new();

    public static void QueueUserWorkItem(Action action)
    {
        s_workItems.Add(action);
    }

    static MyThreadPool()
    {
        for (int i = 0; i < Environment.ProcessorCount; i++)
        {
            new Thread(() =>
            {
                while (true)
                {
                    var action = s_workItems.Take();
                    action();
                }
            })
            {
                IsBackground = false
            }.Start();
        }
    }
}