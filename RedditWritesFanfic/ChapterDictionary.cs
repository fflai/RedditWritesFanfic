using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedditWritesFanfic
{
    class ChapterDictionary
    {
        public Dictionary<string, Chapter> Chapters { get; private set; } = new Dictionary<string, Chapter>();

        public IEnumerable<Chapter> EnumarteChildren(IEnumerable<string> input)
        {
            return input.Where(a => Chapters.ContainsKey(a)).Select(a => Chapters[a]);
        }

        private Chapter GetChapter(string id)
        {
            if (Chapters.TryGetValue(id, out var val))
                return val;

            return null;
        }
    }
}
