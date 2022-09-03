using LinqToTwitter;
using LinqToTwitter.OAuth;

namespace SDParamsDescripter.Core.Models;
public class Twitter
{
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
        var image = File.ReadAllBytes(imagePath);
        var media = await Context.UploadMediaAsync(image, "image/png", "tweet_image");
        await Context.CreateMediaMetadataAsync(media.MediaID, imageAltText);
        await Context.TweetMediaAsync(text, new[] { media.MediaID.ToString() });
    }
}
