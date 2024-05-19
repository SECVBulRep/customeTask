using System.Collections.Concurrent;


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