//
// Youtube.cs
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
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace Migo.Syndication {

    class Youtube {

        internal class Video {

            /// <summary>
            ///   Contains the video type and quality identifier.
            /// </summary>
            private int itag;

            /// <summary>
            ///   Contains the video Mime type.
            /// </summary>
            private string type;

            /// <summary>
            ///   Contains a string representation of the video quality.
            /// </summary>
            private string quality;

            /// <summary>
            ///   Contains the full URL to the video.
            /// </summary>
            private string url;

#region Constructors
            public Video () {
                this.itag = 0;
                this.type = null;
                this.quality = null;
                this.url = null;
            }

            public Video (int itag, string type, string quality, string url) {
                this.itag = itag;
                this.type = type;
                this.quality = quality;
                this.url = url;
            }
#endregion

#region Public Methods
            public int Itag {
                get { return this.itag; }
                internal set { this.itag = value; }
            }

            public string MimeType {
                get { return this.type; }
                internal set { this.type = value; }
            }

            public string Quality {
                get { return this.quality; }
                internal set { this.quality = value; }
            }

            public string Url {
                get { return this.url; }
                internal set { this.url = value; }
            }
#endregion
        }

        /// <summary>
        ///   The video details API end-point.
        /// </summary>
        private readonly string video_metadata_url = "http://www.youtube.com/get_video_info?&video_id";

        /// <summary>
        ///   Contains all decoded video meta details, as a hashmap/Dictionary.
        /// </summary>
        private Dictionary<string, string> post_encoded_vars;

        /// <summary>
        ///   Contains all available video content streams available.
        /// </summary>
        private List<Video> videos;

#region Constructors
        public Youtube (string videoId) {
            string url = String.Format ("{0}={1}", video_metadata_url, videoId);
            WebRequest req = WebRequest.Create (url);
            WebResponse resp = req.GetResponse ();
            StreamReader sr = new StreamReader (resp.GetResponseStream ());

            string post_encoded_video_metadata = sr.ReadToEnd().Trim();

            post_encoded_vars = new Dictionary<string, string>();
            videos = new List<Video>();

            Decode (post_encoded_video_metadata);
        }
#endregion

#region Public Methods
        public string Title {
            get { return post_encoded_vars["title"]; }
        }

        public string ViewCount {
            get { return post_encoded_vars["view_count"]; }
        }

        public string ThumbnailUrl {
            get { return post_encoded_vars["thumbnail_url"]; }
        }

        public string Keywords {
            get { return post_encoded_vars["keywords"]; }
        }

        public string MimeType {
            get { return "video/mp4"; }
        }

        public string Extension {
            get { return "mp4"; }
        }
        
        /// <summary>
        ///   Returns the highest quality WebM video stream available.
        ///
        ///   Note: Youtube does not offer a large variety of WebM video
        ///   streams. It is therefore recommended to use the Mpeg4
        ///   streams instead.
        /// </summary>
        public string GetBestWebm () {
            // find 1080p
            foreach (Video i in videos) {
                if (i.Itag == 46) {
                    return i.Url;
                }
            }

            // find 720p
            foreach (Video i in videos) {
                if (i.Itag == 45) {
                    return i.Url;
                }
            }

            // find 3d 720p
            foreach (Video i in videos) {
                if (i.Itag == 102) {
                    return i.Url;
                }
            }

            // find 480p
            foreach (Video i in videos) {
                if (i.Itag == 44) {
                    return i.Url;
                }
            }

            // Take default, first entry
            return null;
        }

        /// <summary>
        ///   Returns the URL to the highest quality MP4 video stream available.
        /// </summary>
        public string GetBestMpeg4 () {
            // find 1080p
            foreach (Video i in videos) {
                if (i.Itag == 37) {
                    return i.Url;
                }
            }

            // find 720p
            foreach (Video i in videos) {
                if (i.Itag == 22) {
                    return i.Url;
                }
            }

            // find 3d 720p
            foreach (Video i in videos) {
                if (i.Itag == 84) {
                    return i.Url;
                }
            }

            // find 360p
            foreach (Video i in videos) {
                if (i.Itag == 18) {
                    return i.Url;
                }
            }

            return null;
        }
#endregion

        /// <summary>
        ///   Decodes all returned video meta data.
        ///
        ///   Note: The data is returned in form-encoded format, with some
        ///   fields having multiple values. When multiple values are
        ///   supplied, the values are comma-separated and then form-
        ///   encoded.
        /// </summary>
        private void Decode (string postedData) {
            foreach (var item in postedData.Split(new [] { '&' }, StringSplitOptions.RemoveEmptyEntries)) {
                var tokens = item.Split(new [] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length < 2) {
                    continue;
                }
                var paramName = tokens[0].Trim();
                var paramValue = Uri.UnescapeDataString (tokens[1]).Replace('+', ' ').Trim();

                // Are we a hashmap of data?
                var values = paramValue.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (values.Length < 2) {
                    post_encoded_vars[paramName] = paramValue;
                } else {
                    Dictionary<string, string> hash = new Dictionary<string, string>();
                    foreach (var val in values) {
                        foreach (var subitem in val.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries)) {
                            var val_tokens = subitem.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                            if (val_tokens.Length < 2) {
                                post_encoded_vars[paramName] = paramValue;
                            } else {
                                var val_name = val_tokens[0].Trim();
                                var val_value = Uri.UnescapeDataString (val_tokens[1]).Replace('+', ' ').Trim();
                                hash[val_name] = val_value;
                            }
                        }
                        if (hash.Count > 0) {
                            if (paramName == "url_encoded_fmt_stream_map") {
                                // Build a list of all videos available
                                int iTag = 0;
                                
                                Int32.TryParse (hash["itag"], out iTag);

                                Video v = new Video (iTag,
                                                     hash["type"],
                                                     hash["quality"],
                                                     hash["url"]);
                                videos.Add (v);
                            }
                        }
                    }
                }
            }
        }

        /*
        static void Main() {
            Console.WriteLine ("Youtube Downloader is starting...");
            Youtube yt = new Youtube ("1ctYVRB-L4A");

            Console.WriteLine ("Using: {0}", yt.GetBestMpeg4 ());
        }
        */
    }
}
