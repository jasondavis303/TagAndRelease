using CommandLine;
using MimeTypes;
using Octokit;
using System;
using System.Collections.Generic;
using System.IO;
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
                Parser.Default.ParseArguments<CLOptions>(args).WithParsed(opts =>
                {
                    RunAsync(opts).Wait();
                    ret = 0;
                });
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
                opts.Version = TimestampVersion.Generator.Generate().ToString();
            
            if (!opts.Version.StartsWith("v", StringComparison.CurrentCultureIgnoreCase))
                opts.Version = "v" + opts.Version;

            if (opts.Verbose)
                Console.WriteLine($"Release: {opts.Version}");

            var client = new GitHubClient(new ProductHeaderValue("jasondavis303.TagAndRelease"));
            var tokenAuth = new Credentials(opts.GithubToken);
            client.Credentials = tokenAuth;


            Release release = null;
            try
            {
                if (opts.Verbose)
                    Console.WriteLine("Checking for release");
                release = await client.Repository.Release.Get(opts.Owner, opts.RepoName, opts.Version);
            }
            catch { }

            if (release == null)
            {
                if (opts.Verbose)
                    Console.WriteLine("Creating tag");
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

                if (opts.Verbose)
                    Console.WriteLine("Creating release");

                release = await client.Repository.Release.Create(opts.Owner, opts.RepoName, new NewRelease(opts.Version));
            }
            else
            {
                if (opts.Verbose)
                    Console.WriteLine(" - Release already exists");
            }



            foreach (string asset in opts.Assets)
            {
                string filename = Path.GetFileName(asset);

                if(opts.Verbose)
                    Console.WriteLine("Processing asset: {0}", filename);

                if (opts.Verbose)
                    Console.WriteLine("Checking for asset");
                var existingAsset = release.Assets.FirstOrDefault(item => item.Name.Equals(filename, StringComparison.CurrentCultureIgnoreCase));
                if (existingAsset != null)
                {
                    if (opts.Verbose)
                        Console.WriteLine(" - Asset already exists");
                    if (opts.Overwrite)
                    {
                        if (opts.Verbose)
                            Console.WriteLine("Deleting asset");
                        await client.Repository.Release.DeleteAsset(opts.Owner, opts.RepoName, existingAsset.Id);
                        existingAsset = null;
                    }
                }

                if (existingAsset == null)
                {
                    if (opts.Verbose)
                        Console.WriteLine("Uploading asset");
                    string mimeType = MimeTypeMap.GetMimeType(Path.GetExtension(asset));
                    using var fs = File.OpenRead(asset);
                    var assetUpload = new ReleaseAssetUpload(filename, mimeType, fs, null);
                    var newAsset = await client.Repository.Release.UploadAsset(release, assetUpload);
                }
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
