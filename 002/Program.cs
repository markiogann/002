using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

// ENUM
public enum Role { Leader, Medic, Climber, Support }
public enum WeatherCondition { Sunny, Windy, Snowy, Storm }

// STRUCT
public struct Coordinates
{
    public double X;
    public double Y;
    public double Z;

    public Coordinates(double x, double y, double z)
    {
        X = x; Y = y; Z = z;
    }
    public override string ToString() => $"({X}; {Y}; {Z})";
}

// другой класс
public class Equipment
{
    public string Name { get; set; } = "";
    public double WeightKg { get; set; }
    public int Durability { get; set; }

    public override string ToString()
    {
        if (string.IsNullOrWhiteSpace(Name)) return "(нет)";
        return $"{Name} ({WeightKg} кг, прочн. {Durability})";
    }
}

// основной объект коллекции (Climber)
public class Climber
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Name { get; set; } = "";
    public string Status { get; set; } = "";
    public Role Role { get; set; }
    public WeatherCondition LastKnownWeather { get; set; }
    public double Energy { get; set; }
    public int Altitude { get; set; }
    public double Experience { get; set; }
    public Coordinates Position { get; set; }
    public Equipment? PersonalEquipment { get; set; }

    public Climber()
    {
        CreatedAt = DateTime.Now;
    }

    public override string ToString()
    {
        return $"{Id}: {Name}, {Role}, статус: {(string.IsNullOrWhiteSpace(Status) ? "-" : Status)}, " +
               $"высота {Altitude} м, энергия {Energy}, опыт {Experience}, " +
               $"погода {LastKnownWeather}, позиция {Position}, снаряж.: {PersonalEquipment}";
    }
}

