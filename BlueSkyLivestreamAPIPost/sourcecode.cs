using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

public class CPHInline
{
    public bool Execute()
    {
        // Call the main async logic and handle it synchronously in this Execute method
        PostToBluesky().GetAwaiter().GetResult();
        return true;
    }

    private async Task PostToBluesky()
    {
        // Define constants and retrieve arguments
        CPH.TryGetArg("game", out string currentGame);
        CPH.TryGetArg("targetUserName", out string twitchUser);
        CPH.TryGetArg("targetChannelTitle", out string streamTitle);
        CPH.TryGetArg("bskyHandle", out string bskyHandle);
        CPH.TryGetArg("bskyAppPass", out string bskyAppPass);
        string channelUrl = "twitch.tv/" + twitchUser;

        string BlueSkyHandle = bskyHandle + ".bsky.social";
        string ResolveIdEndpoint = "https://bsky.social/xrpc/com.atproto.identity.resolveHandle";
        string BlueSkyAppPass = bskyAppPass;
        string BlueSkyGetTokenEndpoint = "https://bsky.social/xrpc/com.atproto.server.createSession";
        string BlueSkyCreatePostEndpoint = "https://bsky.social/xrpc/com.atproto.repo.createRecord";

        try
        {
            using (HttpClient client = new HttpClient())
            {
                // 1. Resolve handle
                string handleUrl = $"{ResolveIdEndpoint}?handle={Uri.EscapeDataString(BlueSkyHandle)}";
                HttpResponseMessage handleResponse = await client.GetAsync(handleUrl);

                if (handleResponse.IsSuccessStatusCode)
                {
                    string handleContent = await handleResponse.Content.ReadAsStringAsync();
                    string DID = JObject.Parse(handleContent)["did"].ToString();
                    CPH.LogInfo($"DID: {DID}");

                    // 2. Get Token
                    var payload = new
                    {
                        identifier = DID,
                        password = BlueSkyAppPass
                    };
                    string payloadJson = JObject.FromObject(payload).ToString();

                    HttpContent content = new StringContent(payloadJson, Encoding.UTF8, "application/json");
                    HttpResponseMessage tokenResponse = await client.PostAsync(BlueSkyGetTokenEndpoint, content);

                    if (tokenResponse.IsSuccessStatusCode)
                    {
                        string tokenContent = await tokenResponse.Content.ReadAsStringAsync();
                        string TOKEN = JObject.Parse(tokenContent)["accessJwt"].ToString();
                        CPH.LogInfo($"Token: {TOKEN}");

                        // 3. Publish a new post with a hyperlink
                        string postText = "Testing API Posts via Streamer.bot\n\n" + streamTitle + "\n\n" + channelUrl;

                        // Calculate byte indices for the hyperlink
                        int byteStart = postText.IndexOf(channelUrl);
                        int byteEnd = byteStart + channelUrl.Length;

                        // Construct the post payload using JObject to handle special characters
                        var postPayload = new JObject
                        {
                            ["collection"] = "app.bsky.feed.post",
                            ["repo"] = DID,
                            ["record"] = new JObject
                            {
                                ["text"] = postText,
                                ["createdAt"] = DateTime.UtcNow.ToString("o"), // ISO 8601 format
                                ["type"] = "app.bsky.feed.post", // Changed to 'type'
                                ["facets"] = new JArray
                                {
                                    new JObject
                                    {
                                        ["index"] = new JObject
                                        {
                                            ["byteStart"] = byteStart,
                                            ["byteEnd"] = byteEnd
                                        },
                                        ["features"] = new JArray
                                        {
                                            new JObject
                                            {
                                                ["$type"] = "app.bsky.richtext.facet#link", // Added $type here
                                                ["uri"] = "https://" + channelUrl // Ensure the URI is complete
                                            }
                                        }
                                    }
                                }
                            }
                        };

                        string postPayloadJson = postPayload.ToString();

                        HttpRequestMessage postRequest = new HttpRequestMessage(HttpMethod.Post, BlueSkyCreatePostEndpoint);
                        postRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", TOKEN);
                        postRequest.Content = new StringContent(postPayloadJson, Encoding.UTF8, "application/json");

                        HttpResponseMessage postResponse = await client.SendAsync(postRequest);

                        if (postResponse.IsSuccessStatusCode)
                        {
                            string postContent = await postResponse.Content.ReadAsStringAsync();
                            CPH.LogInfo("Post response: " + postContent);
                        }
                        else
                        {
                            string errorContent = await postResponse.Content.ReadAsStringAsync();
                            CPH.LogWarn("Error posting feed: " + postResponse.StatusCode + " - " + errorContent);
                        }
                    }
                    else
                    {
                        CPH.LogWarn("Error retrieving token: " + tokenResponse.StatusCode);
                    }
                }
                else
                {
                    CPH.LogWarn("Error resolving handle: " + handleResponse.StatusCode);
                }
            }
        }
        catch (Exception ex)
        {
            CPH.LogError("Error: " + ex.Message);
        }
    }
}
