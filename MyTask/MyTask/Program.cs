

for (int i = 0;  i < 100;i++)
{
    ThreadPool.QueueUserWorkItem(delegate
    {
        Console.WriteLine(i);
        Thread.Sleep(1000);
    });
}

Console.ReadKey();