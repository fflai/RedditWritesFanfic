using RedditSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RedditWritesFanfic
{
    class Program
    {
        public static string CurrentSubreddit = "fflaitest";

        static void Main(string[] args)
        {
            ChapterDictionary dict = new ChapterDictionary();

            Reddit reddit = new Reddit();
            reddit.LogIn("fflai", "FlaiRocks2");
            
            while (true)
            {
                HashSet<Chapter> ToUpdate = new HashSet<Chapter>();

                AddNewPosts(dict, reddit);

                HashSet<Chapter> Updated = new HashSet<Chapter>();
                foreach (var chapter in ToUpdate)
                {
                    if (!Updated.Contains(chapter))
                    {
                        var res = chapter.UpdateStickiedComment(reddit, dict);
                        foreach (var ud in res)
                            Updated.Add(ud);
                    }
                }

                Console.WriteLine("Sleeping.");
                Thread.Sleep(2000);
            }
        }

        private static void AddNewPosts(ChapterDictionary dict, Reddit reddit)
        {
            foreach (var post in reddit.GetSubreddit(CurrentSubreddit).New.Take(20))
            {
                if (!dict.Chapters.ContainsKey(post.Id) && post.LinkFlairCssClass == "chapter")
                {
                    Console.WriteLine("Creating Chapter: {0}", post.Id);
                    var intThread = new Chapter()
                    {
                        Author = post.AuthorName,
                        PostTitle = post.Title,
                        Subreddit = post.SubredditName,
                        ID = post.Id,
                        LastUpdate = DateTime.MinValue,
                    };
                    dict.Chapters[intThread.ID] = intThread;
                }
            }
        }
    }
}
