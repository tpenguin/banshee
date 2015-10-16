using System;
using System.Text;
using System.Collections.Generic;
using System.Xml;

using Hyena;

namespace Migo.Syndication
{
    public abstract class FeedParser
    {
        public abstract bool CanParse ();
        public abstract Feed CreateFeed ();
        public abstract Feed UpdateFeed (Feed feed);
        public abstract IEnumerable<FeedItem> GetFeedItems (Feed feed);

        protected XmlNamespaceManager mgr;

#region Static Methods

        static public FeedParser GetMatchingParser (string url, string xml) {
            FeedParser plugin = null;

            // Try Atom
            try {
                plugin = new AtomParser (url, xml);
                if (plugin.CanParse ()) {
                    return plugin;
                }
            } catch (Exception) {}

            // Try Rss
            try {
                plugin = new RssParser (url, xml);
                if (plugin.CanParse ()) {
                    return plugin;
                }
            } catch (Exception) {}

            return null;
        }

#endregion

#region Rss/Atom Feed Parsing Convienience Methods

        protected FeedEnclosure ParseEnclosure (XmlNode node)
        {
            try {
                FeedEnclosure enclosure = new FeedEnclosure ();

                enclosure.Url = GetXmlNodeText (node, "enclosure/@url");
                if (String.IsNullOrEmpty (enclosure.Url)) {
                    return null;
                }

                enclosure.FileSize = Math.Max (0, GetInt64 (node, "enclosure/@length"));
                enclosure.MimeType = GetXmlNodeText (node, "enclosure/@type");
                enclosure.Duration = GetITunesDuration (node);
                enclosure.Keywords = GetXmlNodeText (node, "itunes:keywords");
                return enclosure;
             } catch (Exception e) {
                 Hyena.Log.Exception ("Caught error parsing Xml enclosure", e);
             }

             return null;
        }

        protected TimeSpan GetITunesDuration (XmlNode node)
        {
            return GetITunesDuration (GetXmlNodeText (node, "itunes:duration"));
        }

        protected static TimeSpan GetITunesDuration (string duration)
        {
            if (String.IsNullOrEmpty (duration)) {
                return TimeSpan.Zero;
            }

            try {
                int hours = 0, minutes = 0, seconds = 0;
                string [] parts = duration.Split (':');

                if (parts.Length > 0)
                    seconds = Int32.Parse (parts[parts.Length - 1]);

                if (parts.Length > 1)
                    minutes = Int32.Parse (parts[parts.Length - 2]);

                if (parts.Length > 2)
                    hours = Int32.Parse (parts[parts.Length - 3]);

                return TimeSpan.FromSeconds (hours * 3600 + minutes * 60 + seconds);
            } catch {
                return TimeSpan.Zero;
            }
        }

       // Parse one Media RSS media:content node
        // http://search.yahoo.com/mrss/
        protected FeedEnclosure ParseMediaContent (XmlNode item_node)
        {
            try {
                XmlNode node = null;

                // Get the highest bitrate "full" content item
                // TODO allow a user-preference for a feed to decide what quality to get, if there
                // are options?
                int max_bitrate = 0;
                foreach (XmlNode test_node in item_node.SelectNodes ("media:content", mgr)) {
                    string expr = GetXmlNodeText (test_node, "@expression");
                    if (!(String.IsNullOrEmpty (expr) || expr == "full"))
                        continue;

                    int bitrate = GetInt32 (test_node, "@bitrate");
                    if (node == null || bitrate > max_bitrate) {
                        node = test_node;
                        max_bitrate = bitrate;
                    }
                }

                if (node == null)
                    return null;

                FeedEnclosure enclosure = new FeedEnclosure ();
                enclosure.Url = GetXmlNodeText (node, "@url");
                if (String.IsNullOrEmpty (enclosure.Url)) {
                    return null;
                }

                enclosure.FileSize = Math.Max (0, GetInt64 (node, "@fileSize"));
                enclosure.MimeType = GetXmlNodeText (node, "@type");
                enclosure.Duration = TimeSpan.FromSeconds (GetInt64 (node, "@duration"));
                enclosure.Keywords = GetXmlNodeText (item_node, "itunes:keywords");

                // TODO get the thumbnail URL

                return enclosure;
             } catch (Exception e) {
                 Hyena.Log.Exception ("Caught error parsing RSS media:content", e);
             }

             return null;
        }

#endregion

#region Xml Convienience Methods

        protected string GetXmlNodeText (XmlNode node, string tag)
        {
            XmlNode n = node.SelectSingleNode (tag, mgr);
            return (n == null) ? null : n.InnerText.Trim ();
        }

        protected DateTime GetRfc822DateTime (XmlNode node, string tag)
        {
            DateTime ret = DateTime.MinValue;
            string result = GetXmlNodeText (node, tag);

            if (!String.IsNullOrEmpty (result)) {
                if (Rfc822DateTime.TryParse (result, out ret)) {
                    return ret;
                }

                if (DateTime.TryParse (result, out ret)) {
                    return ret;
                }
            }

            return ret;
        }

        protected long GetInt64 (XmlNode node, string tag)
        {
            long ret = 0;
            string result = GetXmlNodeText (node, tag);

            if (!String.IsNullOrEmpty (result)) {
                Int64.TryParse (result, out ret);
            }

            return ret;
        }

        protected int GetInt32 (XmlNode node, string tag)
        {
            int ret = 0;
            string result = GetXmlNodeText (node, tag);

            if (!String.IsNullOrEmpty (result)) {
                Int32.TryParse (result, out ret);
            }

            return ret;
        }
#endregion

    }
}
