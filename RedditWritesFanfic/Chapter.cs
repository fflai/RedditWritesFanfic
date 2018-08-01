using Newtonsoft.Json;
using RedditSharp;
using RedditSharp.Things;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static RedditSharp.Things.ModeratableThing;

namespace RedditWritesFanfic
{
    class Chapter
    {
        public string Subreddit { get; set; }

        public string Id { get; set; }

        public string ParentId { get; set; }

        public string PostTitle { get; set; }

        public string Author { get; set; }

        public HashSet<string> Children { get; set; } = new HashSet<string>();

        public DateTime LastUpdate { get; set; }

        public string CommentId { get; set; }
        
        public bool IsPoisoned { get; set; }


        [JsonIgnore]
        public string RedditLink => $"https://reddit.com/r/{Subreddit}/comments/{Id}/";


        [JsonIgnore]
        public string CommentLink => $"https://www.reddit.com/r/{Subreddit}/comments/{Id}/foo/{CommentId}/";

        [JsonIgnore]
        public bool ShouldUpdate => (DateTime.Now - LastUpdate).TotalMinutes > 0;


        public Chapter()
        {
        }

        
        public async Task UpdateAsyc(Reddit reddit, ChapterDictionary dict)
        {
            var myPost = await reddit.GetPostAsync(new Uri(RedditLink));

            var parentId = GetFirstChapterId(myPost.SelfText);
            if (ParentId != null && ParentId != parentId)
            {
                if (dict.Chapters.TryGetValue(ParentId, out var oldParent))
                {
                    oldParent.PoisonSelf(dict);
                    oldParent.Children.Remove(Id);
                }
            }

            ParentId = parentId;
            if (parentId != null && dict.Chapters.TryGetValue(ParentId, out var newParent))
            {
                if (newParent.Children.Add(Id))
                    newParent.PoisonSelf(dict);
            }

            if (CommentId == null)
                PoisonSelf(dict);

            LastUpdate = DateTime.Now;
        }

        public async Task UpdateOrCreateComment(Reddit reddit, ChapterDictionary dict)
        {
            Console.WriteLine("Updating comment for: {0} ({1})", Id, PostTitle);
            var text = GetStickyText(dict);

            List<Chapter> res = new List<Chapter>();
            res.Add(this);

            if (CommentId == null)
            {
                Console.Write("Creating new comment for {0}... ", Id);
                var post = await reddit.GetPostAsync(new Uri(RedditLink));
                var comment = await post.CommentAsync(text);
                await comment.DistinguishAsync(DistinguishType.Moderator, true);
                CommentId = comment.Id;
                Console.WriteLine("Done! CommendId is {0}", CommentId);
            }
            else
            {
                var post = await reddit.GetPostAsync(new Uri(RedditLink));
                var comments = await post.GetCommentsAsync(5, CommentSort.Best);

                var myComment = comments.SingleOrDefault(a => a.Id == CommentId);
                
                if (myComment == null)
                {
                    Console.WriteLine("Couldn't find our comment {0} for {1}, recreating.", CommentId, Id);
                    CommentId = null;
                    await UpdateOrCreateComment(reddit, dict);
                    return;
                }

                if (myComment.Distinguished != DistinguishType.Moderator)
                    await myComment.DistinguishAsync(DistinguishType.Moderator, true);
                
                if (myComment.Body != text)
                {
                    Console.WriteLine("Actually updating comment {0} for post {1}", CommentId, Id);
                    await (myComment).EditTextAsync(text);
                }
                else
                {
                    Console.WriteLine("Comment {0} for post {1} has not changed - ignoring :)", CommentId, Id);
                }
            }

            IsPoisoned = false;
        }

        private string GetStickyText(ChapterDictionary dict)
        {
            var sb = new StringBuilder();

            sb.AppendLine("[Disclaimer](https://www.reddit.com/r/RedditWritesFanfic/comments/93ps6d/disclaimer/)");
            sb.AppendLine();

            var urlencodedLink = Uri.EscapeDataString(RedditLink);
            var link = $"https://www.reddit.com/r/{Subreddit}/submit?selftext=true&text=[Previous%20Chapter]({urlencodedLink})%20%0A%0AWrite%20your%20story%20here!&title=Chapter%20Title";
            sb.AppendLine($"## [Continue this story!]({link})");
            
            if (Children.Count == 0)
            {
                return sb.ToString();
            }

            sb.AppendLine("-----");
            sb.AppendLine();
            sb.AppendLine("## Next Chapters:");
            sb.AppendLine("");
            foreach (var child in dict.EnumerateChildren(Children))
            {
                sb.AppendFormat("* **[{0}]({1})** by /u/{2} - {3} Chapters deep!\n", child.PostTitle, child.RedditLink, child.Author, child.GetDepth(dict) + 1);
            }

            return sb.ToString();
        }

        private void PoisonSelf(ChapterDictionary dict)
        {
            IsPoisoned = true;
            if (ParentId != null && dict.Chapters.TryGetValue(ParentId, out var parent))
                parent.PoisonSelf(dict);
        }

        public int GetDepth(ChapterDictionary dict)
        {
            if (Children.Count == 0)
                return 0;

            return dict.EnumerateChildren(Children).Max(a => a.GetDepth(dict)) + 1;
        }
        
        private string GetFirstChapterId(string text)
        {
            var regex = new Regex("/r/" + Subreddit + "/comments/(([0-9]|[a-z]|[A-Z])+)/");

            var match = regex.Match(text);
            if (!match.Success)
                return null;

            return match.Groups[1].Value;
        }
        
    }
}
