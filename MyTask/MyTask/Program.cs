

for (int i = 0;  i < 100;i++)
{
    var captured = i;
    ThreadPool.QueueUserWorkItem(delegate
    {
        Console.WriteLine(captured);
        Thread.Sleep(1000);
    });
}

Console.WriteLine("-- end --." +
                  "press any key ...");

Console.ReadKey();