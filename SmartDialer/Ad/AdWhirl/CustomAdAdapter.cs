using System;

namespace MoAds.Example
{
    public class CustomAdAdapter : IAdAdapter
    {
        private CustomAdService adService = new CustomAdService();

        /// <summary>
        /// Gets or sets the publisher id.
        /// </summary>
        /// <value>The publisher id.</value>
        public string PublisherId { get; set; }

        /// <summary>
        /// Gets or sets the client id.
        /// </summary>
        /// <value>The client id.</value>
        public string ClientId { get; set; }

        public CustomAdAdapter()
        {
            adService.GotResult += new EventHandler<Service4u2.Common.EventArgs<CustomAdFetchResponse>>(adService_GotResult);
            adService.GotError += new EventHandler<Service4u2.Common.EventArgs<Exception>>(adService_GotError);
        }

        void adService_GotError(object sender, Service4u2.Common.EventArgs<Exception> e)
        {
            if (GotError != null)
                GotError(this, e);
        }

        void adService_GotResult(object sender, Service4u2.Common.EventArgs<CustomAdFetchResponse> e)
        {
            var adResponse = new AdResponse()
            {
                AdText = e.Argument.AdText,
                ImageUrl = e.Argument.ImageUrl,
                ClickUrl = e.Argument.ClickUrl
            };

            if (GotAdResponse != null)
                GotAdResponse(this, new Service4u2.Common.EventArgs<AdResponse>() { Argument = adResponse });
        }

        #region IAdAdapter Members

        public event EventHandler<Service4u2.Common.EventArgs<AdResponse>> GotAdResponse;

        public event EventHandler<Service4u2.Common.EventArgs<Exception>> GotError;

        public void GetAdResponseAsync()
        {
            adService.FetchAdAsync(this.PublisherId, this.ClientId);
        }

        #endregion
    }
}
