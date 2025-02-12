using System.Diagnostics;

public class Bar 
{
    public int Width { get; set; }
    public int Ind { get; set; }
    public int Pct { get; set; }
    public int CatFrame { get; set; }
    public long ElapsedMs { get; set; }
    public string Beg { get; set; }
    public string BegFilled { get; set; }
    public string Filler { get; set; }
    public string Space { get; set; }
    public string Head { get; set; }
    public string Fin { get; set; }
    public string FinFilled { get; set; }
}

public class UnicodeTheme
{
    public const string Beg = "\uee00";
    public const string BegFilled = "\uee03";
    public const string Filler = "\uee04";
    public const string Space = "\uee01";
    public const string Head = "\uee01";
    public const string Fin = "\uee02"; 
    public const string FinFilled = "\uee05"; 
}

public class AsciiTheme
{
    public const string Beg = "[";
    public const string BegFilled = "[";
    public const string Filler = "=";
    public const string Space = " ";
    public const string Head = ">";
    public const string Fin = "]"; 
    public const string FinFilled = "]"; 
}

public class RmrfArgs 
{
    public bool NoAnimation { get; set; }
    public bool DryRun { get; set; }
    public bool Ascii { get; set; }
    public bool Cat { get; set; }
    public string Path { get; set; }
    public string[] Include { get; set; }
    public string[] Exclude { get; set; }
}

public class Program 
{
    private static string[] _cats = [
        "(  ^ｰ^)ﾆｬﾝ",
        "(  ^ｰ^)ﾆｬﾝ",
        "ﾆｬﾝ(^ｰ^ ) ",
        "ﾆｬﾝ(^ｰ^*) ",
        "み (>ω<*) ",
        "み (>ω<*) ",
        "ﾆｬﾝ(^ｰ^ ) ",
        "ﾆｬﾝ(^ｰ^ ) ",
    ];

    public static void Main(string[] args)
    {
        if(!TryParse(args, out var rmrfArgs))
        {
            return;
        }
        Console.OutputEncoding = rmrfArgs.Ascii 
            ? System.Text.Encoding.ASCII             
            : System.Text.Encoding.UTF8;

        Console.Write("Analyzing...");
        var flatDirs = FlattenTargets(GetTargets(rmrfArgs));

        var success = true;
        Exception e = null;
        using var sw = CreateStream();
        var bar = CreateBar(rmrfArgs);
        Console.CancelKeyPress += delegate {
            if (rmrfArgs.NoAnimation)
                return;
            Recover(sw);
        };

        var stopwatch = new Stopwatch();
        DrawInitial(sw, bar, rmrfArgs);
        var deleted = 0;
        var total = flatDirs.Count;
        stopwatch.Start();
        foreach (var directory in flatDirs)
        {
            success = DeleteDirectory(directory, rmrfArgs, out e);
            if (!success)
                break;
            deleted++;
            var (nextPct, nextInd) = CountProgress(bar, deleted, total);
            MoveDelta(sw, bar, rmrfArgs, nextInd, nextPct, stopwatch.ElapsedMilliseconds);
        }
        if (success)
            MoveDelta(sw, bar, rmrfArgs, bar.Width - 1, 100, stopwatch.ElapsedMilliseconds);
        DrawComplete(sw, rmrfArgs, success, e);
    }

    private static bool DeleteDirectory(string path, RmrfArgs args, out Exception e)
    {
        e = null;
        try 
        {
            if (args.DryRun)
            {
                var time = Directory.GetDirectories(path).Length + 
                    Directory.GetFiles(path).Length;
                Thread.Sleep(time / 10);
                return true;
            }
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
    
    private static (int, int) CountProgress(Bar bar, int current, int total)
    {
        var pct = (double)current / total;
        return ((int)(pct * 100), (int)(pct * (bar.Width - 1)));
    }

    private static StreamWriter CreateStream()
    {
        var sw = new StreamWriter(Console.OpenStandardOutput());
        sw.AutoFlush = true;
        Console.SetOut(sw);
        return sw;
    }

    private static Bar CreateBar(RmrfArgs args)
    {
        var bar = new Bar
        {
            Width = 30
        };
        SetupTheme(bar, args);
        return bar;
    }

    private static void SetupTheme(Bar bar, RmrfArgs args)
    {
        if (args.Ascii)
        {
            bar.Beg = AsciiTheme.Beg;
            bar.BegFilled = AsciiTheme.BegFilled;
            bar.Filler = AsciiTheme.Filler;
            bar.Space = AsciiTheme.Space;
            bar.Head = AsciiTheme.Head;
            bar.Fin = AsciiTheme.Fin; 
            bar.FinFilled = AsciiTheme.FinFilled; 
        } 
        else 
        {
            bar.Beg = UnicodeTheme.Beg;
            bar.BegFilled = UnicodeTheme.BegFilled;
            bar.Filler = UnicodeTheme.Filler;
            bar.Space = UnicodeTheme.Space;
            bar.Head = UnicodeTheme.Head;
            bar.Fin = UnicodeTheme.Fin; 
            bar.FinFilled = UnicodeTheme.FinFilled; 
        }
    }

    private static List<string> FlattenTargets(List<string> targets)
    {
        var flat = new List<string>(targets.Count);
        foreach (var target in targets)
        {
            CollectInner(target, flat);
        }
        return flat;
    }

    private static void CollectInner(string path, List<string> flat)
    {
        var innerDirs = Directory.GetDirectories(path);
        foreach (var innerDir in innerDirs)
        {
            CollectInner(innerDir, flat);
        }
        flat.Add(path);
    }

    private static List<string> GetTargets(RmrfArgs args)
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

    private static bool TryParse(string[] args, out RmrfArgs res)
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
            else if (args[ind] == "-ascii") 
            {
                ind++;
                res.Ascii = true;
            }
            else if (args[ind] == "-dry") 
            {
                ind++;
                res.DryRun = true;
            }
            else if (args[ind] == "-na") 
            {
                ind++;
                res.NoAnimation = true;
            } 
            else if (args[ind] == "-cat") 
            {
                ind++;
                res.Cat = true;
            } 
            else 
            {
                Console.WriteLine("Invalid argument list");
                Console.WriteLine();
                Console.WriteLine("rmrf [args]");
                Console.WriteLine("args list:");
                Console.WriteLine("    -p <folder1>:                path");
                Console.WriteLine("    -i <folder1,folder2,...>:    directories to delete");
                Console.WriteLine("    -e <folder1,folder2,...>:    exclude paths");
                Console.WriteLine("    -na:                         no animation");
                Console.WriteLine("    -dry:                        dry run");
                Console.WriteLine("    -ascii:                      ascii theme");
                Console.WriteLine("    -cat:                        add a cat");
                return false;
            }
        }
        if (res.Ascii)
        {
            res.Cat = false;
        }
        return true;
    }

