using Service4u2.Json;
using Newtonsoft.Json;

namespace SmartDialer.Ad.AdWhirl
{
    [JsonObject(MemberSerialization.OptIn)]
    public class AdWhirlExtraResponse : IJSONSelfSerialize<AdWhirlExtraResponse>
    {

        public AdWhirlExtraResponse SelfSerialize(string json)
        {
            AdWhirlExtraResponse deserializedProduct = JsonConvert.DeserializeObject<AdWhirlExtraResponse>(json);
            return deserializedProduct;
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class AdWhirlRationResponse : IJSONSelfSerialize<AdWhirlExtraResponse>
    {

        public AdWhirlRationResponse SelfSerialize(string json)
        {
            AdWhirlRationResponse deserializedProduct = JsonConvert.DeserializeObject<AdWhirlRationResponse>(json);
            return deserializedProduct;
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class AdWhirlFetchResponse : IJSONSelfSerialize<AdWhirlFetchResponse>
    {
        [JsonProperty]
        public AdWhirlExtraResponse extra { get; set; }
        [JsonProperty]
        public AdWhirlRationResponse ration { get; set; }
        public string AdText { get; set; }
        public string ClickUrl { get; set; }

        public AdWhirlFetchResponse SelfSerialize(string json)
        {
            AdWhirlFetchResponse deserializedProduct = JsonConvert.DeserializeObject<AdWhirlFetchResponse>(json);
            return deserializedProduct;
        }
    }

    public class AdWhirlAdService : BaseJsonService<AdWhirlFetchResponse>
    {
        private const string fetchUrl = "http://mob.adwhirl.com/getInfo?appid={0}&appver=200&client=3";

        public void FetchAdAsync(string publisherId, string clientId = "")
        {
            StartServiceCall(string.Format(fetchUrl, publisherId, clientId));
        }
    }
}
