<Query Kind="Program" />

void Main()
{
	
    var s = System.Diagnostics.Stopwatch.StartNew ();
    s.Start ();
    s.Stop ();
    s.Start ();
    s.Stop ();
    s.Start ();
    s.Stop ();
    
    s.Restart ();
    var ss = 0;
    for (var i = 0; i < 1_000_000_000; i++) 
    {
        ss = F(i, 7);
    }
    
    s.Stop ();
    
    s.Elapsed.Dump ();
        
}

// Define other methods, classes and namespaces here
int F (int a, int b)
{
	switch (a,b)
	{
		case (0,0): return 0;
		case (0,1): return 1;
		case (1,0): return 2;
		case (1,1): return 3;
		default: return -1;
	}
}