    private static void DrawInitial(StreamWriter sw, Bar bar, RmrfArgs args)
    {
        if (args.NoAnimation)
        {
            OutText(sw, "\r\n");
            OutText(sw, "Start deleting");
            OutText(sw, "\r\n");
            return;
        }
        OutEsc(sw, "?25l");
        OutEsc(sw, "1G");
        OutText(sw, bar.Beg);
        for (int i = 0; i < bar.Width; i++)
        {
            OutText(sw, bar.Space);
        }
        OutText(sw, bar.Fin);
        OutText(sw, "   0%");
        if (args.Cat)
        {
            OutText(sw, "    ");
            OutText(sw, _cats[0]);
        }
    }

    private static void DrawComplete(StreamWriter sw, RmrfArgs args, bool success, Exception e = null)
    {
        if (args.NoAnimation)
        {
            DrawCompleteNoAnimation(sw, success, e);
            return;
        }
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

    private static void DrawCompleteNoAnimation(StreamWriter sw, bool success, Exception e = null)
    {
        if (success)
        {
            OutText(sw, "Over");
            return;
        }

        OutText(sw, "Failed");
        if (e != null)
        {
            OutText(sw, "\r\n");
            OutText(sw, e.ToString());
        }
    }

    private static void Recover(StreamWriter sw)
    {
        OutEsc(sw, "0m");
        OutEsc(sw, "?25h");
    }

    private static void MoveDelta(
        StreamWriter sw, 
        Bar bar, 
        RmrfArgs args, 
        int nextInd, 
        int nextPct, 
        long elapsed)
    {
        if (args.NoAnimation)
        {
            MoveDeltaNoAnimation(sw, bar, args, nextInd, nextPct, elapsed);
            return;
        }
        var catFrame = GetCatFrame(bar, args, elapsed);
        if (nextInd == bar.Ind &&
            nextPct == bar.Pct &&
            catFrame == bar.CatFrame)
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

        if (catFrame != bar.CatFrame)
        {
            OutEsc(sw, 4 + 4 + bar.Width + "G");
            OutText(sw, "    ");
            OutText(sw, _cats[catFrame]);
            bar.ElapsedMs = elapsed;
        }

        bar.Ind = nextInd;
        bar.Pct = nextPct;
        bar.CatFrame = catFrame;
    }

    private static void MoveDeltaNoAnimation(
        StreamWriter sw, 
        Bar bar, 
        RmrfArgs args, 
        int nextInd, 
        int nextPct, 
        long elapsed)
    {
        const int delay = 10;
        if (nextPct - bar.Pct >= delay || (nextPct == 100 && bar.Pct != 100))
        {
            OutText(sw, $"Deleted {nextPct}%");
            OutText(sw, "\r\n");
            bar.Pct = nextPct;
        }
    }

    private static int GetCatFrame(Bar bar, RmrfArgs args, long elapsed)
    {
        if (!args.Cat)
            return 0;

        const int delay = 700;

        if (elapsed - bar.ElapsedMs > delay)
            return (bar.CatFrame + 1) % _cats.Length;

        return bar.CatFrame;
    }

    private static void OutEsc(StreamWriter sw, string str)
    {
        sw.Write("\x1b[");
        sw.Write(str);
    }

    private static void OutText(StreamWriter sw, string str)
    {
        sw.Write(str);
    }
}
