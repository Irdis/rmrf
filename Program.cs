public class Bar 
{
    public int Width { get; set; }
    public int Ind { get; set; }
    public int Pct { get; set; }
    public string Beg { get; } = "\uee00";
    public string BegFilled { get; } = "\uee03";
    public string Filler { get; } = "\uee04";
    public string Space { get; } = "\uee01";
    public string Head { get; } = "\uee01";
    public string Fin { get; } = "\uee02"; 
    public string FinFilled { get; } = "\uee05"; 
}

public class RmrfArgs 
{
    public bool NoAnimation { get; set; }
    public string Path { get; set; }
    public string[] Include { get; set; }
    public string[] Exclude { get; set; }
}

public class Program 
{
    public static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        if(!TryParse(args, out var rmrfArgs))
        {
            return;
        }
        Console.Write("Analyzing...");
        var flatDirs = FlattenTargets(GetTargets(rmrfArgs));

        var success = true;
        Exception e = null;
        using var sw = CreateStream();
        var bar = CreateBar();
        Console.CancelKeyPress += delegate {
            Recover(sw);
        };

        DrawInitial(sw, bar);
        var deleted = 0;
        var total = flatDirs.Count;
        foreach (var directory in flatDirs)
        {
            success = DeleteDirectory(directory, out e);
            if (!success)
                break;
            deleted++;
            var (nextPct, nextInd) = CountProgress(bar, deleted, total);
            MoveDelta(sw, bar, nextInd, nextPct);
        }
        if (success)
            MoveDelta(sw, bar, bar.Width - 1, 100);
        DrawComplete(sw, success, e);
    }

    public static bool DeleteDirectory(string path, out Exception e)
    {
        e = null;
        try 
        {
            foreach (var dir in Directory.GetDirectories(path))
            {
                Directory.Delete(dir);
            }
            foreach (var file in Directory.GetFiles(path))
            {
                File.Delete(file);
            }
            if (path != Directory.GetCurrentDirectory())
            {
                Directory.Delete(path);
            }
        }
        catch (Exception err)
        {
            e = err;
            return false;
        }

        return true;
    }
    
    public static (int, int) CountProgress(Bar bar, int current, int total)
    {
        var pct = (double)current / total;
        return ((int)(pct * 100), (int)(pct * (bar.Width - 1)));
    }

    public static StreamWriter CreateStream()
    {
        var sw = new StreamWriter(Console.OpenStandardOutput());
        sw.AutoFlush = true;
        Console.SetOut(sw);
        return sw;
    }

    public static Bar CreateBar()
    {
        var bar = new Bar
        {
            Width = 30
        };
        return bar;
    }

    public static List<string> FlattenTargets(List<string> targets)
    {
        var flat = new List<string>(targets.Count);
        foreach (var target in targets)
        {
            CollectInner(target, flat);
        }
        return flat;
    }

    public static void CollectInner(string path, List<string> flat)
    {
        var innerDirs = Directory.GetDirectories(path);
        foreach (var innerDir in innerDirs)
        {
            CollectInner(innerDir, flat);
        }
        flat.Add(path);
    }

    public static List<string> GetTargets(RmrfArgs args)
    {
        var res = new List<string>();
        var path = args.Path;

        if (args.Include == null)
        {
            res.Add(path);
            return res;
        }
        CollectIncludes(path, args, res);
        return res;
    }

    private static void CollectIncludes(string path, RmrfArgs args, List<string> targets)
    {
        var dirs = Directory.GetDirectories(path);
        var toExplore = new List<string>();
        foreach (var dir in dirs)
        {
            var name = Path.GetFileName(dir);
            if (args.Include.Any(f => f == name)) 
            {
                targets.Add(dir);
            } 
            else if (args.Exclude != null && args.Exclude.Any(f => f == name))
            {
                continue;
            }
            else
            {
                toExplore.Add(dir);
            }
        }
        foreach (var dir in toExplore)
        {
            CollectIncludes(dir, args, targets);
        }
    }

    public static bool TryParse(string[] args, out RmrfArgs res)
    {
        int ind = 0;
        res = new RmrfArgs();
        res.Path = Directory.GetCurrentDirectory();
        while (ind < args.Length)
        {
            if (args[ind] == "-p" && ind < args.Length - 1)
            {
                ind++;
                res.Path = args[ind];
                ind++;
            } 
            else if (args[ind] == "-i" && ind < args.Length - 1)
            {
                ind++;
                res.Include = args[ind].Split(',');
                ind++;
            }
            else if (args[ind] == "-e" && ind < args.Length - 1)
            {
                ind++;
                res.Exclude = args[ind].Split(',');
                ind++;
            }
            else if (args[ind] == "-na") 
            {
                ind++;
                res.NoAnimation = true;
            } else {
                Console.WriteLine("rmrf [args]");
                Console.WriteLine("args list:");
                Console.WriteLine("    -p:  path");
                Console.WriteLine("    -i:  directories to delete");
                Console.WriteLine("    -e:  exclude paths");
                Console.WriteLine("    -na: no animation");
                return false;
            }
        }
        return true;
    }

    public static void DrawInitial(StreamWriter sw, Bar bar)
    {
        OutEsc(sw, "?25l");
        OutEsc(sw, "1G");
        OutText(sw, bar.Beg);
        for (int i = 0; i < bar.Width; i++)
        {
            OutText(sw, bar.Space);
        }
        OutText(sw, bar.Fin);
        OutText(sw, "   0%");
    }

    public static void DrawComplete(StreamWriter sw, bool success, Exception e = null)
    {
        OutEsc(sw, "0m");
        OutText(sw, "\r\n");
        if (success)
        {
            OutEsc(sw, "32m");
            OutText(sw, "Over");
        } else {
            OutEsc(sw, "31m");
            OutText(sw, "Failed");
        }
        OutEsc(sw, "0m");
        if (e != null)
        {
            OutText(sw, "\r\n");
            OutText(sw, e.ToString());
        }
        OutEsc(sw, "?25h");
    }

    public static void Recover(StreamWriter sw)
    {
        OutEsc(sw, "0m");
        OutEsc(sw, "?25h");
    }

    public static void MoveDelta(StreamWriter sw, Bar bar, int nextInd, int nextPct)
    {
        if (nextInd == bar.Ind &&
            nextPct == bar.Pct)
            return;

        if (nextInd != bar.Ind)
        {
            if (bar.Ind == 0)
            {
                OutEsc(sw, 1 + "G");
                OutText(sw, bar.BegFilled);
            }
            OutEsc(sw, 2 + bar.Ind + "G");

            for (int i = bar.Ind; i < nextInd; i++)
            {
                OutText(sw, bar.Filler);
            }
            if (nextInd == bar.Width - 1)
            {
                OutText(sw, bar.Filler);
                OutText(sw, bar.FinFilled);
            } 
            else 
            {
                OutText(sw, bar.Head);
            }
        }
        if (nextPct != bar.Pct)
        {
            OutEsc(sw, 4 + bar.Width + "G");
            var pctStr = nextPct.ToString().PadLeft(3, ' ');
            OutText(sw, pctStr);
        }

        bar.Ind = nextInd;
        bar.Pct = nextPct;
    }

    public static void OutEsc(StreamWriter sw, string str)
    {
        sw.Write("\x1b[");
        sw.Write(str);
    }

    public static void OutText(StreamWriter sw, string str)
    {
        sw.Write(str);
    }
}
