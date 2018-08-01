using RedditSharp;
using RedditSharp.Things;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RedditWritesFanfic
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: RedditWritesFanfic.exe <botFile> <subreddit>");
            }

            var res = Task.Run(async () => await MainAsync(args));
            res.Wait();
        }

        static async Task MainAsync(string[] args)
        {
            var login = File.ReadAllLines(args[0]);
            string subreddit = args[1];

            var wa = new BotWebAgent(login[0], login[1], login[2], login[3], login[4]);
            Reddit reddit = new Reddit(wa, true);
            
            ChapterDictionary dict = new ChapterDictionary(subreddit + ".json");
            Console.WriteLine("Current State: ");
            dict.PrintState();

            while (true)
            {
                await AddNewPostsAsync(dict, reddit, subreddit);
                dict.Save();

                await UpdateChaptersAsync(dict, reddit, subreddit);
                dict.Save();

                await UpdatePoisonedChaptersAsync(dict, reddit, subreddit);
                dict.Save();


                Console.WriteLine("Sleeping.");
                Thread.Sleep(2000);
            }
        }

        private static async Task AddNewPostsAsync(ChapterDictionary dict, Reddit reddit, string subreddit)
        {
            Console.WriteLine($"Searching the subreddit { subreddit } for new posts.");

            var sub = await reddit.GetSubredditAsync(subreddit);
            var posts = await sub.GetPosts(Subreddit.Sort.New, 20).ToArray();

            foreach (var post in posts)
            {
                if (!dict.Chapters.ContainsKey(post.Id) && post.LinkFlairCssClass == "chapter")
                {
                    Console.WriteLine("Creating Chapter: {0}", post.Id);
                    var intThread = new Chapter()
                    {
                        Author = post.AuthorName,
                        PostTitle = post.Title,
                        Subreddit = post.SubredditName,
                        Id = post.Id,
                        LastUpdate = DateTime.MinValue,
                    };
                    dict.Chapters[intThread.Id] = intThread;
                }
            }
        }

        private static async Task UpdateChaptersAsync(ChapterDictionary dict, Reddit reddit, string subreddit)
        {
            Console.WriteLine($"Updating chapters in { subreddit }.");
            foreach (var chapter in dict.Chapters.Values)
            {
                if (chapter.ShouldUpdate)
                {
                    try
                    {
                        await chapter.UpdateAsyc(reddit, dict);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error when updating chapter " + chapter.Id + ": " + e.ToString());
                    }
                }
            }
        }

        private static async Task UpdatePoisonedChaptersAsync(ChapterDictionary dict, Reddit reddit, string subreddit)
        {
            Console.WriteLine($"Updating chapters in { subreddit }.");
            foreach (var chapter in dict.Chapters.Values)
            {
                if (chapter.IsPoisoned)
                {
                    try
                    {
                        await chapter.UpdateOrCreateComment(reddit, dict);
                        dict.Save();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Exception when updating comment of" + chapter.Id + ": " + e.ToString());
                    }
                }
            }
        }
    }
}
