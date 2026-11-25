using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
namespace RepairToolkit
{
    public class Entry
    {
        public string Title, TitlePl, Description, DescriptionPl, Code, Shell, Risk, Quick, Param, ParamLabel, ParamLabelPl, ParamDefault, Special;
        public bool Admin, CopyOnly, Launch, NoRp;   // NoRp: no restore point first (read-only or non-system commands)
        public Category Category;

        public string Name
        {
            get { return (Loc.Pl && !string.IsNullOrEmpty(TitlePl)) ? TitlePl : Title; }
        }

        public string Desc
        {
            get { return (Loc.Pl && !string.IsNullOrEmpty(DescriptionPl)) ? DescriptionPl : Description; }
        }

        public string SearchText
        {
            get { return Title + " " + TitlePl + " " + Description + " " + DescriptionPl + " " + Code + " " + Shell + " " + Category.Title + " " + Category.TitlePl; }
        }
    }

    public class Category
    {
        public string Id, Title, TitlePl, Group, Description, DescriptionPl;
        public bool Hidden;   // not shown in the tree (holds entries used only by buttons, e.g. the system report)
        public List<Entry> Entries = new List<Entry>();
        public List<KeyValuePair<string, string>> Blocks = new List<KeyValuePair<string, string>>();
        public List<KeyValuePair<string, string>> BlocksPl = new List<KeyValuePair<string, string>>();

        public string Name
        {
            get { return (Loc.Pl && !string.IsNullOrEmpty(TitlePl)) ? TitlePl : Title; }
        }

        public string Desc
        {
            get { return (Loc.Pl && !string.IsNullOrEmpty(DescriptionPl)) ? DescriptionPl : Description; }
        }

        public List<KeyValuePair<string, string>> ActiveBlocks
        {
            get { return (Loc.Pl && BlocksPl.Count > 0) ? BlocksPl : Blocks; }
        }

        public string BlockText
        {
            get
            {
                return string.Join(" ", Blocks.Select(b => b.Value).ToArray()) + " " + string.Join(" ", BlocksPl.Select(b => b.Value).ToArray());
            }
        }

        public override string ToString() { return Name; }
    }

    public static class Catalog
    {
        public static List<Category> Load()
        {
            using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("Catalog.xml"))
            {
                var doc = XDocument.Load(s);
                var result = new List<Category>();
                foreach (var c in doc.Root.Elements("category"))
                {
                    var cat = new Category
                    {
                        Id = (string)c.Attribute("id"),
                        Title = (string)c.Attribute("title"),
                        TitlePl = (string)c.Attribute("titlePl"),
                        Group = (string)c.Attribute("group"),
                        Description = (string)c.Attribute("description"),
                        DescriptionPl = (string)c.Attribute("descriptionPl"),
                        Hidden = (string)c.Attribute("hidden") == "true"
                    };
                    foreach (var e in c.Elements("command"))
                    {
                        cat.Entries.Add(new Entry
                        {
                            Title = (string)e.Attribute("title"),
                            TitlePl = (string)e.Attribute("titlePl"),
                            Description = (string)e.Element("description"),
                            DescriptionPl = (string)e.Element("descriptionPl"),
                            Code = (string)e.Element("code"),
                            Shell = (string)e.Attribute("shell") ?? "",
                            Risk = (string)e.Attribute("risk") ?? "info",
                            Quick = (string)e.Attribute("quick"),
                            Param = (string)e.Attribute("param"),
                            ParamLabel = (string)e.Attribute("paramLabel"),
                            ParamLabelPl = (string)e.Attribute("paramLabelPl"),
                            ParamDefault = (string)e.Attribute("paramDefault"),
                            Special = (string)e.Attribute("special"),
                            Admin = (string)e.Attribute("admin") == "true",
                            CopyOnly = (string)e.Attribute("copyonly") == "true",
                            Launch = (string)e.Attribute("launch") == "true",
                            NoRp = (string)e.Attribute("norp") == "true",
                            Category = cat
                        });
                    }
                    foreach (var r in c.Elements("reference"))
                    {
                        bool pl = (string)r.Attribute("lang") == "pl";
                        foreach (var b in r.Elements("block"))
                        {
                            var kv = new KeyValuePair<string, string>((string)b.Attribute("kind"), b.Value);
                            if (pl) cat.BlocksPl.Add(kv); else cat.Blocks.Add(kv);
                        }
                    }
                    result.Add(cat);
                }
                return result;
            }
        }
    }
}
