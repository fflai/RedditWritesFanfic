using RedditSharp;
using RedditSharp.Things;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace RedditWritesFanfic
{
    class Chapter
    {
        public string Subreddit { get; set; }

        public string ID { get; set; }

        public string ParentId { get; set; }

        public string PostTitle { get; set; }

        public string Author { get; set; }

        public HashSet<string> Children { get; set; } = new HashSet<string>();

        public DateTime LastUpdate { get; set; }

        public string CommentId { get; set; }

        public string RedditLink => $"https://reddit.com/r/{Subreddit}/comments/{ID}/";

        public bool IsPoisoned { get; set; }
        
        public Chapter()
        {

        }

        
        public bool Update(Reddit reddit, ChapterDictionary dict)
        {
            var myPost = reddit.GetPost(new Uri(RedditLink));
            var shouldUpdateComment = false;

            var parentId = GetFirstChapterId(myPost.SelfText);
            if (ParentId != null && ParentId != parentId)
            {
                if (dict.Chapters.TryGetValue(ParentId, out var oldParent))
                    oldParent.Children.Remove(ID);
            }

            ParentId = parentId;
            if (parentId != null && dict.Chapters.TryGetValue(ParentId, out var newParent))
            {
                if (newParent.Children.Add(ID))
                {
                    shouldUpdateComment = true;
                }
            }

            return shouldUpdateComment;
        }
        public void Update(Post post, ChapterDictionary dict)
        {

        }

        public List<Chapter> UpdateOrCreateComment(Reddit reddit, ChapterDictionary dict)
        {
            Console.WriteLine("Updating the stickied post for this comment.");
            var text = GetStickyText(dict);

            List<Chapter> res = new List<Chapter>();
            res.Add(this);

            if (CommentId == null)
            {
                var post = reddit.GetPost(new Uri(RedditLink));
                var comment = post.Comment(text);
                comment.Distinguish(VotableThing.DistinguishType.Moderator);
                CommentId = comment.Id;
            }
            else
            {
                var comment = (Comment)reddit.GetThingByFullname(CommentId);
                comment.EditText(text);
                comment.Save();
            }

            if (ParentId != null && dict.Chapters.TryGetValue(ParentId, out var parent))
            {
                res.AddRange(parent.UpdateStickiedComment(reddit, dict));
            }

            return res;
        }

        private string GetStickyText(ChapterDictionary dict)
        {
            if (Children.Count == 0)
                return "There don't seem to be any following chapters";

            var sb = new StringBuilder();
            sb.AppendLine("## Next Chapters:");
            sb.AppendLine("");
            foreach (var child in dict.EnumarteChildren(Children))
            {
                sb.AppendFormat("* **[{0}]({1})** by /u/{2} - {3} Chapters deep!\n", child.PostTitle, child.RedditLink, child.Author, child.GetDepth(dict) + 1);
            }

            return sb.ToString();
        }

        public int GetDepth(ChapterDictionary dict)
        {
            if (Children.Count == 0)
                return 0;

            return dict.EnumarteChildren(Children).Max(a => a.GetDepth(dict)) + 1;
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
