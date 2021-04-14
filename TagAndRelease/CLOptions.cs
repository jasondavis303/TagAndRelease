using CommandLine;
using System.Collections.Generic;

namespace TagAndRelease
{
    class CLOptions
    {
        [Option("owner", Required = true)]
        public string Owner { get; set; }

        [Option("repo-name", Required = true)]
        public string RepoName { get; set; }

        [Option("branch", Default ="master")]
        public string Branch { get; set; }

        [Option("github-token", Required = true)]
        public string GithubToken { get; set; }

        [Option("set-version", HelpText = "Set the tag/release version. If not specified, it's derived from DateTime.UtcNow")]
        public string Version { get; set; }

        [Option("assets", HelpText = "Assets to add to the release")]
        public IEnumerable<string> Assets { get; set; }

        [Option("overwrite")]
        public bool Overwrite { get; set; }

        [Option("verbose")]
        public bool Verbose { get; set; }
    }
}
