using LinqToTwitter;
using LinqToTwitter.OAuth;

namespace SDParamsDescripter.Core.Models;
public class Twitter : IDisposable
{
    private bool disposedValue;

    private TwitterContext Context
    {
        get;
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

    public async Task TweetWithMedia(string text, string imagePath, string imageAltText)
    {
        var image = await ReadImage(imagePath);
        var media = await Context.UploadMediaAsync(image, "image/png", "tweet_image");
        await Context.CreateMediaMetadataAsync(media.MediaID, imageAltText);
        await Context.TweetMediaAsync(text, new[] { media.MediaID.ToString() });
    }

    public async Task<byte[]> ReadImage(string path)
    {
        if (TryReadImage(path, out var image)) return image;

        await Task.Delay(500);
        return await ReadImage(path);
    }

    public bool TryReadImage(string path, out byte[] image)
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
