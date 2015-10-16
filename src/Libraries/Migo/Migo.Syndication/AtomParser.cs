//
// AtomParser.cs
//
// Authors:
//   Joseph Benden <joe@thrallingpenguin.com>
//
// Copyright (C) 2015 Thralling Penguin LLC.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Xml;
using System.Text;
using System.Collections.Generic;

using Hyena;

namespace Migo.Syndication
{
    public class AtomParser : FeedParser
    {
        private XmlDocument doc;
        private string url;

        public AtomParser (string url, string xml)
        {
            this.url = url;
            xml = xml.TrimStart ();
            doc = new XmlDocument ();
            // Don't resolve any external references, see bgo#601554
            doc.XmlResolver = null;

            try {
                doc.LoadXml (xml);
            } catch (XmlException e) {
                bool have_stripped_control = false;
                StringBuilder sb = new StringBuilder ();

                foreach (char c in xml) {
                    if (Char.IsControl (c) && c != '\n') {
                        have_stripped_control = true;
                    } else {
                        sb.Append (c);
                    }
                }

                bool loaded = false;
                if (have_stripped_control) {
                    try {
                        doc.LoadXml (sb.ToString ());
                        loaded = true;
                    } catch (Exception) {
                    }
                }

                if (!loaded) {
                    Hyena.Log.Exception (e);
                    throw new FormatException ("Invalid XML document.");
                }
            }

            // initialize all Xml namespaces
            mgr = new XmlNamespaceManager (doc.NameTable);
            mgr.AddNamespace ("media", "http://search.yahoo.com/mrss/");
            mgr.AddNamespace ("yt", "http://www.youtube.com/xml/schemas/2015");
            mgr.AddNamespace ("atom", "http://www.w3.org/2005/Atom");
        }

        public AtomParser (string url, XmlDocument doc)
        {
            this.url = url;
            this.doc = doc;
        }

        public override Feed CreateFeed ()
        {
            return UpdateFeed (new Feed ());
        }

        public override Feed UpdateFeed (Feed feed)
        {
            try {
                if (feed.Title == null || feed.Title.Trim () == "" || feed.Title == Mono.Unix.Catalog.GetString ("Unknown Podcast")) {
                    feed.Title = StringUtil.RemoveNewlines (GetXmlNodeText (doc, "//atom:title"));

                    if (String.IsNullOrEmpty (feed.Title)) {
                        feed.Title = Mono.Unix.Catalog.GetString ("Unknown Podcast");
                    }
                }

                feed.Description      = StringUtil.RemoveNewlines (GetXmlNodeText (doc, "//atom:title"));
                feed.Copyright        = "";
                feed.LastBuildDate    = GetRfc822DateTime (doc, "//atom:published");
                feed.Link             = GetXmlNodeText (doc, "//atom:author/atom:uri");
                feed.PubDate          = GetRfc822DateTime (doc, "//atom:published");
                feed.Keywords         = feed.Description;

                return feed;
            } catch (Exception e) {
                 Hyena.Log.Exception ("Caught error parsing Atom channel", e);
            }

            return null;
        }

        public override IEnumerable<FeedItem> GetFeedItems (Feed feed)
        {
            XmlNodeList nodes = null;
            try {
                nodes = doc.SelectNodes ("//atom:entry", mgr);
            } catch (Exception e) {
                Hyena.Log.Exception ("Unable to get any RSS items", e);
            }

            if (nodes != null) {
                foreach (XmlNode node in nodes) {
                    FeedItem item = null;

                    try {
                        item = ParseItem (node);
                        if (item != null) {
                            item.Feed = feed;
                        }
                    } catch (Exception e) {
                        Hyena.Log.Exception (e);
                    }

                    if (item != null) {
                        yield return item;
                    }
                }
            }
        }

        private FeedItem ParseItem (XmlNode node)
        {
            try {
                FeedItem item = new FeedItem ();

                item.Description = StringUtil.RemoveNewlines (GetXmlNodeText (node, "media:group/media:description"));
                item.UpdateStrippedDescription ();
                item.Title = StringUtil.RemoveNewlines (GetXmlNodeText (node, "media:group/media:title"));

                if (String.IsNullOrEmpty (item.Description) && String.IsNullOrEmpty (item.Title)) {
                    throw new FormatException ("node:  Either 'title' or 'description' node must exist in Atom document.");
                }

                item.Author     = GetXmlNodeText (node, "atom:author/atom:name");
                item.Comments   = "";
                item.Link       = GetXmlNodeText (node, "atom:link");
                item.PubDate    = GetRfc822DateTime (node, "atom:published");
                item.Modified   = GetRfc822DateTime (node, "atom:updated");
                item.LicenseUri = "";

                XmlNode media_group = node.SelectSingleNode ("media:group", mgr);
                item.Enclosure      = ParseMediaContent (media_group);

                return item;
             } catch (Exception e) {
                 Hyena.Log.Exception ("Caught error parsing Atom item", e);
             }

             return null;
        }

        public override bool CanParse ()
        {
            if (doc.SelectSingleNode ("/atom:feed", mgr) == null) {
                return false;
            }

            if (doc.SelectSingleNode ("//atom:title", mgr) == null) {
                return false;
            }

            return true;
        }

    }
}
