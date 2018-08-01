using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedditWritesFanfic
{
    class ChapterDictionary
    {
        string CurrentFile = null;
        public Dictionary<string, Chapter> Chapters { get; private set; } = new Dictionary<string, Chapter>();

        public ChapterDictionary(string fileName) : this()
        {
            CurrentFile = fileName;
            Load();
        }

        public ChapterDictionary()
        {

        }

        private void Load()
        {
            if (!File.Exists(CurrentFile))
            {
                Console.WriteLine("Warning: File {0} doesn't exist!", CurrentFile);
                return;
            }

            var tree = File.ReadAllText(CurrentFile, Encoding.UTF8);
            Chapters = JsonConvert.DeserializeObject<Dictionary<string, Chapter>>(tree);
        }

        public void Save()
        {
            while (Console.KeyAvailable)
            {
                if (Console.ReadKey(true).KeyChar == 'x')
                {
                    PrintState();
                }
            }

            if (CurrentFile == null)
            {
                Console.WriteLine("No file given, not saving!");
                return;
            }

            File.WriteAllText(CurrentFile, JsonConvert.SerializeObject(Chapters), Encoding.UTF8);
        }


        public IEnumerable<Chapter> EnumerateChildren(IEnumerable<string> input)
        {
            return input.Where(a => Chapters.ContainsKey(a)).Select(a => Chapters[a]);
        }

        private Chapter GetChapter(string id)
        {
            if (Chapters.TryGetValue(id, out var val))
                return val;

            return null;
        }

        public void PrintState()
        {
            foreach (var chapter in Chapters.Values.Where(a => a.ParentId == null))
            {
                PrintAndValidateTree(chapter.Id, null, 0);
            }
        }

        private void PrintAndValidateTree(string head, string fromParent, int depth)
        {
            var chapter = Chapters[head];

            if (fromParent != chapter.ParentId)
                throw new Exception("Invalid Tree!");

            string resLine = "";
            resLine = resLine.PadLeft(2 * depth);

            if (depth != 0)
            {
                resLine += "|--- ";
            }

            resLine += "[" + chapter.Id + "] " + chapter.PostTitle;

            if (chapter.ShouldUpdate)
                Console.ForegroundColor = ConsoleColor.Yellow;

            if (chapter.IsPoisoned)
                Console.ForegroundColor = ConsoleColor.Red;

            Console.WriteLine(resLine);

            foreach (var child in chapter.Children)
            {
                PrintAndValidateTree(child, head, depth + 1);
            }

            Console.ResetColor();
        }
    }
}