// приложение
class Program
{
    static Dictionary<int, Climber> db = new Dictionary<int, Climber>();
    static string csvPath = "climbers.csv";
    static readonly CultureInfo CI = CultureInfo.InvariantCulture;
    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
            csvPath = args[0];
        else
        {
            var env = Environment.GetEnvironmentVariable("CLIMBERS_CSV");
            if (!string.IsNullOrWhiteSpace(env))
                csvPath = env;
            else
            {
                Console.Write($"введите путь к CSV (пусто — {csvPath}): ");
                var input = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(input)) csvPath = input.Trim();
            }
        }
        try { LoadCsv(csvPath); }
        catch (Exception ex) { Console.WriteLine($"[warn] не удалось загрузить файл: {ex.Message}"); }

        DateTime initTime = DateTime.Now;

        Console.WriteLine("коллекция альпинистов. введите 'help' для списка команд.");
        while (true)
        {
            Console.Write("\n> ");
            var line = Console.ReadLine();
            if (line == null) break;
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (line.StartsWith("execute_script"))
            {
                var parts = SplitCmd(line);
                if (parts.Count < 2) { Console.WriteLine("usage: execute_script file_name"); continue; }
                ExecuteScript(parts[1], initTime);
                continue;
            }
            if (!HandleCommand(line, initTime))
                break;
        }
    }

    // разбор и выполнение одной команды 
    static bool HandleCommand(string line, DateTime initTime)
    {
        var parts = SplitCmd(line);
        var cmd = parts[0].ToLowerInvariant();

        try
        {
            switch (cmd)
            {
                case "help":
                    PrintHelp();
                    break;

                case "info":
                    Console.WriteLine("тип коллекции: Dictionary<int, Climber>");
                    Console.WriteLine($"дата инициализации: {initTime}");
                    Console.WriteLine($"элементов: {db.Count}");
                    break;

                case "show":
                    if (db.Count == 0) { Console.WriteLine("(пусто)"); break; }
                    foreach (var kv in db.OrderBy(k => k.Key))
                        Console.WriteLine(kv.Value);
                    break;

                case "insert":
                    {
                        var cl = ReadClimberInteractively(generateId: true);
                        db[cl.Id] = cl;
                        Console.WriteLine("добавлено: " + cl);
                    }
                    break;

                case "update":
                    {
                        if (parts.Count < 2) { Console.WriteLine("usage: update id"); break; }
                        if (!int.TryParse(parts[1], out int id) || !db.ContainsKey(id))
                        {
                            Console.WriteLine("нет такого id");
                            break;
                        }
                        var updated = ReadClimberInteractively(generateId: false, fixedId: id);
                        db[id] = updated;
                        Console.WriteLine("обновлено: " + updated);
                    }
                    break;

                case "remove_key":
                    {
                        if (parts.Count < 2) { Console.WriteLine("usage: remove_key id"); break; }
                        if (int.TryParse(parts[1], out int id) && db.Remove(id))
                            Console.WriteLine("удалено: " + id);
                        else
                            Console.WriteLine("нет такого id");
                    }
                    break;

                case "clear":
                    db.Clear();
                    Console.WriteLine("коллекция очищена.");
                    break;

                case "save":
                    SaveCsv(csvPath);
                    Console.WriteLine($"сохранено в {csvPath}");
                    break;

                case "exit":
                    return false;


                case string s when s.StartsWith("filter_role"):
                    {
                        if (parts.Count < 2) { Console.WriteLine("usage: filter_role {Leader|Medic|Climber|Support}"); break; }
                        if (!Enum.TryParse<Role>(parts[1], out var role)) { Console.WriteLine("неизвестная роль"); break; }
                        var q = db.Values.Where(c => c.Role == role);
                        foreach (var c in q) Console.WriteLine(c);
                    }
                    break;

                case string s2 when s2.StartsWith("group_by_role"):
                    {
                        if (db.Count == 0) { Console.WriteLine("(пусто)"); break; }
                        var gr = db.Values.GroupBy(c => c.Role);
                        foreach (var g in gr)
                        {
                            Console.WriteLine($"\n{g.Key}: {g.Count()} шт.");
                            foreach (var c in g) Console.WriteLine("  " + c);
                        }
                    }
                    break;

                case "count":
                    Console.WriteLine(db.Count);
                    break;

                default:
                    Console.WriteLine("неизвестная команда. Введите 'help'.");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("[error] " + ex.Message);
        }
        return true;
    }

    static Climber ReadClimberInteractively(bool generateId, int fixedId = 0)
    {
        var c = new Climber();
        if (generateId)
        {
            var rnd = new Random();
            int id = rnd.Next(1000, 9999);
            while (db.ContainsKey(id)) id = rnd.Next(1000, 9999);
            c.Id = id;
        }
        else
        {
            c.Id = fixedId;
        }
        c.Name = ReadRequiredString("имя");
        c.Status = ReadOptionalString("статус (пусто — пропустить)");
        c.Role = ReadEnum<Role>("роль", Enum.GetNames(typeof(Role)));
        c.LastKnownWeather = ReadEnum<WeatherCondition>("погода", Enum.GetNames(typeof(WeatherCondition)));
        c.Energy = ReadDouble("энергия (0..100)", 0, 100);
        c.Altitude = ReadInt("высота (0..8848)", 0, 8848);
        c.Experience = ReadDouble("опыт (0..10)", 0, 10);
        double x = ReadDouble("коорд. X (-1000..1000)", -1000, 1000);
        double y = ReadDouble("коорд. Y (-1000..1000)", -1000, 1000);
        double z = ReadDouble("коорд. Z (0..8848)", 0, 8848);
        c.Position = new Coordinates(x, y, z);
        Console.Write("экипировка: имя (пусто — нет): ");
        var ename = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(ename))
        {
            double w = ReadDouble("вес (кг, 0..50)", 0, 50);
            int d = ReadInt("прочность (0..100)", 0, 100);
            c.PersonalEquipment = new Equipment { Name = ename.Trim(), WeightKg = w, Durability = d };
        }
        else
        {
            c.PersonalEquipment = null;
        }

        return c;
    }

    // CSV
    static void LoadCsv(string path)
    {
        if (!File.Exists(path)) { Console.WriteLine($"[info] Файл '{path}' не найден. коллекция стартует пустой."); return; }
        using (var sr = new StreamReader(path))
        {
            string? header = sr.ReadLine();
            if (header == null) return;
            int lineNo = 1;
            string? line;
            while ((line = sr.ReadLine()) != null)
            {
                lineNo++;
                var cols = SplitCsv(line);
                if (cols.Count < 15) { Console.WriteLine($"[warn] строка {lineNo}: мало столбцов"); continue; }
                try
                {
                    var cl = new Climber();
                    cl.Id = int.Parse(cols[0]);
                    if (db.ContainsKey(cl.Id)) { Console.WriteLine($"[warn] дубликат Id {cl.Id} пропущен"); continue; }
                    cl.Name = cols[1];
                    cl.Status = cols[2];
                    cl.Role = (Role)Enum.Parse(typeof(Role), cols[3]);
                    cl.LastKnownWeather = (WeatherCondition)Enum.Parse(typeof(WeatherCondition), cols[4]);
                    cl.Energy = double.Parse(cols[5], CI);
                    cl.Altitude = int.Parse(cols[6], CI);
                    cl.Experience = double.Parse(cols[7], CI);
                    string eqName = cols[8];
                    string eqW = cols[9];
                    string eqD = cols[10];
                    if (!string.IsNullOrWhiteSpace(eqName))
                    {
                        cl.PersonalEquipment = new Equipment
                        {
                            Name = eqName,
                            WeightKg = string.IsNullOrWhiteSpace(eqW) ? 0 : double.Parse(eqW, CI),
                            Durability = string.IsNullOrWhiteSpace(eqD) ? 0 : int.Parse(eqD, CI)
                        };
                    }
                    else cl.PersonalEquipment = null;
                    double cx = double.Parse(cols[11], CI);
                    double cy = double.Parse(cols[12], CI);
                    double cz = double.Parse(cols[13], CI);
                    cl.Position = new Coordinates(cx, cy, cz);
                    cl.CreatedAt = DateTime.Parse(cols[14], CI);
                    db[cl.Id] = cl;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[warn] строка {lineNo}: {ex.Message}");
                }
            }
        }
        Console.WriteLine($"загружено: {db.Count} элементов из {path}");
    }

    static void SaveCsv(string path)
    {
        using (var sw = new StreamWriter(path, false))
        {
            sw.WriteLine("Id,Name,Status,Role,Weather,Energy,Altitude,Experience,EquipName,EquipWeight,EquipDurability,CoordX,CoordY,CoordZ,CreatedAt");
            foreach (var cl in db.Values.OrderBy(v => v.Id))
            {
                string eqName = cl.PersonalEquipment?.Name ?? "";
                string eqW = cl.PersonalEquipment?.WeightKg.ToString(CI) ?? "";
                string eqD = cl.PersonalEquipment?.Durability.ToString(CI) ?? "";

                sw.WriteLine(string.Join(",",
                    cl.Id,
                    EscapeCsv(cl.Name),
                    EscapeCsv(cl.Status),
                    cl.Role,
                    cl.LastKnownWeather,
                    cl.Energy.ToString(CI),
                    cl.Altitude.ToString(CI),
                    cl.Experience.ToString(CI),
                    EscapeCsv(eqName),
                    eqW,
                    eqD,
                    cl.Position.X.ToString(CI),
                    cl.Position.Y.ToString(CI),
                    cl.Position.Z.ToString(CI),
                    cl.CreatedAt.ToString(CI)
                ));
            }
        }
    }

    static void ExecuteScript(string fileName, DateTime initTime)
    {
        if (!File.Exists(fileName)) { Console.WriteLine("файл не найден"); return; }
        var lines = File.ReadAllLines(fileName);
        Console.WriteLine($"[exec] выполняю {fileName}, строк: {lines.Length}");
        foreach (var raw in lines)
        {
            var line = raw?.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
            Console.WriteLine($"> {line}");
            if (!HandleCommand(line, initTime)) break;
        }
    }

    static List<string> SplitCmd(string line)
    {
        var list = new List<string>();
        foreach (var p in line.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries))
            list.Add(p.Trim());
        if (list.Count == 1) list.Add(string.Empty);
        return list;
    }

    static List<string> SplitCsv(string line)
    {
        var parts = new List<string>();
        foreach (var p in line.Split(',')) parts.Add(p.Trim());
        return parts;
    }

    static string EscapeCsv(string s)
    {
        if (s == null) return "";
        if (s.Contains(",") || s.Contains("\""))
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        return s;
    }

    static void PrintHelp()
    {
        Console.WriteLine("команды:");
        Console.WriteLine(" help - справка");
        Console.WriteLine(" info - информация о коллекции");
        Console.WriteLine(" show - вывести все элементы");
        Console.WriteLine(" insert - добавить элемент");
        Console.WriteLine(" update id - обновить элемент по id");
        Console.WriteLine(" remove_key id - удалить по ключу (id)");
        Console.WriteLine(" clear - очистить коллекцию");
        Console.WriteLine(" save - сохранить в CSV");
        Console.WriteLine(" execute_script file - выполнить команды из файла");
        Console.WriteLine(" exit - выход");
        Console.WriteLine("доп. команды (вариант 10):");
        Console.WriteLine(" filter_role {Leader|Medic|Climber|Support}");
        Console.WriteLine(" group_by_role");
        Console.WriteLine(" count");
    }

    static string ReadRequiredString(string prompt)
    {
        while (true)
        {
            Console.Write($"{prompt}: ");
            var s = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(s)) return s.Trim();
            Console.WriteLine("поле не может быть пустым");
        }
    }

    static string ReadOptionalString(string prompt)
    {
        Console.Write($"{prompt}: ");
        var s = Console.ReadLine();
        return s?.Trim() ?? "";
    }

    static TEnum ReadEnum<TEnum>(string prompt, string[] names) where TEnum : struct
    {
        while (true)
        {
            Console.WriteLine($"{prompt} [{string.Join(", ", names)}]: ");
            var s = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(s) && Enum.TryParse<TEnum>(s.Trim(), out var val))
                return val;
            Console.WriteLine("неверное значение");
        }
    }

    static double ReadDouble(string prompt, double min, double max)
    {
        while (true)
        {
            Console.Write($"{prompt}: ");
            var s = Console.ReadLine();
            if (double.TryParse(s, NumberStyles.Float, CI, out var val) && val >= min && val <= max)
                return val;
            Console.WriteLine($"должно быть числом в диапазоне [{min}; {max}]");
        }
    }

    static int ReadInt(string prompt, int min, int max)
    {
        while (true)
        {
            Console.Write($"{prompt}: ");
            var s = Console.ReadLine();
            if (int.TryParse(s, NumberStyles.Integer, CI, out var val) && val >= min && val <= max)
                return val;
            Console.WriteLine($"должно быть целым в диапазоне [{min}; {max}]");
        }
    }
}
