using CommandLine;
using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace TagAndRelease
{
    class Program
    {
        static int Main(string[] args)
        {
            Console.WriteLine("Tag and Release");
            Console.WriteLine($"v{Assembly.GetExecutingAssembly().GetName().Version}");
            Console.WriteLine();

            int ret = -1;

            try
            {
                Parser.Default.ParseArguments<CLOptions>(args).WithParsed(opts => RunAsync(opts).Wait());
                ret = 0;
            }
            catch (AggregateException ex)
            {
                ShowExceptions(ex.InnerExceptions);
            }
            catch (Exception ex)
            {
                ShowExceptions(new Exception[] { ex });
            }

            return ret;
        }

        static async Task RunAsync(CLOptions opts)
        {
            if (string.IsNullOrWhiteSpace(opts.Version))
            {
                opts.Version = TimestampVersion.Generator.Generate().ToString();
                if (opts.Verbose)
                    Console.WriteLine("Generated Version: {0}", opts.Version);
            }

            if (!opts.Version.StartsWith("v", StringComparison.CurrentCultureIgnoreCase))
                opts.Version = "v" + opts.Version;

            var client = new GitHubClient(new ProductHeaderValue("jasondavis303.TagAndRelease"));
            var tokenAuth = new Credentials(opts.GithubToken);
            client.Credentials = tokenAuth;

            bool tagExists = false;
            try
            {
                if (opts.Verbose)
                    Console.WriteLine("Checking for tag: {0}", opts.Version);
                var existingTag = await client.Git.Tag.Get(opts.Owner, opts.RepoName, opts.Version);
                tagExists = existingTag != null;
            }
            catch { }

            if (!tagExists)
            {
                if (opts.Verbose)
                    Console.WriteLine("Creating tag: {0}", opts.Version);
                var commit = await client.Repository.Commit.Get(opts.Owner, opts.RepoName, opts.Branch);
                var newTag = new NewTag
                {
                    Message = commit.Commit.Message,
                    Object = commit.Sha,
                    Tag = opts.Version,
                    Type = TaggedType.Commit,
                    Tagger = commit.Commit.Author                   
                };
                await client.Git.Tag.Create(opts.Owner, opts.RepoName, newTag);
            }

            
        }

        static void ShowExceptions(IEnumerable<Exception> exes)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Red;
            foreach (var ex in exes)
                Console.WriteLine(ex.Message);
            Console.ResetColor();
            Console.WriteLine();
        }
    }
}
