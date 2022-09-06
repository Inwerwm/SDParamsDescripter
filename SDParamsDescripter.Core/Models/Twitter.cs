using LinqToTwitter;
using LinqToTwitter.Common;
using LinqToTwitter.OAuth;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace SDParamsDescripter.Core.Models;
public class Twitter : IDisposable
{
    private bool disposedValue;

    private TwitterContext Context
    {
        get;
    }

    public static string ExpandExceptionMessage(TwitterQueryException ex)
    {
        var errors = string.Join(Environment.NewLine, ex.Errors.Select(e => $"- {e.Code}: {e.Message}"));
        return ex.ReasonPhrase is null ? errors : $"{ex.ReasonPhrase}:{Environment.NewLine}{errors}";
    }

    public Twitter(string consumerKey, string consumerSecret, string accessToken, string accessTokenSecret)
    {
        var auth = new SingleUserAuthorizer
        {
            CredentialStore = new SingleUserInMemoryCredentialStore
            {
                ConsumerKey = consumerKey,
                ConsumerSecret = consumerSecret,
                AccessToken = accessToken,
                AccessTokenSecret = accessTokenSecret
            }
        };

        Context = new TwitterContext(auth);
    }

    public async Task TweetWithMedia(string text, string imagePath, string imageAltText, bool resizeImageWhenTooLarge)
    {
        var image = await ReadImage(imagePath);
        await TweetWithMedia(text, image, imageAltText, resizeImageWhenTooLarge);

    }

    private async Task TweetWithMedia(string text, byte[] image, string imageAltText, bool resizeImageWhenTooLarge)
    {
        var doRetry = false;

        try
        {
            var media = await Context.UploadMediaAsync(image, "image/png", "tweet_image");
            await Context.CreateMediaMetadataAsync(media.MediaID, imageAltText);
            await Context.TweetMediaAsync(text, new[] { media.MediaID.ToString() });
        }
        catch (TwitterQueryException ex)
        {
            var isLargeImageError = (ex.Errors.Count == 1 && ex.Errors[0].Message == "File size exceeds 5242880 bytes.");

            if (isLargeImageError && resizeImageWhenTooLarge)
            {
                doRetry = true;
            }
            else
            {
                throw;
            }
        }

        if (doRetry)
        {
            await TweetWithMedia(text, Resize(image, 0.9f), imageAltText, resizeImageWhenTooLarge);
        }
    }

    private byte[] Resize(byte[] imageBytes, float scale)
    {
        using var image = Image.Load(imageBytes);
        image.Mutate(img => img.Resize((int)Math.Round(image.Width * scale), (int)Math.Round(image.Height * scale)));

        using var memory = new MemoryStream();
        image.SaveAsPng(memory);
        return memory.ToArray();
    }

    private async Task<byte[]> ReadImage(string path)
    {
        if (TryReadImage(path, out var image)) return image;

        await Task.Delay(500);
        return await ReadImage(path);
    }

    private bool TryReadImage(string path, out byte[] image)
    {
        try
        {
            image = File.ReadAllBytes(path);
            return true;
        }
        catch(IOException)
        {
            image = null;
            return false;
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                Context.Dispose();
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
